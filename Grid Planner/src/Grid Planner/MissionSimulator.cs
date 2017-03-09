using System;
using System.Collections.Generic;
using SARLib.SAREnvironment;
using SARLib.SARPlanner;
using System.IO;
using System.Diagnostics;
using SARLib.SARMission;

namespace SARSimulator
{
    interface ISimulator
    {        
    }   
    

    public class MissionSimulator
    {
        //COSTANTI
        string ENVIRONMENTS_DIR = null;
        

        //Campi di classe
        public List<string> EnvPaths { get; set; }
        

        public MissionSimulator(string envsDir)
        {
            //inizializzo
            ENVIRONMENTS_DIR = envsDir;            

            //ottengo lista file ambienti di ricerca
            var envFiles = Directory.GetFiles(ENVIRONMENTS_DIR, "*.json", SearchOption.TopDirectoryOnly);

            //inizializzo
            EnvPaths = new List<string>(envFiles.Length);
            EnvPaths.AddRange(envFiles);

            //CONSOLE
            Console.WriteLine("Loaded Environment Files:");
            EnvPaths.ForEach(x => Console.WriteLine(x));
        }


        ///1- costruzione pool di istanze
        //SimulationInstancesPoolBuilder
        ///2- assegnamento istanze ai worker (impostazione limiti temporali
        //SimulationWorkersManager

       
    }

    public class SimulationInstancesPoolBuilder
    {
        #region Funzioni Distribuzione Probabilità

        static Func<SARGrid, SARGrid> generate_uniform_prior = delegate (SARGrid env)
        {
            foreach (var p in env._grid)
            {
                if (p.Type != SARPoint.PointTypes.Obstacle)
                {
                    p.Confidence = 0.5;//indecisione
                }
            }

            return env;
        };
        static Func<SARGrid, SARGrid> generate_kdist_prior = delegate (SARGrid env)
        {
            var rnd = new Random();

            foreach (var p in env._grid)
            {
                if (p.Type == SARPoint.PointTypes.Clear && p.Confidence == 0)//non impostato manualmente
                {
                    var r = ((double)rnd.Next(0, 3)) / 10;
                    p.Confidence = r;//aggiunta di rumore
                }
            }

            return env;
        };
        static Func<SARGrid, SARGrid> generate_uniform_danger = delegate (SARGrid env)
        {
            foreach (var p in env._grid)
            {
                if (p.Type != SARPoint.PointTypes.Obstacle)
                {
                    p.Danger = 0.5;//indecisione
                }
            }

            return env;
        };
        static Func<SARGrid, SARGrid> generate_kdist_danger = delegate (SARGrid env)
        {
            var rnd = new Random();

            foreach (var p in env._grid)
            {
                if (p.Type != SARPoint.PointTypes.Obstacle && p.Danger == 0)//non impostato manualmente
                {
                    var r = ((double)rnd.Next(0, 3)) / 10;
                    p.Danger = r;//aggiunta di rumore
                }
            }

            return env;            
        };
        
        #endregion

        int _instanceMolteplicy;
        List<string> _envsPaths;

        Dictionary<SimulationInstanceSchema.EnvironmentType, string> _envMap = new Dictionary<SimulationInstanceSchema.EnvironmentType, string>();
        Dictionary<SimulationInstanceSchema.PriorDistribution, Func<SARGrid, SARGrid>> _priorMap = new Dictionary<SimulationInstanceSchema.PriorDistribution, Func<SARGrid, SARGrid>>()
        {
            {SimulationInstanceSchema.PriorDistribution.Uniform, generate_uniform_prior },
            {SimulationInstanceSchema.PriorDistribution.KDistributed, generate_kdist_prior }
        };
        Dictionary<SimulationInstanceSchema.DangerDistribution, Func<SARGrid, SARGrid>> _dangerMap = new Dictionary<SimulationInstanceSchema.DangerDistribution, Func<SARGrid, SARGrid>>()
        {
            {SimulationInstanceSchema.DangerDistribution.Uniform, generate_uniform_danger },
            {SimulationInstanceSchema.DangerDistribution.KDistributed, generate_kdist_danger }
        };
        Dictionary<SimulationInstanceSchema.RiskPropensity, double> _riskMap = new Dictionary<SimulationInstanceSchema.RiskPropensity, double>()
        {
            {SimulationInstanceSchema.RiskPropensity.Safe, 0.8 },
            {SimulationInstanceSchema.RiskPropensity.Normal, 0.5 },
            {SimulationInstanceSchema.RiskPropensity.Risk, 0.2 }
        };

       
        public SimulationInstancesPoolBuilder(List<string> envsPaths, int instanceMolteplicy)
        {
            _instanceMolteplicy = instanceMolteplicy;
            _envsPaths = envsPaths;

            //inizializzo envMap
            _envMap.Add(SimulationInstanceSchema.EnvironmentType.Small, _envsPaths.Find(x => x.Contains("S")));
            _envMap.Add(SimulationInstanceSchema.EnvironmentType.Medium, _envsPaths.Find(x => x.Contains("M")));
            _envMap.Add(SimulationInstanceSchema.EnvironmentType.Large, _envsPaths.Find(x => x.Contains("L")));
        }

        public SimulationInstance BuildInstance(SimulationInstanceSchema schema)
        {
            //carico ambiente
            var env = new SARGrid(_envMap[schema.EnvType]);

            //genero prior
            env = _priorMap[schema.PriorDist].Invoke(env);

            //genero danger
            env = _dangerMap[schema.DangerDist].Invoke(env);

            //estraggo target
            env._realTarget = env.RandomizeTargetPosition(schema.TargetNum);

            //parametro rischio
            var riskParam = _riskMap[schema.RiskParam];

            //soglia pericolo
            var dangerThreshold = schema.DangerThreshold;

            //definisco nome istanza
            var instanceId = schema.GetID();

            //genero istanza
            var instance = new SimulationInstance(env, riskParam, dangerThreshold, instanceId);

            return instance;
        }

        public List<SimulationInstance> BuildInstancesPool()
        {
            decimal DANGER_THRESHOLD = (decimal) 0.2;
            int TARGET_NUM = 1;

            ///1- creo schemi istanze
            ///2- creo istanze
            ///

            //1- Pool di schemi
            var schemes = new List<SimulationInstanceSchema>();

            for (int iMolt = 0; iMolt < _instanceMolteplicy; iMolt++)
            {
                //ambienti
                foreach (var envType in _envMap.Keys)
                {                    

                    //distribuzioni prior
                    foreach (var priorType in _priorMap.Keys)
                    {
                        
                        //distribuzioni pericolo
                        foreach (var dangerType in _dangerMap.Keys)
                        {
                            
                            //comportamenti
                            foreach (var aParam in _riskMap.Keys)
                            {
                                var schema = new SimulationInstanceSchema(envType, priorType, dangerType, aParam, DANGER_THRESHOLD, TARGET_NUM);
                                schemes.Add(schema);
                            }
                        }
                    }
                }
            }

            //Debug
            Debug.WriteLine($"SCHEME POOL ({schemes.Count})");
            schemes.ForEach(x => Debug.WriteLine(x.GetID()));

            //2- Pool di istanze
            var instancesPool = new List<SimulationInstance>();
            //genero pool
            schemes.ForEach(x => instancesPool.Add(BuildInstance(x)));

            //Debug
            Debug.WriteLine($"INSTANCES POOL ({schemes.Count})");
            instancesPool.ForEach(x => Debug.WriteLine(x.ID));


            return instancesPool;
        }


        
    }

    public class SimulationInstanceSchema
    {
        public enum EnvironmentType { Small, Medium, Large };
        public enum PriorDistribution { Uniform, KDistributed };
        public enum DangerDistribution { Uniform, KDistributed };
        public enum RiskPropensity { Safe, Normal, Risk };
        
        //proprietà di una istanza
        public EnvironmentType EnvType { get; set; }
        public PriorDistribution PriorDist { get; set; }
        public DangerDistribution DangerDist { get; set; }
        public int TargetNum { get; set; }
        public RiskPropensity RiskParam { get; set; }
        public decimal DangerThreshold { get; set; }

        public SimulationInstanceSchema(EnvironmentType envType, PriorDistribution priorDist, DangerDistribution dangerDist, RiskPropensity aParam, decimal dangerThreshold, int targetNum = 1)
        {
            EnvType = envType;
            PriorDist = priorDist;
            DangerDist = dangerDist;
            TargetNum = targetNum;
            RiskParam = aParam;
            DangerThreshold = dangerThreshold;
        }

        internal string GetID()
        {
            var id = string.Empty;

            id = $"INSTANCE_" +
                $"{EnvType}_" +
                $"{PriorDist}_" +
                $"{DangerDist}_" +
                $"{TargetNum}_" +
                $"{RiskParam}_" +
                $"{DangerThreshold}";

            return id;
        }
    }

    public class SimulationInstance
    {
        //ID istanza
        string _instanceName;
        public string ID { get { return _instanceName; } }

        //parametri simulazione
        public SARGrid _env;
        double _riskParam;
        decimal _dangerThreshold;

        //default
        SARPoint _entryPoint; //(0,0)
        IUtilityFunction _utilityFunc; //(2, (1-a), a)
        ICostFunction _costFunc; //distanza manhattan
        IGoalSelectionStrategy _goalStrat; //utilità

        public SimulationInstance(SARGrid env, double riskParam, decimal dangerThreshold, string name)
        {
            //parametri simulazione
            _env = env;
            _riskParam = riskParam;
            _dangerThreshold = dangerThreshold;
            
            //default
            _entryPoint = _env.GetPoint(0, 0);
            _utilityFunc = new SARUtilityFunction(2, (1 - _riskParam), _riskParam);
            _costFunc = new SARCostFunction();
            _goalStrat = new SARGoalSelector();

            //assegno id
            _instanceName = name;
        }
             

        //passare parametri per inizializzazione di SARPlanner
        public ISARMission RunInstance(SimulationLogger logger)
        {
            ///inserire logger di riferimento
            ///
            //inizializzo pianificatore
            var planner = new SARPlanner(_env, _entryPoint, _utilityFunc, _costFunc, _goalStrat);

            //avvio simulazione

            //avvio clock
            var mission = planner.GenerateMission();
            //stop clock

            return mission;
        }
    }

    public class SimulationLogger
    {
        //Singleton


    }
}
