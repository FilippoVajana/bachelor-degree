using System;
using System.Collections.Generic;
using System.Linq;
using SARLib.SAREnvironment;
using SARLib.SARPlanner;
using System.IO;
using System.Diagnostics;
using SARLib.SARMission;
using SARLib.Toolbox;
using System.Threading;
using System.Threading.Tasks;

namespace SARSimulator
{

    public class MissionSimulator
    {
        //COSTANTI
        string ENVIRONMENTS_DIR = null;
        int INSTANCE_MULT;
        string LOGS_DIR = null;
        public static string RUN_LOG_DIR = null;
        public static bool VERBOSE_LOG = false;
        public static Dictionary<int, TimeSpan> SIM_MAX_DURATION = null;
            

        //Campi di classe
        public List<string> EnvPaths { get; set; }
        

        public MissionSimulator(string envsDir, int instanceMulteplicity, string logsDir, bool verboseLogging, int maxDuration)
        {
            //inizializzo
            ENVIRONMENTS_DIR = envsDir;
            INSTANCE_MULT = instanceMulteplicity;
            //cartella log per la run
            LOGS_DIR = Directory.CreateDirectory(Path.Combine(logsDir, $"{DateTime.Now.DayOfYear}")).FullName;
            var runLogFolder = $"{DateTime.Now.Hour}{DateTime.Now.Minute}";

            RUN_LOG_DIR = Directory.CreateDirectory(Path.Combine(LOGS_DIR, runLogFolder)).FullName;

            VERBOSE_LOG = verboseLogging;

            //inizializzo mappa per durate istanza
            SIM_MAX_DURATION = new Dictionary<int, TimeSpan>()
            {
                {10, TimeSpan.FromSeconds(maxDuration) },
                {25, TimeSpan.FromSeconds(maxDuration) },
                {50, TimeSpan.FromSeconds(maxDuration) }
            };

            //ottengo lista file ambienti di ricerca
            var envFiles = Directory.GetFiles(ENVIRONMENTS_DIR, "*.json", SearchOption.TopDirectoryOnly);

            //inizializzo
            EnvPaths = new List<string>(envFiles.Length);
            EnvPaths.AddRange(envFiles);

            //CONSOLE
            Console.WriteLine("Loaded Environment Files:");
            EnvPaths.ForEach(x => Console.WriteLine(x));            
        }

        public void StartSimulation()
        {
            //avvio simulazione
            var instancePool = RunSimulationPreparation();

            //avvio le istanze
            var worker = new SimulationWorkersManager(1, instancePool);
            worker.RunSimulationInstances();

            //estrazione sommario simulazione
            SimulationLogger.ExtractSimulationResult(RUN_LOG_DIR);
        }

        private List<SimulationInstance> RunSimulationPreparation()
        {
            Console.WriteLine("BUILDING INSTANCES POOL . . .");
            //costruisco pool di istanze
            var instancePool = new SimulationInstancesPoolBuilder(EnvPaths, INSTANCE_MULT).BuildInstancesPool();
            Console.WriteLine($"BUILT {instancePool?.Count} INSTANCES");

            return instancePool;            
        }
        
        class SimulationWorkersManager
        {
            //int THREAD_NUM = Environment.ProcessorCount;
            int THREAD_NUM = 2;

            List<SimulationInstance> INSTANCE_POOL = new List<SimulationInstance>();
            Queue<SimulationInstance> INSTANCE_QUEUE = null;

            public SimulationWorkersManager(int threadNum, List<SimulationInstance> pool)
            {
                //THREAD_NUM = threadNum;
                INSTANCE_POOL = pool;
                INSTANCE_QUEUE = new Queue<SimulationInstance>();
            }

            public void RunSimulationInstances()
            {           
                FillWorkingQueue();

                var etaS = (SIM_MAX_DURATION[10].TotalSeconds * INSTANCE_QUEUE.Where(x => x._env._numCol == 10).Count());
                var etaM = (SIM_MAX_DURATION[25].TotalSeconds * INSTANCE_QUEUE.Where(x => x._env._numCol == 25).Count());
                var etaL = (SIM_MAX_DURATION[50].TotalSeconds * INSTANCE_QUEUE.Where(x => x._env._numCol == 50).Count());
                var etaTot = (etaS + etaM + etaL) / THREAD_NUM;

                var tLimS = SIM_MAX_DURATION[10].TotalSeconds;
                var tLimM = SIM_MAX_DURATION[25].TotalSeconds;
                var tLimL = SIM_MAX_DURATION[50].TotalSeconds;


                Console.WriteLine($"STARTING SIMULATION: {Environment.NewLine}" +
                    $"THREADS: {THREAD_NUM}{Environment.NewLine}" +
                    $"TIME THRESHOLD: {tLimS}sec.[S]  {tLimM}sec.[M]  {tLimL}sec.[L] {Environment.NewLine}" +
                    $"MAX DURATION: {etaTot} sec. [{TimeSpan.FromSeconds(etaTot).TotalMinutes} min.]{Environment.NewLine}");
                Console.WriteLine("Press Enter");
                Console.ReadKey();


                RunWorkingQueue();

                Thread.Sleep(100);
            }

            //rimpimento code di lavoro
            private void FillWorkingQueue()
            {
                INSTANCE_POOL.ForEach(x => INSTANCE_QUEUE.Enqueue(x));
            }

            //elaborazione istanze
            private void RunWorkingQueue()
            {
                var instanceQueueLength = INSTANCE_QUEUE.Count;
                var simStart = DateTime.Now;
                var runningTask = new List<Task>();
                //var runningTask = new Task[THREAD_NUM];

                while (INSTANCE_QUEUE.Count > 0)
                {
                    // var runningTask = new Task[THREAD_NUM];
                    var rT = runningTask.Count;
                    for (int i = 0; i < THREAD_NUM - rT; i++)
                    {                      
                        //creo task
                        var instance = INSTANCE_QUEUE.Dequeue();
                                                
                        var task = instance.RunInstanceAsync(SIM_MAX_DURATION[instance._env._numCol]);

                        //aggiungo alla coda di elaborazione
                        //runningTask[i] = task;
                        runningTask.Add(task);
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("RUNNING_TASK_QUEUE_LENGTH: " + runningTask.Count);
                        Console.ForegroundColor = ConsoleColor.Gray;

                        //Thread.Sleep(100);
                    }

                    //attendo completamento tasks
                    int index = Task.WaitAny(runningTask.ToArray());
                    runningTask.RemoveAt(index);

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("INSTANCE_QUEUE_LENGTH: " + INSTANCE_QUEUE.Count);
                    Console.WriteLine("RUNNING_TASK_QUEUE_LENGTH: " + runningTask.Count);
                    Console.ForegroundColor = ConsoleColor.Gray;

                    //Thread.Sleep(100);
                }

                Task.WaitAll(runningTask.ToArray(), (int)(runningTask.Count * SIM_MAX_DURATION[50].TotalMilliseconds));

                var simEnd = DateTime.Now;
                var simDuration = TimeSpan.FromTicks(simEnd.Ticks - simStart.Ticks);
                Console.WriteLine($"SIMULATION DURATION: {simEnd.Subtract(simStart).TotalMinutes}");
                //creazione file dettagli simulazione
                var simDetails = "THREAD_NUM MAX_INSTANCE_DURATION SIM_DURATION SIM_INSTANCE_NUM" + Environment.NewLine;
                simDetails += $"{THREAD_NUM} {SIM_MAX_DURATION[10]} {simDuration} {instanceQueueLength}";
                var simDetailSW = File.CreateText(Path.Combine(RUN_LOG_DIR, "simulation_details.txt"));
                simDetailSW.AutoFlush = true;
                simDetailSW.WriteLine(simDetails);
                simDetailSW.Dispose();
            }
        }

    }

    public class SimulationInstance
    {
        //ID istanza
        string _instanceName;
        public string ID { get { return _instanceName; } }
        //ID molteplicità
        public int MID { get; set; }

        //parametri simulazione
        public SARGrid _env;
        double _riskParam;
        decimal _dangerThreshold;

        //default
        SARPoint _entryPoint; //(0,0)
        IUtilityFunction _utilityFunc; //(2, (1-a), a)
        ICostFunction _costFunc; //distanza manhattan
        IGoalSelectionStrategy _goalStrat; //utilità

        public SimulationInstance(SARGrid env, double riskParam, decimal dangerThreshold, string name, int mID)
        {
            //parametri simulazione
            _env = env;
            _riskParam = riskParam;
            _dangerThreshold = dangerThreshold;

            //default
            _entryPoint = _env.GetPoint(0, 0);
            //_utilityFunc = new SARUtilityFunction(2, (1 - _riskParam), _riskParam);
            _utilityFunc = new SARUtilityFunction_Test_NoArea(0, (1 - _riskParam), _riskParam); //utilità modificata
            _costFunc = new SARCostFunction();
            _goalStrat = new SARGoalSelector();

            //assegno id
            _instanceName = name;
            MID = mID;
        }


        //passare parametri per inizializzazione di SARPlanner
        public async Task<ISARMission> RunInstanceAsync(TimeSpan maxDuration)
        {
            //inizializzo pianificatore
            var planner = new SARPlanner(_env, _entryPoint, _dangerThreshold, _utilityFunc, _costFunc, _goalStrat);

            //inizializzo logger             
            var instanceLogger = planner.SetupLogger(this.ID, this.MID, MissionSimulator.RUN_LOG_DIR, MissionSimulator.VERBOSE_LOG);

            //Thread.Sleep(10);
            //avvio simulazione   
            
            var simTaskResult = await Task.Run(() => { return planner.GenerateMission(maxDuration); });

            //Thread.Sleep(100);
            await Task.Run(() => { instanceLogger.SaveLogs(); });

            Thread.Sleep(100);

            return simTaskResult;
        }
    }

    public class SimulationInstancesPoolBuilder
    {
        #region Funzioni Distribuzione Probabilità
        const int TARGET_POINTS = 4;
        const int PROB_EXPANSION_RADIUS = 4;

        static Func<SARGrid, int, List<SARPoint>> get_random_points = delegate (SARGrid env, int pointsNum)
        {
            var list = new List<SARPoint>(pointsNum);
            var rnd = new Random();
            var col = env._numCol - 1;
            var row = env._numRow - 1;
            int pc = pointsNum;

            while (pc > 0)
            {
                var x = rnd.Next(0, col);
                var y = rnd.Next(0, row);
                var p = env.GetPoint(x, y);

                if (p.Type != SARPoint.PointTypes.Obstacle)
                {
                    list.Add(p);
                    pc--;
                }                
            }

            return list;
        };
        static Func<SARGrid, SARPoint, int, SARGrid> expand_probability = delegate (SARGrid env, SARPoint point, int radius)
        {
            var border = new HashSet<SARPoint>() { point };
            var expArea = new HashSet<SARPoint>();

            //genero la frontiera
            while (radius > 0)
            {
                var borderNear = new HashSet<SARPoint>();
                foreach (var p in border)
                {
                    var neighbors = (SARPoint[]) env.GetNeighbors(p);
                    foreach (var pNear in neighbors)
                    {                        
                        borderNear.Add(pNear);
                        expArea.Add(pNear);
                    }
                }
                radius--;
                border = borderNear;
            }

            //assegno probabilità
            foreach (var p in expArea)
            {
                p.Confidence = point.Confidence / (1 + env.Distance(point, p));
                p.Danger = point.Danger / (1 + env.Distance(point, p));
            }
            
            return env;
        };

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
            //ottengo k punti
            var points = get_random_points(env, TARGET_POINTS);

            //assegno probabilità
            var rnd = new Random();
            foreach (var p in points)
            {
                var conf = ((double) rnd.Next(1, 11)) / 10;
                p.Confidence = conf;

                //diffondo probabilità
                env = expand_probability(env, p, PROB_EXPANSION_RADIUS);
            }
            
            //aggiorno lista possibili target
            env._estimatedTargetPositions = env.GetPossibleTargetPositions();

            Thread.Sleep(10);
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
            //ottengo k punti
            var points = get_random_points(env, env._numCol);

            //assegno probabilità
            var rnd = new Random();
            foreach (var p in points)
            {
                var danger = ((double)rnd.Next(1, 11)) / 10;
                p.Danger = danger;

                //diffondo probabilità
                env = expand_probability(env, p, PROB_EXPANSION_RADIUS);
            }
            
            return env;
        };
        
        #endregion

        int _instanceMulteplicy;
        List<string> _envsPaths;

        #region Mappe parametri

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
            {SimulationInstanceSchema.RiskPropensity.Safe, 0.2 },
            {SimulationInstanceSchema.RiskPropensity.Normal, 0.5 },
            {SimulationInstanceSchema.RiskPropensity.Risk, 0.8 }
        };

        #endregion

        public SimulationInstancesPoolBuilder(List<string> envsPaths, int instanceMolteplicy)
        {
            _instanceMulteplicy = instanceMolteplicy;
            _envsPaths = envsPaths;

            //inizializzo envMap
            _envMap.Add(SimulationInstanceSchema.EnvironmentType.Small, _envsPaths.Find(x => x.Contains("S")));
            _envMap.Add(SimulationInstanceSchema.EnvironmentType.Medium, _envsPaths.Find(x => x.Contains("M")));
            _envMap.Add(SimulationInstanceSchema.EnvironmentType.Large, _envsPaths.Find(x => x.Contains("L")));
        }

        public SimulationInstance BuildInstance(SimulationInstanceSchema schema, int mID)
        {
            //carico ambiente
            SARGrid env;
            try
            {
                env = new SARGrid(_envMap[schema.EnvType]);
            }
            catch (Exception)
            {
                return null;
            }

            //genero prior
            env = _priorMap[schema.PriorDist].Invoke(env);

            //genero danger
            env = _dangerMap[schema.DangerDist].Invoke(env);

            //normalizzo la prior
            new SARLib.Toolbox.BayesEngine.BayesFilter(0, 0).NormalizeConfidence(env);

            //estraggo target
            env._realTarget = env.RandomizeTargetPosition(schema.TargetNum);
            
            //parametro rischio
            var riskParam = _riskMap[schema.RiskParam];

            //soglia pericolo
            var dangerThreshold = schema.DangerThreshold;

            //definisco nome istanza
            var instanceId = schema.GetID();

            //definisco molteplicità
            var instanceMID = mID;

            //genero istanza
            var instance = new SimulationInstance(env, riskParam, dangerThreshold, instanceId, instanceMID);

            return instance;
        }

        public List<SimulationInstance> BuildInstancesPool()
        {
            decimal DANGER_THRESHOLD = (decimal) 0.8;
            int TARGET_NUM = 1;

            //1- Pool di schemi
            var schemes = new List<SimulationInstanceSchema>();
            //2- Pool di istanze
            var instancesPool = new List<SimulationInstance>();

            for (int iMolt = 0; iMolt < _instanceMulteplicy; iMolt++)
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
                                //costruisco schema
                                var schema = new SimulationInstanceSchema(envType, priorType, dangerType, aParam, DANGER_THRESHOLD, TARGET_NUM);
                                
                                //costruisco istanza
                                var instance = BuildInstance(schema, iMolt);
                                if (instance != null)
                                {
                                    instancesPool.Add(instance);
                                }



                                schemes.Add(schema);
                            }
                        }
                    }
                }
            }

            //Debug
            Debug.WriteLine($"SCHEME POOL ({schemes.Count})");
            schemes.ForEach(x => Debug.WriteLine(x?.GetID()));

            //2- Pool di istanze
            //var instancesPool = new List<SimulationInstance>();
            //genero pool
            //schemes.ForEach(x => instancesPool.Add(BuildInstance(x)));
            //foreach (var s in schemes)
            //{
            //    var instance = BuildInstance(s);
            //    if (instance != null)
            //    {                    
            //        instancesPool.Add(instance);
            //    }
            //}

            //Debug
            Debug.WriteLine($"INSTANCES POOL ({schemes.Count})");
            instancesPool.ForEach(x => Debug.WriteLine(x?.ID + " " + x?.MID));


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

        
}
