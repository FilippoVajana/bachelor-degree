using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using SARLib.SAREnvironment;
using SARLib.SARPlanner;
using System.Diagnostics;

namespace SARLibUnitTest
{
    [TestClass]
    public class PlannerUT
    {
        const int GRID_ROWS = 5;
        const int GRID_COLUMNS = 8;
        const int GRID_SEED = 10;
        const int RND_SHUFFLE = 50;

        SARGrid GRID = null;

        private SARGrid GetRndGrid()
        {
            var grid = new SARGrid(GRID_COLUMNS, GRID_ROWS);
            grid.RandomizeGrid(GRID_SEED, RND_SHUFFLE);
            return grid;
        }

        //GRIGLIA 8x5 -> (7x4 Max)
        [TestInitialize]
        public void TestInitialize()
        {
            GRID = GetRndGrid();
#if DEBUG
            Debug.WriteLine(GRID.ConvertToConsoleString());
#endif
        }

        [TestMethod]
        public void CostFunction()
        {
            var costFun = new SARCostFunction();
            //var GRID = GetRndGrid();

            var p1 = GRID.GetPoint(3, 4);
            var pCurr = GRID.GetPoint(0, 0);

            var cost = costFun.EvaluateCost(pCurr, p1);

            Assert.AreEqual(7, cost);
        }

        [TestMethod]
        public void UtilityFunction()
        {
            const int RADIUS = 2;
            double CONF_EXP = 0.7;
            double DNG_EXP = 1 - CONF_EXP;

            var utilityFun = new SARUtilityFunction(RADIUS, DNG_EXP, CONF_EXP);
            //var GRID = GetRndGrid();

            var p1 = GRID.GetPoint(3, 4);
            var pCurr = GRID.GetPoint(0, 0);

            var utilityValue = utilityFun.ComputeUtility(p1, pCurr, GRID);

            Assert.AreEqual(4.453.ToString("N3"), utilityValue.ToString("N3"));

        }
    }
}
