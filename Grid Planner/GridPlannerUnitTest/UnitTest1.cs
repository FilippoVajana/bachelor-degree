using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SARSimulator;
using System.IO;
using System.Collections.Generic;
using SARLib.SAREnvironment;

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
            SIM = new MissionSimulator(ENVS_DIR);
        }

        [TestMethod]
        public void InitMissionSimulator()
        {
            var SIM = new MissionSimulator(ENVS_DIR);

            var envs = new List<string>()
            {
                "S-T4.json",
                "M-T4.json",
                "L-T4.json"
            };

            Assert.AreEqual(envs.Count, SIM.EnvPaths.Count);
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
            var SIM = new MissionSimulator(ENVS_DIR);
            var instanceBuilder = new SimulationInstancesPoolBuilder(SIM.EnvPaths, 5);
            

            var schema = new SimulationInstanceSchema(SimulationInstanceSchema.EnvironmentType.Small, SimulationInstanceSchema.PriorDistribution.Uniform, SimulationInstanceSchema.DangerDistribution.Uniform, SimulationInstanceSchema.RiskPropensity.Normal, (decimal)0.2);
            var schemaName = @"INSTANCE_Small_Uniform_Uniform_1_Normal_0,2";
            var instance = instanceBuilder.BuildInstance(schema);
            Assert.AreEqual(schemaName, instance.ID);
            //Debug
            var viewer = new SARViewer().DisplayProperty(instance._env, SARViewer.SARPointAttributes.Confidence);

            schema = new SimulationInstanceSchema(SimulationInstanceSchema.EnvironmentType.Medium, SimulationInstanceSchema.PriorDistribution.KDistributed, SimulationInstanceSchema.DangerDistribution.KDistributed, SimulationInstanceSchema.RiskPropensity.Normal, (decimal)0.2);
            schemaName = @"INSTANCE_Medium_KDistributed_KDistributed_1_Normal_0,2";
            instance = instanceBuilder.BuildInstance(schema);
            Assert.AreEqual(schemaName, instance.ID);
            //Debug
            viewer = new SARViewer().DisplayProperty(instance._env, SARViewer.SARPointAttributes.Confidence);
            viewer = new SARViewer().DisplayProperty(instance._env, SARViewer.SARPointAttributes.Danger);
        }

        [TestMethod]
        public void SimulationInstancesPoolBuild()
        {
            var poolBuilder = new SimulationInstancesPoolBuilder(SIM.EnvPaths, 10);

            var pool = poolBuilder.BuildInstancesPool();

            Assert.AreEqual(360, pool.Count);
        }
    }
}
