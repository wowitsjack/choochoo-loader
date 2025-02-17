using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ChooChooApp
{
    public static class ProcessHelper
    {
        // Under Wine, Process.GetProcesses() works reliably.
        // We filter by nonempty MainWindowTitle.
        public static Process[] GetProcesses()
        {
            try
            {
                Process[] all = Process.GetProcesses();
                List<Process> list = new List<Process>();
                foreach (Process p in all)
                {
                    try { if (!string.IsNullOrEmpty(p.MainWindowTitle)) list.Add(p); } catch { }
                }
                return list.ToArray();
            }
            catch { return new Process[0]; }
        }
    }
}
