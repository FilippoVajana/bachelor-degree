using System;
using System.Collections.Generic;
using System.Text;
using SARLib.SAREnvironment;
using SARLib.SARMission;
using SARLib.SARPlanner;
using System.Threading;
using System.Threading.Tasks;

namespace SARSimulator
{
    interface ISimulator
    {
        void InitSimulation(string environmentsFolder, int maxEnvInstance, TimeSpan maxSimDuration, int maxSimRunningInstance);
        SARMission StartInstance(SimulationInstance instance);
        void StopInstance();
    }

    class MissionSimulator
    {
        ///Passi di inizializzazione di una simulazione
        ///1- setup dei parametri base della simulazione
        ///2- importare environment da file
        ///3- generazione random prior
        ///4- generazione random danger
        ///5- assegnazione random del target
        ///6- impostare pianificatore
        ///

        String ENVS_FOLDER;
        int ENVS_INSTANCES;

        TimeSpan SIM_DURATION;
        int SIM_THREADS;        

        private void InitSimulation(string environmentsFolder, int maxEnvInstance, TimeSpan maxSimDuration, int maxSimRunningInstance)
        {
            ENVS_FOLDER = environmentsFolder;
            ENVS_INSTANCES = maxEnvInstance;
            SIM_DURATION = maxSimDuration;
            SIM_THREADS = maxSimRunningInstance;
            
        }

        /*
        private SARMission StartInstance(SimulationInstance instance)
        {
            //configuro pianificatore missione
            var missionPlanner = new SARPlanner();

            ///CREAZIONE E CANCELLAZIONE DI UN TASK
            ///
            // sorgente segnale di cancellazione
            CancellationTokenSource cancTokenSource = new CancellationTokenSource(SIM_DURATION);
            // task per la simulazione
            var simTask = new Task<ISARMission>(() => { return missionPlanner.GenerateMission(); }, cancTokenSource.Token);
        }
        */


        private SARGrid LoadEnvironmentJson(string source)
        {
            return new SARGrid(source);            
        }
    }

    class SimulationInstance
    {
        public enum PriorDistribution {Uniform, KDistributed};
        public enum DangerDistribution { Uniform, KDistributed};
        public SimulationInstance(SARGrid environment, PriorDistribution priorType, DangerDistribution dangerType, SARPoint targetPos, double utilityParam_A, double riskThreshold)
        { }
    }
}
