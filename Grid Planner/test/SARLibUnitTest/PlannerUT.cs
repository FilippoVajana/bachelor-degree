using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using SARLib.SAREnvironment;
using SARLib.SARPlanner;
using System.Diagnostics;
using SARLib.Toolbox;
using System.Threading;

namespace SARLibUnitTest
{
    [TestClass]
    public class PlannerUT
    {
        const string GRID1_FILE_PATH = @"C:\Users\filip\Dropbox\Unimi\pianificazione\Grid Planner\GridPlannerUnitTest\Data\Environments\S-T4.json";
        
        const int GRID_ROWS = 5;
        const int GRID_COLUMNS = 8;
        const int GRID_SEED_1 = 10;
        const int GRID_SEED_2 = 34;
        const int RND_SHUFFLE = 50;
        const int UTILITY_RADIUS = 2;
        const double UTILITY_CONF_EXP = 0.7;
        const double UTILITY_DNG_EXP = 1 - UTILITY_CONF_EXP;
        const double FILTER_ALFA = 0.2;
        const double FILTER_BETA = 0.2;

        SARGrid GRID = null;
        SARViewer VIEWER = null;
        BayesEngine.BayesFilter FILTER = new BayesEngine.BayesFilter(FILTER_ALFA, FILTER_BETA);

        #region Metodi ausiliari
        private SARGrid GetRndGrid()
        {
            var grid = new SARGrid(GRID1_FILE_PATH);
            return grid;
        }
        private SARGrid GetRndGrid(int rndSeed)
        {
            var grid = new SARGrid(GRID_COLUMNS, GRID_ROWS);
            grid.RandomizeGrid(rndSeed, RND_SHUFFLE);
            return grid;
        }
        #endregion

        //GRIGLIA 8x5 -> (7x4 Max)
        [TestInitialize]
        public void TestInitialize()
        {
            GRID = GetRndGrid();
            FILTER.NormalizeConfidence(GRID);
            VIEWER = new SARViewer();            
        }

        [TestMethod]
        public void CostFunction()
        {
            var costFun = new SARCostFunction();            

            var p1 = GRID.GetPoint(3, 4);//goal
            var pCurr = GRID.GetPoint(0, 0);//start

            var cost = costFun.EvaluateCost(pCurr, p1);

            Assert.AreEqual(7, cost);
        }

        [TestMethod]
        public void UtilityFunction()
        {
            var utilityFun = new SARUtilityFunction(UTILITY_RADIUS, UTILITY_DNG_EXP, UTILITY_CONF_EXP);

            var pCurr = GRID.GetPoint(0, 0);
            var p1 = GRID.GetPoint(7, 4);
            var utilityValue1 = utilityFun.ComputeUtility(p1, pCurr, GRID);
            Assert.AreEqual(0.111.ToString("N3"), utilityValue1.ToString("N3"));

            var p2 = GRID.GetPoint(0, 9);
            var utilityValue2 = utilityFun.ComputeUtility(p2, pCurr, GRID);
            Assert.AreEqual(0.202.ToString("N3"), utilityValue2.ToString("N3"));

            var p3 = GRID.GetPoint(0, 2);
            var utilityValue3 = utilityFun.ComputeUtility(p3, pCurr, GRID);
            Assert.AreEqual(0.158.ToString("N3"), utilityValue3.ToString("N3"));

            //Debug
            //var confStr = VIEWER.DisplayProperty(GRID, SARViewer.SARPointAttributes.Confidence) + "\n\n" +
            //    VIEWER.DisplayProperty(GRID, SARViewer.SARPointAttributes.Danger);
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

            #region Debug/Analisi
            //visualizzazione debug
            var utilMapString = VIEWER.DisplayMap(GRID, utilityMap);
            var confMapString = VIEWER.DisplayProperty(GRID, SARViewer.SARPointAttributes.Confidence);
            var dangerMapString = VIEWER.DisplayProperty(GRID, SARViewer.SARPointAttributes.Danger);

            ////salvataggio csv mappe
            //VIEWER.BuildMapCsv(GRID, utilityMap, "UTILITY");
            //VIEWER.BuildPropertyCsv(GRID, SARViewer.SARPointAttributes.Confidence);
            //VIEWER.BuildPropertyCsv(GRID, SARViewer.SARPointAttributes.Danger);

            #endregion
            //applico strategia
            var goal = selectionStrategy.SelectNextTarget(utilityMap);

            Assert.AreEqual(GRID.GetPoint(0, 1), goal);

            //SECONDO AMBIENTE
            var GRID2 = GetRndGrid(GRID_SEED_2);
            new BayesEngine.BayesFilter(1, 1).NormalizeConfidence(GRID2);//normalizzo confidenza

            //generazione mappa utilità
            utilityMap = new Dictionary<SARPoint, double>();
            foreach (var p in GRID2._grid)
            {
                utilityMap.Add(p, utilityFun.ComputeUtility(p, pCurr, GRID2));
            }

            //visualizzazione debug
            utilMapString = VIEWER.DisplayMap(GRID2, utilityMap);
            confMapString = VIEWER.DisplayProperty(GRID2, SARViewer.SARPointAttributes.Confidence);
            dangerMapString = VIEWER.DisplayProperty(GRID2, SARViewer.SARPointAttributes.Danger);

            ////salvataggio csv mappe
            //VIEWER.BuildMapCsv(GRID2, utilityMap, "UTILITY");
            //VIEWER.BuildPropertyCsv(GRID2, SARViewer.SARPointAttributes.Confidence);
            //VIEWER.BuildPropertyCsv(GRID2, SARViewer.SARPointAttributes.Danger);

            //applico strategia
            goal = selectionStrategy.SelectNextTarget(utilityMap);

            Assert.AreEqual(GRID2.GetPoint(6, 4), goal);
        }

        [TestMethod]
        public void RoutePlanner()
        {
            //PERCORSO 1 - Soglia massima

            var costFunc = new SARCostFunction();
            var planner = new RoutePlanner(GRID, costFunc);

            //pianifico percorso
            var startPos = GRID.GetPoint(0, 0);
            var goalPos = GRID.GetPoint(4, 3);
            var route = planner.ComputeRoute(startPos, goalPos);

            //visualizzazione grafica
            var gridStr = VIEWER.DisplayEnvironment(GRID);
            var routeStr = VIEWER.DisplayRoute(GRID, route);

            Assert.AreEqual(8, route.Count); //lunghezza percorso

            //////////////////////////////////////////////////////////

            //PERCORSO 2 - Pericolo Alto
            
            //costFunc = new SARCostFunction();
            planner = new RoutePlanner(GRID, costFunc, (decimal)0.8);

            //pianifico percorso
            startPos = GRID.GetPoint(0, 0);
            goalPos = GRID.GetPoint(0, 9);
            route = planner.ComputeRoute(startPos, goalPos);

            //visualizzazione grafica
            gridStr = VIEWER.DisplayEnvironment(GRID);
            routeStr = VIEWER.DisplayRoute(GRID, route);
            //hash = SARViewer.GetHashString(GRID);

            Assert.AreEqual(0, route.Count); //lunghezza percorso

            //////////////////////////////////////////////////////////

            //PERCORSO 3 - Valutazione pericolo per ordinamento frontiera 
            GRID = new SARGrid(@"C:\Users\filip\Dropbox\Unimi\pianificazione\Grid Planner\GridPlannerUnitTest\Data\Environments\S-T4_danger_cost.json");         
            //costFunc = new SARCostFunction();
            planner = new RoutePlanner(GRID, costFunc, (decimal) 1);

            //pianifico percorso
            startPos = GRID.GetPoint(0, 0);
            goalPos = GRID.GetPoint(4, 9);
            route = planner.ComputeRoute(startPos, goalPos);

            //visualizzazione grafica
            gridStr = VIEWER.DisplayEnvironment(GRID);
            routeStr = VIEWER.DisplayRoute(GRID, route);
            var hash = SARViewer.GetHashString(GRID);

            Assert.AreEqual(32, route.Count); //lunghezza percorso

            //////////////////////////////////////////////////////////
            
        }

        [TestMethod]
        public void PlanRunner()
        {
            //PERCORSO 1
            var costFunc = new SARCostFunction();
            var planner = new RoutePlanner(GRID, costFunc);
            var runner = new PlanRunner();

            //pianifico percorso
            var startPos = GRID.GetPoint(0, 0);
            var goalPos = GRID.GetPoint(0, 2);
            var route = planner.ComputeRoute(startPos, goalPos);
            var nextPos = runner.ExecutePlan(route);

            //visualizzazione grafica
            var gridStr = VIEWER.DisplayEnvironment(GRID);
            var routeStr = VIEWER.DisplayRoute(GRID, route);

            Assert.AreEqual(GRID.GetPoint(1, 0), nextPos);
        }

        [TestMethod]
        public void MissionPlanner_Small()
        {
            var u_radius = 2;
            var u_conf = 0.8;
            var u_dang = 1 - u_conf;

            var costFunc = new SARCostFunction();
            var utilityFunc = new SARUtilityFunction(u_radius, UTILITY_DNG_EXP, UTILITY_CONF_EXP);
            var goalStrat = new SARGoalSelector();
            var dangerThreshold = (decimal) 0.8;

            //carico ambiente custom
            GRID = new SARGrid(@"C:\Users\filip\Dropbox\Unimi\pianificazione\Grid Planner\GridPlannerUnitTest\Data\Environments\S-T1_mission-planner.json");
            var entryP = GRID.GetPoint(0, 0);
            GRID._realTarget = GRID.GetPoint(9, 9); //forzo la posizione del target


            //MISSIONE 1
            
            //inizializzo pianificatore
            var planner = new SARPlanner(GRID, entryP, 1, utilityFunc, costFunc, goalStrat);

            //pianifico missione
            var mission = planner.GenerateMission(new CancellationToken());

            var mRoute = VIEWER.DisplayRoute(GRID, mission.Route);
            var mGoals = mission.Goals;
            Debug.WriteLine("\n\n\n");

            //MISSIONE 2
            GRID = new SARGrid(@"C:\Users\filip\Dropbox\Unimi\pianificazione\Grid Planner\GridPlannerUnitTest\Data\Environments\S-T1_mission-planner.json");
            entryP = GRID.GetPoint(0, 0);
            GRID._realTarget = GRID.GetPoint(9, 9); //forzo la posizione del target

            //inizializzo pianificatore
            planner = new SARPlanner(GRID, entryP, dangerThreshold, utilityFunc, costFunc, goalStrat);

            //pianifico missione
            mission = planner.GenerateMission(new CancellationToken());

            mRoute = VIEWER.DisplayRoute(GRID, mission.Route);
            mGoals = mission.Goals;
            
        }
        
        [TestMethod]
        public void MissionPlanner_Medium()
        {
            var u_radius = 1;
            var u_conf = 0.8;
            var u_dang = 1 - u_conf;

            var costFunc = new SARCostFunction();
            var utilityFunc = new SARUtilityFunction(u_radius, UTILITY_DNG_EXP, UTILITY_CONF_EXP);
            var goalStrat = new SARGoalSelector();
            var dangerThreshold = (decimal)0.7;
            


            //MISSIONE 1
            GRID = new SARGrid(@"C:\Users\filip\Dropbox\Unimi\pianificazione\Grid Planner\GridPlannerUnitTest\Data\Environments\M-T1_mission-planner.json");
            var entryP = GRID.GetPoint(0, 0);
            GRID._realTarget = GRID.GetPoint(11, 23); //forzo la posizione del target

            //inizializzo pianificatore
            var planner = new SARPlanner(GRID, entryP, dangerThreshold, utilityFunc, costFunc, goalStrat);

            //pianifico missione
            var mission = planner.GenerateMission(new CancellationToken());

            var mRoute = VIEWER.DisplayRoute(GRID, mission.Route);
            var mGoals = mission.Goals;
        }

        [TestMethod]
        public void AdaptiveDangerThreshold()
        {
            //PERCORSO 1
            //var searchAlgoritm = new AStar();
            var costFunc = new SARCostFunction();
            decimal dangerThreshold = (decimal) 1;
            
            var planner = new RoutePlanner(GRID, costFunc, dangerThreshold);
            
            //pianifico percorso
            var startPos = GRID.GetPoint(0, 0);
            var goalPos = GRID.GetPoint(4, 5);
            var route = planner.ComputeRoute(startPos, goalPos);

            //visualizzazione grafica
            var gridStr = VIEWER.DisplayEnvironment(GRID);
            var routeStr = VIEWER.DisplayRoute(GRID, route);

            //costruisco csv per l'andamento della soglia di pericolo
            //var dangerThresholdLogCsv = "A* Danger Threshold Value" + Environment.NewLine;
            //foreach (var l in planner._dangerThresholdLog)
            //{
            //    dangerThresholdLogCsv += $"{l.ToString()};{Environment.NewLine}";
            //}
            
            Assert.AreEqual(10, route.Count); //lunghezza percorso
        }
    }
}
