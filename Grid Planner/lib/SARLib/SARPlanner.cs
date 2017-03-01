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
    /// <summary>
    /// Funzione costo del percorso
    /// </summary>
    public interface ICostFunction
    {
        double EvaluateCost(SARPoint point, SARPoint goal);
    }

    /// <summary>
    /// Valuta l'appetibilità a priori di una posizione
    /// </summary>
    public interface IUtilityFunction
    {
        double ComputeUtility(SARPoint point);
    }

    /// <summary>
    /// Strategia per la selezione del prossimo goal
    /// sulla base della mappa di utilità
    /// </summary>
    public interface IGoalSelectionStrategy
    {
        SARPoint SelectNextTarget(Dictionary<SARPoint, double> utilityMap);
    }

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
        public IPoint _start;
        public IUtilityFunction _utilityFunc;
        public ICostFunction _costFunc;
        public IGoalSelectionStrategy _strategy;

        //campi per creazione SARMission
        public ISARRoute _route;
        public List<IPoint> _goals;

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
            throw new NotImplementedException();
        }

        /// <summary>
        /// Rappresenta un nodo del grafo usato per l'esplorazione
        /// </summary>
        protected class Node //rivedere in relazione ad A*
        {
            public SARPoint point;
            public double GCost { get; set; } //costo dall'origine
            public double FCost { get; set; } //costo aggregato g + h

            public Node(SARPoint sarPoint)
            {
                this.point = sarPoint;
                GCost = double.MaxValue;
                FCost = 0;
            }
        }
    }

    /// <summary>
    /// Selezionatore per posizione candidata a goal
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

        public SARPoint SelectGoal()
        {
            //costruisco la mappa utilità dell'ambiente
            _utilMap = BuildUtilityMap();
            //seleziono il punto con utilità massima
            var goal = _strategy.SelectNextTarget(_utilMap);

            return goal;
        }

        private Dictionary<SARPoint, double> BuildUtilityMap()
        {
            Dictionary<SARPoint, double> map = new Dictionary<SARPoint, double>(_env._grid.Length);

            //calcolo valore di utilità delle celle
            foreach (var point in _env._grid)
            {
                var pUtility = _util.ComputeUtility(point);
                map.Add(point, pUtility);
            }

            return map;
        }
    }
    
    /// <summary>
    /// Generatore per il percorso ottimo fino al goal
    /// </summary>
    class RoutePlanner
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
        Toolbox.BayesEngine.Logger _filterLogger;

        public EnvironmentUpdater(double bayesErrorRate)
        {
            _filterLogger = null; //rivedere
            _bayesFilter = new Toolbox.BayesEngine.BayesFilter(bayesErrorRate, _filterLogger);
        }

        public SARGrid UpdateEnvironmentConfidence(SARGrid environment, IPoint sensingPoint)
        {
            //aggiorno distribuzione di probabilità
            var updatedGrid = _bayesFilter.UpdateConfidence(environment, sensingPoint);

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
