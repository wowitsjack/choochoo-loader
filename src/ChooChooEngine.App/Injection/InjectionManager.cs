using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Timers;
using ChooChooEngine.App.Core;

namespace ChooChooEngine.App.Injection
{
    public class InjectionManager
    {
        #region Win32 API

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll")]
        private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, 
            uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll")]
        private static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint dwFreeType);

        [DllImport("kernel32.dll")]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, 
            uint nSize, out UIntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        private static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, 
            IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);

        // Access rights
        private const int PROCESS_CREATE_THREAD = 0x0002;
        private const int PROCESS_QUERY_INFORMATION = 0x0400;
        private const int PROCESS_VM_OPERATION = 0x0008;
        private const int PROCESS_VM_WRITE = 0x0020;
        private const int PROCESS_VM_READ = 0x0010;
        private const int PROCESS_ALL_ACCESS = 0x1F0FFF;

        // Memory allocation
        private const uint MEM_COMMIT = 0x1000;
        private const uint MEM_RESERVE = 0x2000;
        private const uint MEM_RELEASE = 0x8000;
        private const uint PAGE_READWRITE = 0x04;
        private const uint PAGE_EXECUTE_READWRITE = 0x40;

        #endregion

        private ProcessManager _processManager;
        private System.Timers.Timer _monitoringTimer;
        private Dictionary<string, bool> _validatedDlls = new Dictionary<string, bool>();
        private readonly object _injectionLock = new object();
        
        public event EventHandler<InjectionEventArgs> InjectionSucceeded;
        public event EventHandler<InjectionEventArgs> InjectionFailed;
        
        public string MainDllPath { get; set; }
        public List<string> AdditionalDllPaths { get; } = new List<string>(4);
        public bool AutoInject { get; set; } = false;
        public int MonitoringInterval { get; set; } = 1000;
        public bool IsMonitoring { get; private set; } = false;
        public InjectionMethod InjectionMethod { get; set; } = InjectionMethod.StandardInjection;

        public InjectionManager(ProcessManager processManager)
        {
            _processManager = processManager;
            _monitoringTimer = new System.Timers.Timer(MonitoringInterval);
            _monitoringTimer.Elapsed += OnMonitoringTimerElapsed;
        }

        public void StartMonitoring()
        {
            if (!IsMonitoring)
            {
                _monitoringTimer.Interval = MonitoringInterval;
                _monitoringTimer.Start();
                IsMonitoring = true;
            }
        }

        public void StopMonitoring()
        {
            if (IsMonitoring)
            {
                _monitoringTimer.Stop();
                IsMonitoring = false;
            }
        }

        public bool InjectDll(string dllPath)
        {
            if (string.IsNullOrEmpty(dllPath) || !File.Exists(dllPath))
                return false;

            if (!ValidateDll(dllPath))
            {
                OnInjectionFailed(new InjectionEventArgs(dllPath, "DLL validation failed"));
                return false;
            }

            IntPtr processHandle = _processManager.GetProcessHandle();
            if (processHandle == IntPtr.Zero)
            {
                OnInjectionFailed(new InjectionEventArgs(dllPath, "Process handle is invalid"));
                return false;
            }

            try
            {
                lock (_injectionLock)
                {
                    switch (InjectionMethod)
                    {
                        case InjectionMethod.StandardInjection:
                            return InjectDllStandard(processHandle, dllPath);
                        case InjectionMethod.ManualMapping:
                            return InjectDllManualMapping(processHandle, dllPath);
                        default:
                            return InjectDllStandard(processHandle, dllPath);
                    }
                }
            }
            catch (Exception ex)
            {
                OnInjectionFailed(new InjectionEventArgs(dllPath, $"Injection failed: {ex.Message}"));
                return false;
            }
        }

        public void InjectAllDlls()
        {
            if (!string.IsNullOrEmpty(MainDllPath))
            {
                InjectDll(MainDllPath);
            }

            foreach (string dllPath in AdditionalDllPaths)
            {
                if (!string.IsNullOrEmpty(dllPath))
                {
                    InjectDll(dllPath);
                }
            }
        }

        public bool ValidateDll(string dllPath)
        {
            if (string.IsNullOrEmpty(dllPath) || !File.Exists(dllPath))
                return false;

            // Check if we've already validated this DLL
            if (_validatedDlls.TryGetValue(dllPath, out bool isValid))
                return isValid;

            try
            {
                // Try to load the DLL in the current process
                IntPtr moduleHandle = LoadLibrary(dllPath);
                if (moduleHandle == IntPtr.Zero)
                {
                    _validatedDlls[dllPath] = false;
                    return false;
                }

                // Check architecture compatibility (32-bit vs 64-bit)
                bool isProcess64Bit = Environment.Is64BitProcess;
                bool isDll64Bit = IsDll64Bit(dllPath);

                // Unload the DLL
                FreeLibrary(moduleHandle);

                // Check if architectures match
                bool architecturesMatch = isProcess64Bit == isDll64Bit;
                _validatedDlls[dllPath] = architecturesMatch;
                
                return architecturesMatch;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error validating DLL: {ex.Message}");
                _validatedDlls[dllPath] = false;
                return false;
            }
        }

        private bool IsDll64Bit(string dllPath)
        {
            try
            {
                // Read the PE header to determine if the DLL is 32-bit or 64-bit
                using (FileStream fs = new FileStream(dllPath, FileMode.Open, FileAccess.Read))
                using (BinaryReader br = new BinaryReader(fs))
                {
                    // DOS header
                    fs.Position = 0x3C;
                    uint peHeaderOffset = br.ReadUInt32();

                    // PE header signature
                    fs.Position = peHeaderOffset;
                    uint peHeaderSignature = br.ReadUInt32();
                    if (peHeaderSignature != 0x00004550) // "PE\0\0"
                        return false;

                    // COFF header
                    fs.Position += 20;
                    ushort characteristics = br.ReadUInt16();

                    // Check if the IMAGE_FILE_32BIT_MACHINE flag is set
                    const ushort IMAGE_FILE_32BIT_MACHINE = 0x0100;
                    bool is32Bit = (characteristics & IMAGE_FILE_32BIT_MACHINE) != 0;
                    
                    return !is32Bit;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error determining DLL architecture: {ex.Message}");
                return false;
            }
        }

        private bool InjectDllStandard(IntPtr processHandle, string dllPath)
        {
            // Get the address of LoadLibraryA in kernel32.dll
            IntPtr loadLibraryAddr = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");
            if (loadLibraryAddr == IntPtr.Zero)
                return false;

            // Allocate memory in the target process for the DLL path
            byte[] dllPathBytes = Encoding.ASCII.GetBytes(dllPath);
            uint allocSize = (uint)((dllPathBytes.Length + 1) * Marshal.SizeOf(typeof(char)));

            IntPtr remoteMemory = VirtualAllocEx(processHandle, IntPtr.Zero, allocSize, 
                MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
            if (remoteMemory == IntPtr.Zero)
                return false;

            try
            {
                // Write the DLL path to the allocated memory
                UIntPtr bytesWritten;
                bool writeResult = WriteProcessMemory(processHandle, remoteMemory, dllPathBytes, 
                    allocSize, out bytesWritten);
                if (!writeResult)
                    return false;

                // Create a remote thread that calls LoadLibraryA with the DLL path as argument
                IntPtr threadHandle = CreateRemoteThread(processHandle, IntPtr.Zero, 0, 
                    loadLibraryAddr, remoteMemory, 0, IntPtr.Zero);
                if (threadHandle == IntPtr.Zero)
                    return false;

                // Wait for the thread to finish
                WaitForSingleObject(threadHandle, 5000);

                // Get the thread exit code (should be the handle to the loaded DLL)
                uint exitCode;
                GetExitCodeThread(threadHandle, out exitCode);

                // Clean up
                CloseHandle(threadHandle);

                if (exitCode == 0)
                {
                    OnInjectionFailed(new InjectionEventArgs(dllPath, "LoadLibraryA returned 0"));
                    return false;
                }

                OnInjectionSucceeded(new InjectionEventArgs(dllPath));
                return true;
            }
            finally
            {
                // Free the allocated memory
                VirtualFreeEx(processHandle, remoteMemory, 0, MEM_RELEASE);
            }
        }

        private bool InjectDllManualMapping(IntPtr processHandle, string dllPath)
        {
            // Manual mapping is a more complex technique and would be implemented here
            // For now, we'll use the standard injection method as a fallback
            return InjectDllStandard(processHandle, dllPath);
        }

        private void OnMonitoringTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (AutoInject && _processManager.IsProcessRunning)
            {
                InjectAllDlls();
            }
        }

        #region Additional P/Invoke for thread handling

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetExitCodeThread(IntPtr hThread, out uint lpExitCode);

        #endregion

        #region Event Methods

        protected virtual void OnInjectionSucceeded(InjectionEventArgs e)
        {
            InjectionSucceeded?.Invoke(this, e);
        }

        protected virtual void OnInjectionFailed(InjectionEventArgs e)
        {
            InjectionFailed?.Invoke(this, e);
        }

        #endregion
    }

    public enum InjectionMethod
    {
        StandardInjection,
        ManualMapping
    }

    public class InjectionEventArgs : EventArgs
    {
        public string DllPath { get; }
        public string Message { get; }

        public InjectionEventArgs(string dllPath, string message = null)
        {
            DllPath = dllPath;
            Message = message;
        }
    }
} 