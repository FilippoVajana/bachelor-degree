using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SARSimulator;
using System.IO;
using System.Collections.Generic;
using SARLib.SAREnvironment;
using SARLib.SARPlanner;
using SARLib.Toolbox;

namespace GridPlannerUnitTest
{
    [TestClass]
    public class UnitTest1
    {
        //COSTANTI
        string ENVS_DIR;
        MissionSimulator SIM;


        [TestInitialize]
        public void TestInitialize()
        {
            ENVS_DIR = @"C:\Users\filip\Dropbox\Unimi\pianificazione\Grid Planner\GridPlannerUnitTest\Data\Environments";
            SIM = new MissionSimulator(ENVS_DIR, 1, @"C:\Users\filip\Dropbox\Unimi\pianificazione\Grid Planner\GridPlannerUnitTest\Data\Logs\", false, 1);
        }

        [TestMethod]
        public void InitMissionSimulator()
        {
            //var SIM = new MissionSimulator(ENVS_DIR);

            ////var envs = new List<string>()
            ////{
            ////    "S-T4.json",
            ////    "M-T4.json",
            ////    "L-T4.json"
            ////};

            //Assert.AreEqual(5, SIM.EnvPaths.Count);
        }

        [TestMethod]
        public void SimulationSchemaBuild()
        {
            var schema = new SimulationInstanceSchema(SimulationInstanceSchema.EnvironmentType.Small, SimulationInstanceSchema.PriorDistribution.Uniform, SimulationInstanceSchema.DangerDistribution.Uniform, SimulationInstanceSchema.RiskPropensity.Normal, (decimal)0.2);

            Assert.AreEqual(schema.TargetNum, 1);
            Assert.AreEqual(schema.EnvType, SimulationInstanceSchema.EnvironmentType.Small);            
        }

        [TestMethod]
        public void SimulationInstanceBuild()
        {
            //inizializzazione
            //var SIM = new MissionSimulator(ENVS_DIR);
            var instanceBuilder = new SimulationInstancesPoolBuilder(SIM.EnvPaths, 5);


            var schema = new SimulationInstanceSchema(SimulationInstanceSchema.EnvironmentType.Small, SimulationInstanceSchema.PriorDistribution.Uniform, SimulationInstanceSchema.DangerDistribution.Uniform, SimulationInstanceSchema.RiskPropensity.Normal, (decimal)0.2);
            var schemaName = @"INSTANCE_Small_Uniform_Uniform_1_Normal_0,2";
            var instance = instanceBuilder.BuildInstance(schema, 1);
            Assert.AreEqual(schemaName, instance.ID);
            //Debug
            var viewer = new SARViewer().DisplayProperty(instance._env, SARViewer.SARPointAttributes.Confidence);
            instance._env.SaveToFile(@"C:\Users\filip\Dropbox\Unimi\pianificazione\Grid Planner\GridPlannerUnitTest\Data\Logs\", schemaName);

            /////////////////////////////
            schema = new SimulationInstanceSchema(SimulationInstanceSchema.EnvironmentType.Small, SimulationInstanceSchema.PriorDistribution.KDistributed, SimulationInstanceSchema.DangerDistribution.KDistributed, SimulationInstanceSchema.RiskPropensity.Normal, (decimal)0.2);
            schemaName = @"INSTANCE_Small_KDistributed_KDistributed_1_Normal_0,2";
            instance = instanceBuilder.BuildInstance(schema, 1);
            Assert.AreEqual(schemaName, instance.ID);
            //Debug
            viewer = new SARViewer().DisplayEnvironment(instance._env);
            viewer = new SARViewer().DisplayProperty(instance._env, SARViewer.SARPointAttributes.Confidence);
            instance._env.SaveToFile(@"C:\Users\filip\Dropbox\Unimi\pianificazione\Grid Planner\GridPlannerUnitTest\Data\Logs\", schemaName);

            /////////////////////////////
            schema = new SimulationInstanceSchema(SimulationInstanceSchema.EnvironmentType.Medium, SimulationInstanceSchema.PriorDistribution.KDistributed, SimulationInstanceSchema.DangerDistribution.KDistributed, SimulationInstanceSchema.RiskPropensity.Normal, (decimal)0.2);
            schemaName = @"INSTANCE_Medium_KDistributed_KDistributed_1_Normal_0,2";
            instance = instanceBuilder.BuildInstance(schema, 1);
            Assert.AreEqual(schemaName, instance.ID);
            //Debug
            viewer = new SARViewer().DisplayProperty(instance._env, SARViewer.SARPointAttributes.Confidence);
            viewer = new SARViewer().DisplayProperty(instance._env, SARViewer.SARPointAttributes.Danger);
            instance._env.SaveToFile(@"C:\Users\filip\Dropbox\Unimi\pianificazione\Grid Planner\GridPlannerUnitTest\Data\Logs\", schemaName);
        }

        [TestMethod]
        public void SimulationInstancesPoolBuild()
        {
            var poolBuilder = new SimulationInstancesPoolBuilder(SIM.EnvPaths, 10);

            var pool = poolBuilder.BuildInstancesPool();

            Assert.AreEqual(360, pool.Count);
        }

        [TestMethod]
        public void DataAnalisysBuildRndEnv()
        {
            var instanceBuilder = new SimulationInstancesPoolBuilder(SIM.EnvPaths, 1);

            var schema = new SimulationInstanceSchema(SimulationInstanceSchema.EnvironmentType.Large, SimulationInstanceSchema.PriorDistribution.KDistributed, SimulationInstanceSchema.DangerDistribution.KDistributed, SimulationInstanceSchema.RiskPropensity.Normal, (decimal)0.2);
            var instance = instanceBuilder.BuildInstance(schema, 1);

            //Debug
            var viewer = new SARViewer().DisplayProperty(instance._env, SARViewer.SARPointAttributes.Confidence);
            viewer = new SARViewer().DisplayProperty(instance._env, SARViewer.SARPointAttributes.Danger);
            //instance._env.SaveToFile(@"C:\Users\filip\Dropbox\Unimi\pianificazione\Grid Planner\GridPlannerUnitTest\Data\Logs\", instance.ID);
        }

        [TestMethod]
        public void DataAnalisysGetDistributions()
        {
            var baseGrid = new SARGrid(@"C:\Users\filip\Dropbox\Unimi\pianificazione\Grid Planner\GridPlannerUnitTest\Data\Logs\INSTANCE_Large_KDistributed_KDistributed_1_Normal_0,2.json");
            var viewerConf = new SARViewer().DisplayProperty(baseGrid, SARViewer.SARPointAttributes.Confidence);
            var viewerDang = new SARViewer().DisplayProperty(baseGrid, SARViewer.SARPointAttributes.Danger);

            //calcolo utilità
            var utilityFunc = new SARUtilityFunction_Test_NoArea(0, (1 - 0.5), 0.5); //utilità modificata
            var utilBuilder = new GoalSelector(baseGrid, utilityFunc, new SARGoalSelector());
            var utilMap = utilBuilder.BuildUtilityMap(baseGrid.GetPoint(0, 0));
            var viewerUtil = new SARViewer().DisplayMap(baseGrid, utilMap);

            //calcolo aggiornamento post Z=1
            var sensePoint = baseGrid.GetPoint(0, 20);
            var updater = new BayesEngine.BayesFilter(0.2, 0.2);
            var updateTrue = updater.UpdateEnvironmentConfidence(baseGrid, sensePoint, 1);
            viewerConf = new SARViewer().DisplayProperty(updateTrue, SARViewer.SARPointAttributes.Confidence);

            //calcolo aggiornamento post Z=0
            baseGrid = new SARGrid(@"C:\Users\filip\Dropbox\Unimi\pianificazione\Grid Planner\GridPlannerUnitTest\Data\Logs\INSTANCE_Large_KDistributed_KDistributed_1_Normal_0,2.json");
            var updateFalse = updater.UpdateEnvironmentConfidence(baseGrid, sensePoint, 0);
            viewerConf = new SARViewer().DisplayProperty(updateFalse, SARViewer.SARPointAttributes.Confidence);
        }
        
    }
}
