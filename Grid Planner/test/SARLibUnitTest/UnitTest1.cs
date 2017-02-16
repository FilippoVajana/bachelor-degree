using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SARLib.SAREnvironment;
using System.IO;

namespace SARLibUnitTest
{
    [TestClass]
    public class SAREnvironment
    {
        [TestMethod]
        public void Test()
        {
            var grid = new SARGrid(10, 10);
            grid.Distance(new SARPoint(0, 0), new SARPoint(1, 1));

        }
        [TestMethod]
        public void GetValidPoint()
        {
            var grid = new SARGrid(8, 5);
            var point = grid.GetPoint(0, 0);
            Assert.IsNotNull(point);

            point = grid.GetPoint(5, 5);
            Assert.IsNull(point);

            point = grid.GetPoint(-1, 6);
            Assert.IsNull(point);

            point = grid.GetPoint(2, 4);
            Assert.AreEqual(2, point.X);
            Assert.AreEqual(4, point.Y);
        }

        [TestMethod]
        public void GetNullFromInvalidPoint()
        {
            var grid = new SARGrid(5, 5);
            var point = grid.GetPoint(5, 5);
            Assert.IsNull(point);

            point = grid.GetPoint(-1, 6);
            Assert.IsNull(point);
        }

        [TestMethod]
        public void ManhattanDistance()
        {
            var grid = new SARGrid(5, 5);
            var distance = grid.Distance(grid.GetPoint(0, 0), grid.GetPoint(1, 1));
            Assert.AreEqual(2, distance);

            distance = grid.Distance(grid.GetPoint(0, 0), grid.GetPoint(4, 1));
            Assert.AreEqual(5, distance);

        }

        [TestMethod]
        public void RetrieveMaxNeighbors()
        {
            var grid = new SARGrid(5, 5);

            var n = grid.GetNeighbors(new SARPoint(0, 0));
            Assert.AreEqual(2, n.Length);

            n = grid.GetNeighbors(new SARPoint(2, 2));
            Assert.AreEqual(4, n.Length);
        }

        [TestMethod]
        public void RetrieveNeighborsRndGrid()
        {
            var grid = new SARGrid(8, 5);
            grid.RandomizeGrid(5, 50);

            var gridString = grid.ConvertToConsoleString();

            var n = grid.GetNeighbors(new SARPoint(0, 0));
            Assert.AreEqual(2, n.Length);

            n = grid.GetNeighbors(new SARPoint(2, 2));
            Assert.AreEqual(3, n.Length);

            n = grid.GetNeighbors(new SARPoint(1, 4));
            Assert.AreEqual(1, n.Length);
        }

        [TestMethod]
        public void FileIO()
        {
            var savedGrid = new SARGrid(4, 8);
            savedGrid.RandomizeGrid(5, 4);

#if !DEBUG
            string outFilePath = savedGrid.SaveToFile(Directory.GetCurrentDirectory()); 
#endif
#if DEBUG
            string outFilePath = savedGrid.SaveToFile(Path.GetFullPath(@"C:\Users\filip\Dropbox\Unimi\pianificazione\Grid Planner\test\SARLibUnitTest\bin\Debug\netcoreapp1.0")); 
#endif
            Assert.IsNotNull(outFilePath);

            var loadedGrid = new SARGrid(outFilePath);
            Assert.AreEqual(savedGrid.ConvertToConsoleString(), loadedGrid.ConvertToConsoleString());
        }

        [TestMethod]
        public void GridRandomization()
        {
            var grid = new SARGrid(10, 20);
            grid.RandomizeGrid(5, 4, 0.8F);
#if !DEBUG
            string outFilePath = Directory.GetCurrentDirectory(); 
#endif
#if DEBUG
            string outFilePath = Path.GetFullPath(@"C:\Users\filip\Dropbox\Unimi\pianificazione\Grid Planner\test\SARLibUnitTest\bin\Debug\netcoreapp1.0");
#endif
            grid.SaveToFile(outFilePath);
        }
    }
}