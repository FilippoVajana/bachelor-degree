using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SARLib.Toolbox;
using SARLib.SAREnvironment;
using System.Diagnostics;

namespace SARLibUnitTest
{    
    [TestClass]
    public class BayesUT
    {
        #region COSTANTI
        const string GRID_FILE_PATH = @"C:\Users\filip\Dropbox\Unimi\pianificazione\Grid Planner\test\SARLibUnitTest\Output\Data\ENVIRONMENTS\R10_C10_T5.json";
        const int GRID_ROWS = 5;
        const int GRID_COLUMNS = 8;
        const int GRID_SEED_1 = 10;
        const int GRID_SEED_2 = 34;
        const int RND_SHUFFLE = 50;
        const double FILTER_ALFA = 0.2;
        const double FILTER_BETA = 0.2;
        #endregion

        SARGrid GRID = null;
        SARViewer VIEWER = null;
        BayesEngine.BayesFilter FILTER = null;

        #region Metodi ausiliari
        private SARGrid GetRndGrid()
        {
            var grid = new SARGrid(GRID_FILE_PATH);
            //grid.RandomizeGrid(GRID_SEED_1, RND_SHUFFLE);
            return grid;
        }
        private SARGrid GetRndGrid(int rndSeed)
        {
            var grid = new SARGrid(GRID_COLUMNS, GRID_ROWS);
            grid.RandomizeGrid(rndSeed, RND_SHUFFLE);
            return grid;
        }
        private void DebugConsolePrint(SARViewer viewer, SARGrid environment)
        {
            VIEWER.DisplayProperty(environment, SARViewer.SARPointAttributes.Confidence);
            VIEWER.DisplayProperty(environment, SARViewer.SARPointAttributes.Danger);
        }
        #endregion

        //GRIGLIA 8x5 -> (7x4 Max)
        [TestInitialize]
        public void TestInitialize()
        {
            GRID = GetRndGrid();
            VIEWER = new SARViewer();
            FILTER = new BayesEngine.BayesFilter(FILTER_ALFA, FILTER_BETA);            
        }        

        [TestMethod]
        public void FilterPoint_SenseTrue()
        {
            //debug
            var gridConfStr = VIEWER.DisplayProperty(GRID, SARViewer.SARPointAttributes.Confidence);
            var gridStr = VIEWER.DisplayEnvironment(GRID);

            //imposto posizione target
            //GRID._realTarget = GRID.GetPoint(2, 1);

            //punto di sensing
            var sensePoint = GRID.GetPoint(2, 1);

            var posterior = FILTER.ComputeConfidencePosterior(sensePoint, sensePoint, 1);
            Assert.AreEqual(0.522.ToString("N3"), posterior.ToString("N3"));

            var p1 = GRID.GetPoint(4, 3); //deve calare
            posterior = FILTER.ComputeConfidencePosterior(sensePoint, p1, 1);
            Assert.AreEqual(0.019.ToString("N3"), posterior.ToString("N3"));

            p1 = GRID.GetPoint(4, 4); //ostacolo
            posterior = FILTER.ComputeConfidencePosterior(sensePoint, p1, 1);
            Assert.AreEqual(0.000.ToString("N3"), posterior.ToString("N3"));

            //debug
            gridConfStr = VIEWER.DisplayProperty(GRID, SARViewer.SARPointAttributes.Confidence);
        }

        [TestMethod]
        public void FilterPoint_SenseFalse()
        {
            //debug
            var gridConfStr = VIEWER.DisplayProperty(GRID, SARViewer.SARPointAttributes.Confidence);

            var sensePoint = GRID.GetPoint(2, 1); //auto aggiornamento
            var posterior = FILTER.ComputeConfidencePosterior(sensePoint, sensePoint, 0);
            Assert.AreEqual(0.064.ToString("N3"), posterior.ToString("N3"));

            var p1 = GRID.GetPoint(4, 3); //deve salire
            posterior = FILTER.ComputeConfidencePosterior(sensePoint, p1, 0);
            Assert.AreEqual(0.038.ToString("N3"), posterior.ToString("N3"));

            p1 = GRID.GetPoint(4, 4); //ostacolo
            posterior = FILTER.ComputeConfidencePosterior(sensePoint, p1, 0);
            Assert.AreEqual(0.000.ToString("N3"), posterior.ToString("N3"));

            //debug
            gridConfStr = VIEWER.DisplayProperty(GRID, SARViewer.SARPointAttributes.Confidence);
        }

        [TestMethod]
        public void FilterGrid_SenseTrue()
        {
            //debug
            var gridConfStr = VIEWER.DisplayProperty(GRID, SARViewer.SARPointAttributes.Confidence);

            var sensePoint = GRID.GetPoint(0, 0);
            var updateGrid = FILTER.UpdateEnvironmentConfidence(GRID, sensePoint, 1);

            //debug
            gridConfStr = VIEWER.DisplayProperty(GRID, SARViewer.SARPointAttributes.Confidence);
        }

        [TestMethod]
        public void FilterGrid_SenseFalse()
        {
            //debug
            var gridConfStr = VIEWER.DisplayProperty(GRID, SARViewer.SARPointAttributes.Confidence);

            var sensePoint = GRID.GetPoint(0, 0);
            var updateGrid = FILTER.UpdateEnvironmentConfidence(GRID, sensePoint, 0);

            //debug
            gridConfStr = VIEWER.DisplayProperty(GRID, SARViewer.SARPointAttributes.Confidence);
        }

        [TestMethod]
        public void NormalizeConfidence()
        {
            var gridStr = VIEWER.DisplayProperty(GRID, SARViewer.SARPointAttributes.Confidence);

            //normalizzo
            FILTER.NormalizeConfidence(GRID);

            var gridNormStr = VIEWER.DisplayProperty(GRID, SARViewer.SARPointAttributes.Confidence);

            double confidenceSum = 0;
            foreach (var p in GRID._grid)
            {
                confidenceSum += p.Confidence;
            }
            Assert.AreEqual(1.ToString(), confidenceSum.ToString());
        }
    }
}
