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

            Assert.AreEqual(0.708.ToString("N3"), utilityValue.ToString("N3"));

        }

        [TestMethod]
        public void GoalSelectionStrategy()
        {
            //funzione utilità
            const int RADIUS = 2;
            double CONF_EXP = 0.7;
            double DNG_EXP = 1 - CONF_EXP;
            var utilityFun = new SARUtilityFunction(RADIUS, DNG_EXP, CONF_EXP);

            //strategia selezione goal
            var selectionStrategy = new SARGoalSelector();

            //posizione corrente
            var pCurr = GRID.GetPoint(0, 0);

            //costruisco mappa utilità
            var utilityMap = new Dictionary<SARPoint, double>();            
            foreach (var p in GRID._grid)
            {
                utilityMap.Add(p, utilityFun.ComputeUtility(p, pCurr, GRID));
            }

            //visualizzazione debug
            var utilMapString = VIEWER.DisplayMap(GRID, utilityMap);            
            var confMapString = VIEWER.DisplayProperty(GRID, SARViewer.SARPointAttributes.Confidence);
            var dangerMapString = VIEWER.DisplayProperty(GRID, SARViewer.SARPointAttributes.Danger);

            //salvataggio csv mappe
            VIEWER.BuildMapCsv(GRID, utilityMap, "UTILITY");
            VIEWER.BuildPropertyCsv(GRID, SARViewer.SARPointAttributes.Confidence);
            VIEWER.BuildPropertyCsv(GRID, SARViewer.SARPointAttributes.Danger);

            //applico strategia
            var goal = selectionStrategy.SelectNextTarget(utilityMap);

            var pGoal = GRID.GetPoint(1, 4);
            Assert.AreEqual(pGoal, goal);
        }
    }
}
