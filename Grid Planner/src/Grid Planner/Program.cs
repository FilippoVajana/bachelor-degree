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
            var grid = new SARGrid(10, 5); 
            //Console.WriteLine(grid.ConvertToConsoleString());

            grid.RandomizeGrid(10, 5, .8F);
            Console.WriteLine(grid.ConvertToConsoleString());
                       

            Console.Write("\nPress Enter to exit");
            Console.ReadKey();            
        }
    }
}
