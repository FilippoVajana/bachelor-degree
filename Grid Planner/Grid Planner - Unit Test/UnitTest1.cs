using System;
using System.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GridPlanner_UnitTest
{
    [TestClass]
    public class SAREnvironmentLib_UT
    {
        [TestMethod]
        public void Test()
        {
            var grid = new SAREnvironmentLibrary.SARGrid(10, 10);
            grid.Distance(new SAREnvironmentLibrary.SARPoint(0, 0), new SAREnvironmentLibrary.SARPoint(1, 1));

        }
        [TestMethod]
        public void GetValidPoint()
        {
            var grid = new SAREnvironmentLibrary.SARGrid(8, 5);
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
            var grid = new SAREnvironmentLibrary.SARGrid(5, 5);
            var point = grid.GetPoint(5, 5);            
            Assert.IsNull(point);

            point = grid.GetPoint(-1, 6);
            Assert.IsNull(point);            
        }

        [TestMethod]
        public void ManhattanDistance()
        {
            var grid = new SAREnvironmentLibrary.SARGrid(5, 5);
            var distance = grid.Distance(grid.GetPoint(0, 0), grid.GetPoint(1, 1));
            Assert.AreEqual(2, distance);

            distance = grid.Distance(grid.GetPoint(0, 0), grid.GetPoint(4, 1));
            Assert.AreEqual(5, distance);           
            
        }

        [TestMethod]
        public void RetrieveMaxNeighbors()
        {
            var grid = new SAREnvironmentLibrary.SARGrid(5, 5);

            var n = grid.GetNeighbors(new SAREnvironmentLibrary.SARPoint(0,0));
            Assert.AreEqual(2, n.Length);

            n = grid.GetNeighbors(new SAREnvironmentLibrary.SARPoint(2, 2));
            Assert.AreEqual(4, n.Length);
        }

        [TestMethod]
        public void RetrieveNeighborsRndGrid()
        {
            var grid = new SAREnvironmentLibrary.SARGrid(8, 5);
            grid.RandomizeGrid(5, 50);

            var gridString = grid.ConvertToConsoleString();

            var n = grid.GetNeighbors(new SAREnvironmentLibrary.SARPoint(0, 0));
            Assert.AreEqual(2, n.Length);

            n = grid.GetNeighbors(new SAREnvironmentLibrary.SARPoint(2, 2));
            Assert.AreEqual(3, n.Length);

            n = grid.GetNeighbors(new SAREnvironmentLibrary.SARPoint(1, 4));
            Assert.AreEqual(1, n.Length);
        }

        [TestMethod]
        public void FileIO()
        {
            var savedGrid = new SAREnvironmentLibrary.SARGrid(4, 8);
            savedGrid.RandomizeGrid(5, 4);

            string outFilePath = savedGrid.SaveToFile(true);
            Assert.IsNotNull(outFilePath);

            var loadedGrid = new SAREnvironmentLibrary.SARGrid(outFilePath);
            Assert.AreEqual(savedGrid.ConvertToConsoleString(), loadedGrid.ConvertToConsoleString());
        }
        
        [TestMethod]
        public void GridRandomization()
        {
            var grid = new SAREnvironmentLibrary.SARGrid(10, 20);
            grid.RandomizeGrid(5, 4, 0.8F);
            grid.SaveToFile(true);
        }
    }
}
