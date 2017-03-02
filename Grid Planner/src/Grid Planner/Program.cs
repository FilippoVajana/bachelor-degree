using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SARLib.SAREnvironment;
using System.IO;

namespace GridPlanner
{
    public class Program
    {        
        public static void Main(string[] args)
        {
            var sarEnv = new SARGrid(10, 20);            
            
            sarEnv.RandomizeGrid(10, 5, .65F);
            Console.WriteLine(new SARViewer().DisplayEnvironment(sarEnv));

            var json = sarEnv.SaveToFile(Directory.GetCurrentDirectory());
            Console.WriteLine($"Saved JSON At {json}");

            Console.Write("\nPress Enter to exit");
            Console.ReadKey();            
        }
    }
}
