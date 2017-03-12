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
    /// Il costo viene valutato tenendo conto della distanza in norma Manhattan
    /// e del livello di pericolo
    /// </summary>
    public class SARCostFunction : ICostFunction
    {
        const int DANGER_MAGNIFIER = 10;

        public double EvaluateCost(SARPoint point, SARPoint goal)
        {
            var cost = Math.Abs(point.X - goal.X) + Math.Abs(point.Y - goal.Y) + (point.Danger * DANGER_MAGNIFIER);
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
            if (currentPos.X == point.X && currentPos.Y == point.Y)
            {
                return 0;
            }
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

            utility = CR * (Math.Pow(1 + 1/(L * DR), _dExp));

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


    //public interface ISARRoute
    //{
    //    List<SARPoint> Route { get; set; }
    //    //string SaveToFile(string destination);
    //    //ISARPlan LoadFromFile(string source);
    //}
    //public class SARRoute : ISARRoute
    //{
    //    public List<SARPoint> Route { get; set; }
    //    public SARRoute(List<SARPoint> route)
    //    {
    //        Route = route;
    //    }
    //}

    
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

        //costruttore
        public SARPlanner(SARGrid environment, SARPoint entryPoint, IUtilityFunction utilityFunc, ICostFunction costFunc, IGoalSelectionStrategy strategy)
        {
            _environment = environment;
            _start = entryPoint;
            _utilityFunc = utilityFunc;
            _costFunc = costFunc;
            _strategy = strategy;
            
            //_goals = environment._estimatedTargetPositions;
        }
        
        //costanti per filtro Bayes
        const double FILTER_FALSENEG_RATIO = 0.2;
        const double FILTER_FALSEPOS_RATIO = 0.2;
        

        public ISARMission GenerateMission()
        {            
            //imposto punto critici
            var currentPos = _start;
            var targetPos = _environment._realTarget;

            //inizializzo moduli base
            var selector = new GoalSelector(_environment, _utilityFunc, _strategy);
            var planner = new RoutePlanner(_environment, _costFunc);
            var runner = new PlanRunner();
            var updater = new EnvironmentUpdater(FILTER_FALSENEG_RATIO, FILTER_FALSEPOS_RATIO);

            //Parametri Log//
            List<SARPoint> selectedGoals = new List<SARPoint>();//lista dei goals selezionati durante la ricerca
            List<double> dangerLevelsHistory = new List<double>() { currentPos.Danger};//storico del livello di rischio rilevato durante la ricerca
            ////

            //inizializzazione prior di inizio
            new BayesEngine.BayesFilter(0,0).NormalizeConfidence(_environment);                    
            _environment = updater.UpdateEnvironmentConfidence(_environment, currentPos);

            //inizializzo missione
            var mission = new SARMission.SARMission(_environment, new List<SARPoint>(), currentPos);
            mission.Route.Add(currentPos); //aggiungo posizione iniziale    
            
            

            //ciclo generazione 
            while ((currentPos.X != targetPos.X) && (currentPos.Y != targetPos.Y))
            {
                //SELEZIONE GOAL
                //var selector = new GoalSelector(_environment, _utilityFunc, _strategy);
                var currentGoal = selector.SelectGoal(currentPos);
                selectedGoals.Add(currentGoal); //LOG

                //PIANIFICAZIONE PERCORSO
                //var planner = new RoutePlanner(_environment, _costFunc);
                var currentPlan = planner.ComputeRoute(currentPos, currentGoal);

                //controllo che sia stato trovato un percorso
                if (currentPlan.Count == 0)
                {
                    return mission;
                }

                //ESECUZIONE STEP PERCORSO
                //var runner = new PlanRunner();
                currentPos = runner.ExecutePlan(currentPlan);
                dangerLevelsHistory.Add(currentPos.Danger); //LOG

                //AGGIORNAMENTO ROUTE MISSIONE
                mission.Route.Add(currentPos);

                //AGGIORNAMENTO PRIOR AMBIENTE             
                //var updater = new EnvironmentUpdater(FILTER_FALSENEG_RATIO, FILTER_FALSEPOS_RATIO);
                _environment = updater.UpdateEnvironmentConfidence(_environment, currentPos);                
            }

            return mission;
        }
        
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
        decimal _dangerThreshold = 1;
        ISARSearchAlgoritm _algoritm;

        //public RoutePlanner(SARGrid environment, ICostFunction costFunction)
        //{
        //    _env = environment;
        //    _cost = costFunction;
        //    _algoritm = new AStar(_env, _cost, _dangerThreshold);
        //}

        public RoutePlanner(SARGrid environment, ICostFunction costFunction, decimal dangerThreshold = 1)
        {            
            _env = environment;
            _cost = costFunction;
            _dangerThreshold = dangerThreshold;
            _algoritm = new AStar(_env, _cost, _dangerThreshold);
        }

        public List<SARPoint> ComputeRoute(SARPoint currentPosition, SARPoint goalPosition)
        {
            //inizializzo modulo ricerca percorso
            ISARPathFinder pathFinder = new PathFinder(_algoritm, _env, _cost, _dangerThreshold);
            
            //inizializzo route
            var route = new List<SARPoint>();

            //applicazione di A*
            route = pathFinder.FindRoute(currentPosition, goalPosition);

            return route;
        }        
    }

    /// <summary>
    /// Esecutore per un piano di movimento 
    /// </summary>
    public class PlanRunner
    {      
        /// <summary>
        /// Esegue il primo passo del percorso pianificato
        /// </summary>
        /// <param name="route"></param>
        /// <returns></returns>
        public SARPoint ExecutePlan(List<SARPoint> route)
        {
            var position = route[1]; //prendo la posizione [1] poichè [0] è la posizione corrente

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
            _bayesFilter = new BayesEngine.BayesFilter(falseNegRatio, falsePosRatio);
        }

        public SARGrid UpdateEnvironmentConfidence(SARGrid environment, IPoint sensePoint)
        {
            //aggiorno distribuzione di probabilità
            var updatedGrid = _bayesFilter.UpdateEnvironmentConfidence(environment, sensePoint, -1);

            return updatedGrid;
        }
    }    
}
