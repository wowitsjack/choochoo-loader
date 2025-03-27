using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.IO;
using System.Threading;

namespace ChooChooEngine.App.Core
{
    public class ProcessManager
    {
        #region Win32 API

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        private static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, 
            IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        [DllImport("kernel32.dll")]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, 
            uint nSize, out UIntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, 
            uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll")]
        private static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint dwFreeType);

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenThread(int dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport("kernel32.dll")]
        private static extern uint SuspendThread(IntPtr hThread);

        [DllImport("kernel32.dll")]
        private static extern uint ResumeThread(IntPtr hThread);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CreateProcess(string lpApplicationName, string lpCommandLine, 
            IntPtr lpProcessAttributes, IntPtr lpThreadAttributes, bool bInheritHandles, uint dwCreationFlags, 
            IntPtr lpEnvironment, string lpCurrentDirectory, ref STARTUPINFO lpStartupInfo, 
            out PROCESS_INFORMATION lpProcessInformation);
        
        [DllImport("Dbghelp.dll", SetLastError = true)]
        private static extern bool MiniDumpWriteDump(IntPtr hProcess, int ProcessId, IntPtr hFile, 
            int DumpType, IntPtr ExceptionParam, IntPtr UserStreamParam, IntPtr CallbackParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct STARTUPINFO
        {
            public int cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        // Access rights
        private const int PROCESS_CREATE_THREAD = 0x0002;
        private const int PROCESS_QUERY_INFORMATION = 0x0400;
        private const int PROCESS_VM_OPERATION = 0x0008;
        private const int PROCESS_VM_WRITE = 0x0020;
        private const int PROCESS_VM_READ = 0x0010;
        private const int PROCESS_ALL_ACCESS = 0x1F0FFF;

        // Thread access rights
        private const int THREAD_SUSPEND_RESUME = 0x0002;
        private const int THREAD_GET_CONTEXT = 0x0008;
        private const int THREAD_SET_CONTEXT = 0x0010;
        private const int THREAD_ALL_ACCESS = 0x1F03FF;

        // Memory allocation
        private const uint MEM_COMMIT = 0x1000;
        private const uint MEM_RESERVE = 0x2000;
        private const uint MEM_RELEASE = 0x8000;
        private const uint PAGE_READWRITE = 0x04;
        private const uint CREATE_SUSPENDED = 0x00000004;

        // MiniDump types
        private const int MiniDumpNormal = 0x00000000;
        private const int MiniDumpWithFullMemory = 0x00000002;

        #endregion

        private Process _process;
        private IntPtr _processHandle;
        private bool _processHandleOpen = false;

        public event EventHandler<ProcessEventArgs> ProcessStarted;
        public event EventHandler<ProcessEventArgs> ProcessStopped;
        public event EventHandler<ProcessEventArgs> ProcessAttached;
        public event EventHandler<ProcessEventArgs> ProcessDetached;

        public Process CurrentProcess => _process;
        public bool IsProcessRunning => _process != null && !_process.HasExited;
        public int ProcessId => _process?.Id ?? -1;

        public ProcessManager()
        {
        }

        public bool LaunchProcess(string exePath, string workingDir = null, LaunchMethod method = LaunchMethod.CreateProcess)
        {
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                return false;

            if (string.IsNullOrEmpty(workingDir))
                workingDir = Path.GetDirectoryName(exePath);

            try
            {
                switch (method)
                {
                    case LaunchMethod.CreateProcess:
                        return LaunchWithCreateProcess(exePath, workingDir);
                    case LaunchMethod.CmdStart:
                        return LaunchWithCmd(exePath, workingDir);
                    case LaunchMethod.CreateThreadInjection:
                        return LaunchWithCreateThreadInjection(exePath, workingDir);
                    case LaunchMethod.RemoteThreadInjection:
                        return LaunchWithRemoteThreadInjection(exePath, workingDir);
                    case LaunchMethod.ShellExecute:
                        return LaunchWithShellExecute(exePath, workingDir);
                    case LaunchMethod.ProcessStart:
                        return LaunchWithProcessStart(exePath, workingDir);
                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error launching process: {ex.Message}");
                return false;
            }
        }

        public bool AttachToProcess(int processId)
        {
            try
            {
                _process = Process.GetProcessById(processId);
                OpenProcessHandle();
                OnProcessAttached(new ProcessEventArgs(_process));
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error attaching to process: {ex.Message}");
                return false;
            }
        }

        public bool DetachFromProcess()
        {
            if (_process == null)
                return false;

            CloseProcessHandle();
            OnProcessDetached(new ProcessEventArgs(_process));
            _process = null;
            return true;
        }

        public bool KillProcess()
        {
            if (_process == null || _process.HasExited)
                return false;

            try
            {
                _process.Kill();
                OnProcessStopped(new ProcessEventArgs(_process));
                _process = null;
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error killing process: {ex.Message}");
                return false;
            }
        }

        public bool SuspendProcess()
        {
            if (_process == null || _process.HasExited)
                return false;

            try
            {
                foreach (ProcessThread thread in _process.Threads)
                {
                    IntPtr threadHandle = OpenThread(THREAD_SUSPEND_RESUME, false, (uint)thread.Id);
                    if (threadHandle != IntPtr.Zero)
                    {
                        SuspendThread(threadHandle);
                        CloseHandle(threadHandle);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error suspending process: {ex.Message}");
                return false;
            }
        }

        public bool ResumeProcess()
        {
            if (_process == null || _process.HasExited)
                return false;

            try
            {
                foreach (ProcessThread thread in _process.Threads)
                {
                    IntPtr threadHandle = OpenThread(THREAD_SUSPEND_RESUME, false, (uint)thread.Id);
                    if (threadHandle != IntPtr.Zero)
                    {
                        ResumeThread(threadHandle);
                        CloseHandle(threadHandle);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error resuming process: {ex.Message}");
                return false;
            }
        }

        public bool CreateMiniDump(string outputPath, bool fullMemory = false)
        {
            if (_process == null || _process.HasExited)
                return false;

            try
            {
                using (FileStream fs = new FileStream(outputPath, FileMode.Create, FileAccess.ReadWrite, FileShare.Write))
                {
                    int dumpType = fullMemory ? MiniDumpWithFullMemory : MiniDumpNormal;
                    MiniDumpWriteDump(_processHandle, _process.Id, fs.SafeFileHandle.DangerousGetHandle(), 
                        dumpType, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating minidump: {ex.Message}");
                return false;
            }
        }

        public List<ProcessModule> GetProcessModules()
        {
            if (_process == null || _process.HasExited)
                return new List<ProcessModule>();

            try
            {
                List<ProcessModule> modules = new List<ProcessModule>();
                foreach (ProcessModule module in _process.Modules)
                {
                    modules.Add(module);
                }
                return modules;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting process modules: {ex.Message}");
                return new List<ProcessModule>();
            }
        }

        public List<ProcessThread> GetProcessThreads()
        {
            if (_process == null || _process.HasExited)
                return new List<ProcessThread>();

            try
            {
                List<ProcessThread> threads = new List<ProcessThread>();
                foreach (ProcessThread thread in _process.Threads)
                {
                    threads.Add(thread);
                }
                return threads;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting process threads: {ex.Message}");
                return new List<ProcessThread>();
            }
        }

        public IntPtr GetProcessHandle()
        {
            if (_process == null || _process.HasExited)
                return IntPtr.Zero;

            if (!_processHandleOpen)
                OpenProcessHandle();

            return _processHandle;
        }

        private bool OpenProcessHandle()
        {
            if (_process == null || _process.HasExited)
                return false;

            try
            {
                _processHandle = OpenProcess(PROCESS_ALL_ACCESS, false, _process.Id);
                _processHandleOpen = _processHandle != IntPtr.Zero;
                return _processHandleOpen;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error opening process handle: {ex.Message}");
                return false;
            }
        }

        private void CloseProcessHandle()
        {
            if (_processHandleOpen && _processHandle != IntPtr.Zero)
            {
                CloseHandle(_processHandle);
                _processHandle = IntPtr.Zero;
                _processHandleOpen = false;
            }
        }

        #region Launch Methods

        private bool LaunchWithCreateProcess(string exePath, string workingDir)
        {
            STARTUPINFO startupInfo = new STARTUPINFO();
            startupInfo.cb = Marshal.SizeOf(startupInfo);
            PROCESS_INFORMATION processInfo = new PROCESS_INFORMATION();

            bool result = CreateProcess(exePath, null, IntPtr.Zero, IntPtr.Zero, 
                false, 0, IntPtr.Zero, workingDir, ref startupInfo, out processInfo);

            if (result)
            {
                _process = Process.GetProcessById(processInfo.dwProcessId);
                _processHandle = processInfo.hProcess;
                _processHandleOpen = true;
                OnProcessStarted(new ProcessEventArgs(_process));
                return true;
            }

            return false;
        }

        private bool LaunchWithCmd(string exePath, string workingDir)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c start \"\" \"{exePath}\"",
                WorkingDirectory = workingDir,
                CreateNoWindow = true,
                UseShellExecute = false
            };

            Process cmdProcess = Process.Start(startInfo);
            cmdProcess.WaitForExit();

            // We need to find our newly created process
            // This is a simplistic approach and may not work in all cases
            Process[] processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(exePath));
            if (processes.Length > 0)
            {
                _process = processes[0];
                OpenProcessHandle();
                OnProcessStarted(new ProcessEventArgs(_process));
                return true;
            }

            return false;
        }

        private bool LaunchWithCreateThreadInjection(string exePath, string workingDir)
        {
            // This is a placeholder. In a real implementation, this would
            // create a suspended process and inject code to call CreateThread
            return LaunchWithCreateProcess(exePath, workingDir);
        }

        private bool LaunchWithRemoteThreadInjection(string exePath, string workingDir)
        {
            // This is a placeholder. In a real implementation, this would
            // create a suspended process and inject code to call CreateRemoteThread
            return LaunchWithCreateProcess(exePath, workingDir);
        }

        private bool LaunchWithShellExecute(string exePath, string workingDir)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = workingDir,
                UseShellExecute = true
            };

            _process = Process.Start(startInfo);
            OpenProcessHandle();
            OnProcessStarted(new ProcessEventArgs(_process));
            return true;
        }

        private bool LaunchWithProcessStart(string exePath, string workingDir)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = workingDir,
                UseShellExecute = false
            };

            _process = Process.Start(startInfo);
            OpenProcessHandle();
            OnProcessStarted(new ProcessEventArgs(_process));
            return true;
        }

        #endregion

        #region Event Methods

        protected virtual void OnProcessStarted(ProcessEventArgs e)
        {
            ProcessStarted?.Invoke(this, e);
        }

        protected virtual void OnProcessStopped(ProcessEventArgs e)
        {
            ProcessStopped?.Invoke(this, e);
        }

        protected virtual void OnProcessAttached(ProcessEventArgs e)
        {
            ProcessAttached?.Invoke(this, e);
        }

        protected virtual void OnProcessDetached(ProcessEventArgs e)
        {
            ProcessDetached?.Invoke(this, e);
        }

        #endregion
    }

    public enum LaunchMethod
    {
        CreateProcess,
        CmdStart,
        CreateThreadInjection,
        RemoteThreadInjection,
        ShellExecute,
        ProcessStart
    }

    public class ProcessEventArgs : EventArgs
    {
        public Process Process { get; }

        public ProcessEventArgs(Process process)
        {
            Process = process;
        }
    }
} 