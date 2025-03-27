using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using ChooChooEngine.App.Core;

namespace ChooChooEngine.App.Memory
{
    public class MemoryManager
    {
        #region Win32 API

        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, 
            uint nSize, out UIntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, 
            uint nSize, out UIntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        private static extern IntPtr VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, 
            out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);

        [StructLayout(LayoutKind.Sequential)]
        private struct MEMORY_BASIC_INFORMATION
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public uint AllocationProtect;
            public IntPtr RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }

        // Memory states
        private const uint MEM_COMMIT = 0x1000;
        private const uint MEM_FREE = 0x10000;
        private const uint MEM_RESERVE = 0x2000;

        // Memory protection
        private const uint PAGE_EXECUTE = 0x10;
        private const uint PAGE_EXECUTE_READ = 0x20;
        private const uint PAGE_EXECUTE_READWRITE = 0x40;
        private const uint PAGE_EXECUTE_WRITECOPY = 0x80;
        private const uint PAGE_NOACCESS = 0x01;
        private const uint PAGE_READONLY = 0x02;
        private const uint PAGE_READWRITE = 0x04;
        private const uint PAGE_WRITECOPY = 0x08;
        private const uint PAGE_GUARD = 0x100;

        #endregion

        private ProcessManager _processManager;
        
        public event EventHandler<MemoryEventArgs> MemoryOperationSucceeded;
        public event EventHandler<MemoryEventArgs> MemoryOperationFailed;

        public MemoryManager(ProcessManager processManager)
        {
            _processManager = processManager;
        }

        public byte[] ReadMemory(IntPtr address, uint size)
        {
            IntPtr processHandle = _processManager.GetProcessHandle();
            if (processHandle == IntPtr.Zero)
            {
                OnMemoryOperationFailed(new MemoryEventArgs(address, size, "Process handle is invalid"));
                return null;
            }

            byte[] buffer = new byte[size];
            UIntPtr bytesRead;

            try
            {
                if (!ReadProcessMemory(processHandle, address, buffer, size, out bytesRead) || bytesRead.ToUInt32() != size)
                {
                    OnMemoryOperationFailed(new MemoryEventArgs(address, size, "Failed to read memory"));
                    return null;
                }

                OnMemoryOperationSucceeded(new MemoryEventArgs(address, size, "Memory read successfully"));
                return buffer;
            }
            catch (Exception ex)
            {
                OnMemoryOperationFailed(new MemoryEventArgs(address, size, $"Error reading memory: {ex.Message}"));
                return null;
            }
        }

        public bool WriteMemory(IntPtr address, byte[] data)
        {
            IntPtr processHandle = _processManager.GetProcessHandle();
            if (processHandle == IntPtr.Zero)
            {
                OnMemoryOperationFailed(new MemoryEventArgs(address, (uint)data.Length, "Process handle is invalid"));
                return false;
            }

            UIntPtr bytesWritten;

            try
            {
                if (!WriteProcessMemory(processHandle, address, data, (uint)data.Length, out bytesWritten) || 
                    bytesWritten.ToUInt32() != data.Length)
                {
                    OnMemoryOperationFailed(new MemoryEventArgs(address, (uint)data.Length, "Failed to write memory"));
                    return false;
                }

                OnMemoryOperationSucceeded(new MemoryEventArgs(address, (uint)data.Length, "Memory written successfully"));
                return true;
            }
            catch (Exception ex)
            {
                OnMemoryOperationFailed(new MemoryEventArgs(address, (uint)data.Length, $"Error writing memory: {ex.Message}"));
                return false;
            }
        }

        public List<MemoryRegion> QueryMemoryRegions()
        {
            IntPtr processHandle = _processManager.GetProcessHandle();
            if (processHandle == IntPtr.Zero)
            {
                OnMemoryOperationFailed(new MemoryEventArgs(IntPtr.Zero, 0, "Process handle is invalid"));
                return null;
            }

            List<MemoryRegion> regions = new List<MemoryRegion>();
            IntPtr address = IntPtr.Zero;

            try
            {
                while (true)
                {
                    MEMORY_BASIC_INFORMATION mbi;
                    IntPtr result = VirtualQueryEx(processHandle, address, out mbi, (uint)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION)));
                    
                    if (result.ToInt64() == 0)
                        break;

                    if (mbi.State == MEM_COMMIT)
                    {
                        MemoryRegion region = new MemoryRegion
                        {
                            BaseAddress = mbi.BaseAddress,
                            Size = mbi.RegionSize,
                            Protection = mbi.Protect,
                            State = mbi.State,
                            Type = mbi.Type
                        };

                        regions.Add(region);
                    }

                    // Move to the next region
                    address = new IntPtr(mbi.BaseAddress.ToInt64() + mbi.RegionSize.ToInt64());
                    
                    // Break if we've reached the end of the address space or wrapped around
                    if (address.ToInt64() <= 0 || address.ToInt64() >= Int64.MaxValue)
                        break;
                }

                OnMemoryOperationSucceeded(new MemoryEventArgs(IntPtr.Zero, 0, "Memory regions queried successfully"));
                return regions;
            }
            catch (Exception ex)
            {
                OnMemoryOperationFailed(new MemoryEventArgs(IntPtr.Zero, 0, $"Error querying memory regions: {ex.Message}"));
                return null;
            }
        }

        public MemoryState SaveMemoryState()
        {
            List<MemoryRegion> regions = QueryMemoryRegions();
            if (regions == null)
                return null;

            MemoryState state = new MemoryState();
            state.Regions = new List<MemoryRegionState>();

            foreach (MemoryRegion region in regions)
            {
                // Skip regions that are not readable
                if ((region.Protection & (PAGE_READONLY | PAGE_READWRITE | PAGE_EXECUTE_READ | PAGE_EXECUTE_READWRITE)) == 0)
                    continue;

                byte[] data = ReadMemory(region.BaseAddress, (uint)region.Size.ToInt64());
                if (data != null)
                {
                    MemoryRegionState regionState = new MemoryRegionState
                    {
                        BaseAddress = region.BaseAddress,
                        Size = region.Size,
                        Data = data
                    };

                    state.Regions.Add(regionState);
                }
            }

            return state;
        }

        public bool RestoreMemoryState(MemoryState state)
        {
            if (state == null || state.Regions == null)
                return false;

            bool success = true;

            foreach (MemoryRegionState regionState in state.Regions)
            {
                if (!WriteMemory(regionState.BaseAddress, regionState.Data))
                {
                    success = false;
                }
            }

            return success;
        }

        public bool SaveMemoryStateToFile(string filePath)
        {
            MemoryState state = SaveMemoryState();
            if (state == null)
                return false;

            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    // Write the number of regions
                    bw.Write(state.Regions.Count);

                    // Write each region
                    foreach (MemoryRegionState regionState in state.Regions)
                    {
                        // Write base address
                        bw.Write(regionState.BaseAddress.ToInt64());

                        // Write size
                        bw.Write(regionState.Size.ToInt64());

                        // Write data
                        bw.Write(regionState.Data.Length);
                        bw.Write(regionState.Data);
                    }
                }

                OnMemoryOperationSucceeded(new MemoryEventArgs(IntPtr.Zero, 0, "Memory state saved to file successfully"));
                return true;
            }
            catch (Exception ex)
            {
                OnMemoryOperationFailed(new MemoryEventArgs(IntPtr.Zero, 0, $"Error saving memory state to file: {ex.Message}"));
                return false;
            }
        }

        public MemoryState LoadMemoryStateFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            try
            {
                MemoryState state = new MemoryState();
                state.Regions = new List<MemoryRegionState>();

                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                using (BinaryReader br = new BinaryReader(fs))
                {
                    // Read the number of regions
                    int regionCount = br.ReadInt32();

                    // Read each region
                    for (int i = 0; i < regionCount; i++)
                    {
                        // Read base address
                        long baseAddress = br.ReadInt64();

                        // Read size
                        long size = br.ReadInt64();

                        // Read data
                        int dataLength = br.ReadInt32();
                        byte[] data = br.ReadBytes(dataLength);

                        MemoryRegionState regionState = new MemoryRegionState
                        {
                            BaseAddress = new IntPtr(baseAddress),
                            Size = new IntPtr(size),
                            Data = data
                        };

                        state.Regions.Add(regionState);
                    }
                }

                OnMemoryOperationSucceeded(new MemoryEventArgs(IntPtr.Zero, 0, "Memory state loaded from file successfully"));
                return state;
            }
            catch (Exception ex)
            {
                OnMemoryOperationFailed(new MemoryEventArgs(IntPtr.Zero, 0, $"Error loading memory state from file: {ex.Message}"));
                return null;
            }
        }

        #region Event Methods

        protected virtual void OnMemoryOperationSucceeded(MemoryEventArgs e)
        {
            MemoryOperationSucceeded?.Invoke(this, e);
        }

        protected virtual void OnMemoryOperationFailed(MemoryEventArgs e)
        {
            MemoryOperationFailed?.Invoke(this, e);
        }

        #endregion
    }

    public class MemoryRegion
    {
        public IntPtr BaseAddress { get; set; }
        public IntPtr Size { get; set; }
        public uint Protection { get; set; }
        public uint State { get; set; }
        public uint Type { get; set; }
    }

    public class MemoryRegionState
    {
        public IntPtr BaseAddress { get; set; }
        public IntPtr Size { get; set; }
        public byte[] Data { get; set; }
    }

    public class MemoryState
    {
        public List<MemoryRegionState> Regions { get; set; }
    }

    public class MemoryEventArgs : EventArgs
    {
        public IntPtr Address { get; }
        public uint Size { get; }
        public string Message { get; }

        public MemoryEventArgs(IntPtr address, uint size, string message = null)
        {
            Address = address;
            Size = size;
            Message = message;
        }
    }
} 