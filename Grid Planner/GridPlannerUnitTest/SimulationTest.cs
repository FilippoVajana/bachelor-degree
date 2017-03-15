using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SARSimulator;
using System.IO;
using System.Collections.Generic;
using SARLib.SAREnvironment;
using SARLib.SARPlanner;
using System.Threading;
using System.Diagnostics;

namespace GridPlannerUnitTest
{
    [TestClass]
    public class SimulationTest
    {
        [TestMethod]
        public void TestUtilityNoDistanceAsync()
        {
            //parametri istanza
            double risk = 0.8;
            decimal dangerT = (decimal)0.8;
            

            int mult = 0;
            string logDir = @"C:\Users\filip\Dropbox\Unimi\pianificazione\Grid Planner\GridPlannerUnitTest\Data\Test\FUtil";

            string refEnvPath = Path.GetFullPath(@"C:\Users\filip\Dropbox\Unimi\pianificazione\Grid Planner\GridPlannerUnitTest\Data\Test\FUtil\small_k_k_safe.json");
            //carico ambiente di riferimento
            var _refEnv = new SARGrid(refEnvPath);
            var _entryPoint = _refEnv.GetPoint(0, 0);
            var _utilityRadius = 3;
            var _utilityFuncDist = new SARUtilityFunction(_utilityRadius, (1 - risk), risk);
            var _utilityFuncNoDist = new SARUtilityFunction_Test_NoDistance(_utilityRadius, (1 - risk), risk);
            var _costFunc = new SARCostFunction();
            var _goalStrat = new SARGoalSelector();

            //nomi istanze
            string nameInstDist = $"INSTANCE_Small_KDistributed_KDistributed_1_Safe_0,8_DIST_Radius{_utilityRadius}";
            string nameInstNoDist = $"INSTANCE_Small_KDistributed_KDistributed_1_Safe_0,8_NODIST_Radius{_utilityRadius}";

            ///ISTANZA CON VALUTAZIONE DISTANZA
            ///
            //inizializzo parametri simulazione
            SimulationInstance simInst = new SimulationInstance(_refEnv, risk, dangerT, nameInstDist, mult);
            TimeSpan simDuration = TimeSpan.FromSeconds(10);

            //inizializzo pianificatore
            var planner = new SARPlanner(_refEnv, _entryPoint, dangerT, _utilityFuncDist, _costFunc, _goalStrat);

            //inizializzo logger             
            var instanceLogger = planner.SetupLogger(nameInstDist, mult, logDir, true);

            //avvio simulazione
            Debug.WriteLine($"[{DateTime.Now.ToUniversalTime()}] {nameInstDist} STARTED");
            var cTS = new CancellationTokenSource(simDuration);
            var cT = cTS.Token;
            //var task = new Task()
            var simTaskResult = planner.GenerateMission(cT);

            if (cT.IsCancellationRequested == false)
            {
                Debug.WriteLine($"[{DateTime.Now.ToUniversalTime()}] {nameInstDist} STOPPED");
            }

            //salvataggio logs
            //await new TaskFactory(TaskCreationOptions.AttachedToParent, TaskContinuationOptions.None).StartNew(() => instanceLogger.SaveLogs());
            //await Task.Run(() => instanceLogger.SaveLogs());
            instanceLogger.SaveLogs();

            cTS.Dispose();


            ///ISTANZA SENZA VALUTAZIONE DISTANZA
            ///

            //inizializzo parametri simulazione
            simInst = new SimulationInstance(_refEnv, risk, dangerT, nameInstNoDist, mult);
            simDuration = TimeSpan.FromSeconds(10);

            //inizializzo pianificatore
            planner = new SARPlanner(_refEnv, _entryPoint, dangerT, _utilityFuncNoDist, _costFunc, _goalStrat);

            //inizializzo logger             
            instanceLogger = planner.SetupLogger(nameInstNoDist, mult, logDir, true);

            //avvio simulazione
            Debug.WriteLine($"[{DateTime.Now.ToUniversalTime()}] {nameInstNoDist} STARTED");
            cTS = new CancellationTokenSource(simDuration);
            cT = cTS.Token;
            //var task = new Task()
            simTaskResult = planner.GenerateMission(cT);

            if (cT.IsCancellationRequested == false)
            {
                Debug.WriteLine($"[{DateTime.Now.ToUniversalTime()}] {nameInstNoDist} STOPPED");
            }

            //salvataggio logs
            //await new TaskFactory(TaskCreationOptions.AttachedToParent, TaskContinuationOptions.None).StartNew(() => instanceLogger.SaveLogs());
            //await Task.Run(() => instanceLogger.SaveLogs());
            instanceLogger.SaveLogs();

            cTS.Dispose();
        }

        [TestMethod]
        public void TestUtilityNoArea()
        {
            //parametri istanza
            double risk = 0.8;
            decimal dangerT = (decimal)0.4;


            int mult = 0;
            string logDir = @"C:\Users\filip\Dropbox\Unimi\pianificazione\Grid Planner\GridPlannerUnitTest\Data\Test\FUtil";

            string refEnvPath = Path.GetFullPath(@"C:\Users\filip\Dropbox\Unimi\pianificazione\Grid Planner\GridPlannerUnitTest\Data\Test\FUtil\small_k_k_safe.json");
            //carico ambiente di riferimento
            var _refEnv = new SARGrid(refEnvPath);
            var _entryPoint = _refEnv.GetPoint(0, 0);
            var _utilityRadius = 0;
            var _utilityFuncDist = new SARUtilityFunction_Test_NoArea(_utilityRadius, (1 - risk), risk);            
            var _costFunc = new SARCostFunction();
            var _goalStrat = new SARGoalSelector();

            //nomi istanze
            string nameInstNoArea = $"INSTANCE_Small_KDistributed_KDistributed_1_Safe_0,8_DIST_RADIUS{_utilityRadius}";
            

            ///ISTANZA CON VALUTAZIONE DISTANZA
            ///
            //inizializzo parametri simulazione
            SimulationInstance simInst = new SimulationInstance(_refEnv, risk, dangerT, nameInstNoArea, mult);
            TimeSpan simDuration = TimeSpan.FromSeconds(10);

            //inizializzo pianificatore
            var planner = new SARPlanner(_refEnv, _entryPoint, dangerT, _utilityFuncDist, _costFunc, _goalStrat);

            //inizializzo logger             
            var instanceLogger = planner.SetupLogger(nameInstNoArea, mult, logDir, true);

            //avvio simulazione
            Debug.WriteLine($"[{DateTime.Now.ToUniversalTime()}] {nameInstNoArea} STARTED");
            var cTS = new CancellationTokenSource(simDuration);
            var cT = cTS.Token;
            //var task = new Task()
            var simTaskResult = planner.GenerateMission(cT);

            if (cT.IsCancellationRequested == false)
            {
                Debug.WriteLine($"[{DateTime.Now.ToUniversalTime()}] {nameInstNoArea} STOPPED");
            }

            //salvataggio logs
            //await new TaskFactory(TaskCreationOptions.AttachedToParent, TaskContinuationOptions.None).StartNew(() => instanceLogger.SaveLogs());
            //await Task.Run(() => instanceLogger.SaveLogs());
            instanceLogger.SaveLogs();

            cTS.Dispose();            
        }
    }
}
