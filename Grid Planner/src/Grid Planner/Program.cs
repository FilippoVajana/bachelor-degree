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


            Console.WriteLine("URBAN SEARCH AND RESCUE SIMULATOR\n\n");

            //input molteplicità
            Console.WriteLine("Insert instance multiplicity: ");
            var mult = Console.ReadLine();

            //input durata massima istanza
            Console.WriteLine("Insert instance maximum running time (seconds): ");
            int maxTime = int.Parse(Console.ReadLine());

            //input verbose
            bool verbose = false;
            Console.WriteLine($"Logging VERBOSE? [y/n]");
            var v = Console.ReadLine();
            if (v == "y")
            {
                verbose = true;
            }
            
            //SIMULATOR
            SARSimulator.MissionSimulator sarSimulator = new SARSimulator.MissionSimulator(APP_ENVS_ROOT, int.Parse(mult), APP_LOGS_ROOT, verbose, maxTime);
            sarSimulator.StartSimulation();

            Console.Write("\nPress Enter to exit");
            Console.ReadKey();
        }
        
    }
}
