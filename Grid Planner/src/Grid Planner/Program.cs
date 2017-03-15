using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SARLib.SAREnvironment;
using System.IO;
using System.Threading;

namespace GridPlanner
{
    public class Program
    {        
        public static void Main(string[] args)
        {            
            //APP DIRECTORIES 
            string APP_DATA_ROOT = Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Data")).FullName;
            string APP_ENVS_ROOT = Directory.CreateDirectory(Path.Combine(APP_DATA_ROOT, "Environments")).FullName;
            //string APP_LOGS_ROOT = Directory.CreateDirectory(Path.Combine(APP_DATA_ROOT, "Logs")).FullName;
            string APP_LOGS_ROOT = @"C:\Users\filip\Desktop\Grid Planner\Data\Logs";

            //creo directories
            Directory.CreateDirectory(APP_DATA_ROOT);
            Directory.CreateDirectory(APP_ENVS_ROOT);
            Directory.CreateDirectory(APP_LOGS_ROOT);

            //input molteplicità
            Console.WriteLine("INSERT MULTEPLICITY:" + Environment.NewLine);
            var mult = Console.ReadLine();

            //input verbose
            bool verbose = false;
            Console.WriteLine($"VERBOSE MODE? [y/n]");
            var v = Console.ReadLine();
            if (v == "y")
            {
                verbose = true;
            }
            
            //SIMULATOR
            SARSimulator.MissionSimulator SIMULATOR = new SARSimulator.MissionSimulator(APP_ENVS_ROOT, int.Parse(mult), APP_LOGS_ROOT, verbose);
            

            Console.Write("\nPress Enter to exit");
            Console.ReadKey();
        }
        
    }
}
