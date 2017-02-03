using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grid_Planner
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var grid = new SARGrid(10, 10);
            Console.WriteLine(grid.ToString());

            grid.Randomize(10, 50);
            Console.WriteLine(grid.ToString());

            var neighbors = grid.GetNeighbors(1, 1);
            foreach (var n in neighbors)
            {
                Console.Write("{0}, ", n); 
            }

            Console.Write("\nPress Enter to exit");
            Console.ReadKey();
        }
    }
}
