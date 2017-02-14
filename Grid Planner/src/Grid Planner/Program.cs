using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GridPlanner
{
    public class Program
    {        
        public static void Main(string[] args)
        {
            var grid = new SARGrid(20, 10); 
            Console.WriteLine(grid.ConvertToConsoleString());

            grid.FillGridRandom(10, 50);
            Console.WriteLine(grid.ConvertToConsoleString());

            var neighbors = grid.GetNeighbors(new SARPoint(1,1));
            foreach (var n in neighbors)
            {
                Console.Write("{0}, ", n); 
            }

            Console.Write("\nPress Enter to exit");
            Console.ReadKey();            
        }
    }
}
