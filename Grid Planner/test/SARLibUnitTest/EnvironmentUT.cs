using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SARLib.SAREnvironment;
using System.IO;

namespace SARLibUnitTest
{
    [TestClass]
    public class EnvironmentUT
    {
        const int GRID_ROWS = 5;
        const int GRID_COLUMNS = 8;
        const int GRID_SEED = 10;
        const int RND_SHUFFLE = 50;

        SARGrid GRID = null;
        SARViewer VIEWER = null;

        #region Metodi ausiliari
        private SARGrid GetRndGrid()
        {
            var grid = new SARGrid(GRID_COLUMNS, GRID_ROWS);
            grid.RandomizeGrid(GRID_SEED, RND_SHUFFLE);
            return grid;
        }
        #endregion

        //GRIGLIA 8x5 -> (7x4 Max)
        [TestInitialize]
        public void TestInitialize()
        {
            GRID = GetRndGrid();
            VIEWER = new SARViewer();
            var gridStr = VIEWER.DisplayEnvironment(GRID);
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

            var n = grid.GetNeighbors(grid.GetPoint(0, 0));
            Assert.AreEqual(2, n.Length);

            n = grid.GetNeighbors(grid.GetPoint(2, 2));
            Assert.AreEqual(4, n.Length);
        }

        [TestMethod]
        public void RetrieveNeighborsRndGrid()
        {
            var grid = GRID;
            //grid.RandomizeGrid(5, 50);

            var gridString = VIEWER.DisplayEnvironment(GRID); //grid.ConvertToConsoleString();

            var n = grid.GetNeighbors(grid.GetPoint(0, 0));
            Assert.AreEqual(0, n.Length);

            n = grid.GetNeighbors(grid.GetPoint(2, 2));
            Assert.AreEqual(4, n.Length);

            n = grid.GetNeighbors(grid.GetPoint(1, 4));
            Assert.AreEqual(3, n.Length);
        }

        [TestMethod]
        public void ExportEnvironment()
        {
            var savedGrid = GRID;            

#if !DEBUG
            string outFilePath = savedGrid.SaveToFile(Directory.GetCurrentDirectory()); 
#endif
#if DEBUG
            string outFilePath = savedGrid.SaveToFile(Path.GetFullPath(@"C:\Users\filip\Dropbox\Unimi\pianificazione\Grid Planner\test\SARLibUnitTest\Output\SARGrid")); 
#endif
            Assert.IsNotNull(outFilePath);
            
        }

        [TestMethod]
        public void ImportEnvironment()
        {
            var savedGrid = GRID;
            var dstPath = Path.GetFullPath(@"C:\Users\filip\Dropbox\Unimi\pianificazione\Grid Planner\test\SARLibUnitTest\Output\SARGrid");
#if !DEBUG
            string outFilePath = savedGrid.SaveToFile(Directory.GetCurrentDirectory()); 
#endif
#if DEBUG
            string outFilePath = savedGrid.SaveToFile(dstPath, "prova");
#endif
            Assert.IsNotNull(outFilePath);

            //var outFilePath = @"C:\Users\filip\Dropbox\Unimi\pianificazione\Grid Planner\test\SARLibUnitTest\Output\SARGrid\prova.json";
            var loadedGrid = new SARGrid(outFilePath);
            Assert.AreEqual(savedGrid.ToString(), loadedGrid.ToString());
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