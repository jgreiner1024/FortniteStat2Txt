using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FortniteApi.Data;
using FortniteStat2Txt.Shared;

//needed to prevent conflict between namespace and the Console class
using ConsoleScreen = System.Console;

namespace FortniteStat2Txt.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            
            if (args.Length < 1)
            {
                ConsoleScreen.WriteLine(" - Usage - ");
                ConsoleScreen.WriteLine("FortniteStat2Txt.Console.exe [platform]");
                ConsoleScreen.WriteLine("platform = XBOX PS4 PC");
                return;
            }

            Platform platform;
            string platformArg = args[0].ToUpper();
            switch (args[0])
            {
                case ("XBOX"):
                    platform = Platform.Xbl;
                    break;
                case ("PS4"):
                    platform = Platform.Psn;
                    break;
                case ("PC"):
                    platform = Platform.Pc;
                    break;
                default:
                    ConsoleScreen.WriteLine("Unknown Platform please use one of these: XBOX PS4 PC");
                    return;
            }
            

            FortniteStatWriter statWriter = new FortniteStatWriter(
                ConfigurationManager.AppSettings["ApiKey"],
                ConfigurationManager.AppSettings["EpicUserName"],
                ConfigurationManager.AppSettings["OutputFolder"], 
                platform
            );

            statWriter.Updated += StatWriter_Updated;

            Task statWriterTask = statWriter.Start();

            ConsoleScreen.WriteLine("Press the ENTER key to exit.");
            ConsoleScreen.ReadLine();
            ConsoleScreen.WriteLine("Exiting please wait...");
            statWriter.Stop();
            statWriterTask.Wait();
            
        }

        private static void StatWriter_Updated(object sender, UpdatedEventArgs e)
        {
            ConsoleScreen.Title = $"Fortnite Stats Last Updated: {e.UpdatedTime.ToLongTimeString()}";
        }
    }
}
