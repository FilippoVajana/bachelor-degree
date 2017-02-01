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
            var grid = new Grid(10, 10);
            Console.WriteLine(grid.ToString());

            grid.Randomize(10, 50);
            Console.WriteLine(grid.ToString());

            Console.Write("Press Enter to exit");
            Console.ReadKey();
        }
    }
}
