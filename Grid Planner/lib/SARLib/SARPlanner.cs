using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SARLib.SAREnvironment;
using SARLib.SARMission;
using SARLib.Toolbox;

namespace SARLib.SARPlanner
{

    #region Funzione di costo
    /// <summary>
    /// Funzione costo del percorso
    /// </summary>
    public interface ICostFunction
    {
        double EvaluateCost(SARPoint point, SARPoint goal);
    }
    /// <summary>
    /// Funzione di costo per applicazioni SAR.
    /// Il costo viene valutato tenendo conto della sola distanza in norma Manhattan
    /// </summary>
    public class SARCostFunction : ICostFunction
    {
        public double EvaluateCost(SARPoint point, SARPoint goal)
        {
            var cost = Math.Abs(point.X - goal.X) + Math.Abs(point.Y - goal.Y);
            return cost;
        }
    }
    #endregion

    #region Funzione di utilità

    /// <summary>
    /// Valuta l'appetibilità a priori di una posizione
    /// </summary>
    public interface IUtilityFunction
    {
        double ComputeUtility(SARPoint envPoint, SARPoint currentPoint, SARGrid environment);
    }
    /// <summary>
    /// Funzione di utilità per applicazioni SAR.
    /// L'utilità viene calcolata tenendo conto del rapporto Conf/Danger
    /// nell'intorno del punto
    /// </summary>
    public class SARUtilityFunction : IUtilityFunction
    {
        int _radius = 1;
        double _dExp;
        double _cExp;
        Func<IPoint, IPoint, double> manhattan_distance = delegate (IPoint a, IPoint b)
        {
            var distance = Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
            return distance;
        };

        public SARUtilityFunction(int evaluationRadius, double dangerExp, double confidenceExp)
        {
            _radius = evaluationRadius;
            _dExp = dangerExp;
            _cExp = confidenceExp;
        }

        //modificare rendendo parametrico rispetto alla formula per il calcolo della utilità
        public double ComputeUtility(SARPoint point, SARPoint currentPos, SARGrid environment)
        {
            //Creazione set per i nodi considerati nella valutazione
            HashSet<SARPoint> evalNodes = new HashSet<SARPoint>();

            double utility = 0;

            //scansione l'area circostante
            var nodeToScan = new HashSet<SARPoint>() { point }; //nodi da espandere
            var nodeScanned = new HashSet<SARPoint>();
            while (nodeToScan.Count > 0)
            {
                var n = nodeToScan.First();
                nodeToScan.Remove(n);
                nodeScanned.Add(n);

                //ottengo nodi limitrofi
                var neighbors = environment.GetNeighbors(n);

                //aggiorno set di nodi da valutare
                foreach (var near in neighbors)
                {
                    if (environment.Distance(near, point) <= _radius) //limito espansione della frontiera
                    {
                        evalNodes.Add(near as SARPoint);
                        if (!nodeScanned.Contains(near))
                            nodeToScan.Add(near as SARPoint);
                    }
                }

                //decremento contatore
                //_radius--;
            }

            //calcolo parametri funzione di utilità
            double DR = 0;
            double CR = 0;
            int Area = evalNodes.Count;
            double L = manhattan_distance(currentPos, point);

            foreach (var node in evalNodes)
            {
                DR += node.Danger;
                CR += node.Confidence;
            }

            DR = DR / Area;
            CR = Math.Pow(CR / Area, _cExp);

            utility = CR * (Math.Pow(1 + L * DR, _dExp));

            return (!double.IsNaN(utility))? utility : 0;
        }
    }

    #endregion

    #region Selezione Goal

    /// <summary>
    /// Strategia per la selezione del prossimo goal
    /// sulla base della mappa di utilità
    /// </summary>
    public interface IGoalSelectionStrategy
    {
        SARPoint SelectNextTarget(Dictionary<SARPoint, double> utilityMap);
    }

    public class SARGoalSelector : IGoalSelectionStrategy
    {
        //seleziono la cella con valore di utilità massimo
        public SARPoint SelectNextTarget(Dictionary<SARPoint, double> utilityMap)
        {
            var orderedMap = utilityMap.OrderByDescending(e => e.Value);
            return orderedMap.First(e => e.Value != double.NaN).Key;
        }
    }

    #endregion


    public interface ISARRoute
    {
        List<SARPoint> Route { get; set; }
        //string SaveToFile(string destination);
        //ISARPlan LoadFromFile(string source);
    }
    public class SARRoute : ISARRoute
    {
        public List<SARPoint> Route { get; set; }
        public SARRoute(List<SARPoint> route)
        {
            Route = route;
        }
    }

    
    ///Processo calcolo Route alla ricerca del target
    ///1) seleziono prossima posizione candidata (SELECTOR)
    ///2) calcolo percorso fino al goal (ROUTE PLANNER)
    ///3) eseguo la prima mossa (PLAN RUNNER)
    ///4) lettura sensoriale + aggiornamento probabilità (ENVIRONMENT UPDATER)
    ///5) ripeto dal passo 1 (controllo invarianza goal --> salto pianificazione)

    //Pianificatore
    public interface ISARMissionPlanner
    {
        ISARMission GenerateMission();
    }

    public class SARPlanner : ISARMissionPlanner
    {
        //campi per setup pianificatore        
        public SARGrid _environment;
        public SARPoint _start;
        public IUtilityFunction _utilityFunc;
        public ICostFunction _costFunc;
        public IGoalSelectionStrategy _strategy;

        //campi per creazione SARMission
        public ISARRoute _route;
        public List<IPoint> _goals; //posizione dei target reali

        //costruttore
        public SARPlanner(SARGrid environment, SARPoint entryPoint, IUtilityFunction utilityFunc, ICostFunction costFunc, IGoalSelectionStrategy strategy)
        {
            _environment = environment;
            _start = entryPoint;
            _utilityFunc = utilityFunc;
            _costFunc = costFunc;
            _strategy = strategy;

            _route = null;
            _goals = environment._realTargets;
        }

        public ISARMission GenerateMission()
        {
            //imposto punto di partenza
            var currentPos = _start;

            //inizializzo missione
            var mission = new SARMission.SARMission();
            mission.Route.Route.Add(currentPos);            

            //ciclo generazione 
            while ((currentPos.X != _goals.First().X) && (currentPos.Y != _goals.First().Y))
            {
                //seleziono candidato goal
                var selector = new GoalSelector(_environment, _utilityFunc, _strategy);
                var currentGoal = selector.SelectGoal(currentPos);

                //pianifico percorso
                var planner = new RoutePlanner(_environment, _costFunc);
                var currentPlan = planner.ComputeRoute(currentPos, currentGoal);

                //eseguo il piano
                var runner = new PlanRunner();
                currentPos = runner.ExecutePlan(currentPlan);

                //aggiorno ambiente
                const double FILTER_FALSENEG_RATIO = 0.2;
                const double FILTER_FALSEPOS_RATIO = 0.2;
                var updater = new EnvironmentUpdater(FILTER_FALSENEG_RATIO, FILTER_FALSEPOS_RATIO);
                _environment = updater.UpdateEnvironmentConfidence(_environment, currentPos);

                //aggiorno piano missione
                mission.Route.Route.Add(currentPos);
            }

            return mission;
        }

        ///// <summary>
        ///// Rappresenta un nodo del grafo usato per l'esplorazione
        ///// </summary>
        //protected class Node //rivedere in relazione ad A*
        //{
        //    public SARPoint point;
        //    public double GCost { get; set; } //costo dall'origine
        //    public double FCost { get; set; } //costo aggregato g + h

        //    public Node(SARPoint sarPoint)
        //    {
        //        this.point = sarPoint;
        //        GCost = double.MaxValue;
        //        FCost = 0;
        //    }
        //}
    }

    /// <summary>
    /// Selezionatore per posizione candidata a goal
    /// in base alla mappa dei valori di utilità
    /// </summary>
    class GoalSelector
    {
        private SARGrid _env;
        private IUtilityFunction _util;
        private IGoalSelectionStrategy _strategy;
        private Dictionary<SARPoint, double> _utilMap;

        public GoalSelector(SARGrid environment, IUtilityFunction utilityFunction, IGoalSelectionStrategy selectionStrategy)
        {
            _env = environment;
            _util = utilityFunction;
            _strategy = selectionStrategy;
            _utilMap = new Dictionary<SARPoint, double>(environment._grid.Length);
        }

        public SARPoint SelectGoal(SARPoint currentPos)
        {
            //costruisco la mappa utilità dell'ambiente
            _utilMap = BuildUtilityMap(currentPos);
            //seleziono il punto con utilità massima
            var goal = _strategy.SelectNextTarget(_utilMap);

            return goal;
        }

        private Dictionary<SARPoint, double> BuildUtilityMap(SARPoint currentPos)
        {
            Dictionary<SARPoint, double> map = new Dictionary<SARPoint, double>(_env._grid.Length);

            //calcolo valore di utilità delle celle
            foreach (var point in _env._grid)
            {
                var pUtility = _util.ComputeUtility(point, currentPos, _env);
                map.Add(point, pUtility);
            }

            return map;
        }
    }
    
    /// <summary>
    /// Generatore per il percorso ottimo fino al goal
    /// </summary>
    public class RoutePlanner
    {
        SARGrid _env;
        ICostFunction _cost;
        List<SARPoint> _route;        
                
        public RoutePlanner(SARGrid environment, ICostFunction costFunction)
        {
            _env = environment;
            _cost = costFunction;
        }        

        public SARRoute ComputeRoute(SARPoint currentPosition, SARPoint goalPosition)
        {
            //inizializzo modulo ricerca percorso
            ISARPathFinder pathFinder = new PathFinder(new AStar(), _env, _cost);
            
            //inizializzo route
            _route = new List<SARPoint>();

            //applicazione di A*
            _route = pathFinder.FindRoute(currentPosition, goalPosition);

            return new SARRoute(_route);
        }        
    }

    /// <summary>
    /// Esecutore per un piano di movimento 
    /// </summary>
    class PlanRunner
    {      
        /// <summary>
        /// Esegue il primo passo del percorso pianificato
        /// </summary>
        /// <param name="route"></param>
        /// <returns></returns>
        public SARPoint ExecutePlan(ISARRoute route)
        {
            var position = route.Route.First();

            return position;
        }        
    }

    /// <summary>
    /// Updater per la distribuzione di probabilità del parametro Confidence
    /// nel'ambiente di ricerca
    /// </summary>
    class EnvironmentUpdater
    {
        Toolbox.BayesEngine.BayesFilter _bayesFilter;        

        public EnvironmentUpdater(double falseNegRatio, double falsePosRatio)
        {
            _bayesFilter = new Toolbox.BayesEngine.BayesFilter(falseNegRatio, falsePosRatio);
        }

        public SARGrid UpdateEnvironmentConfidence(SARGrid environment, IPoint sensePoint)
        {
            //aggiorno distribuzione di probabilità
            var updatedGrid = _bayesFilter.UpdateEnvironmentConfidence(environment, sensePoint, -1);

            return updatedGrid;
        }
    }



    public class SearchLogger
    {

        public string SaveToFile(string logsDirPath)
        {
            //definisco nome log
            string logName = $"{GetType().Name}_{DateTime.Now.ToUniversalTime()}";

            //serializzo l'oggetto log
            string json = JsonConvert.SerializeObject(Log);

            //salvo il log
            string fullPath = $"{logsDirPath}\\{logName}";
            var logFile = File.CreateText(fullPath);

            return fullPath;
        }
        public SearchLog Log { get; }

        public class SearchLog
        {
            internal SearchLog()
            { }

            private double totalStep;
            private double totalTime;
            private int totalOpenNodes;
        }
    }    
}
