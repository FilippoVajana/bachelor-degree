using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SARLib.SAREnvironment;
using SARLib.SARMission;

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


    #region PLAN

    public interface ISARRoute
    {
        List<SARPoint> Route { get; set; }
        //string SaveToFile(string destination);
        //ISARPlan LoadFromFile(string source);
    }
    public class SARRoute : ISARRoute
    {
        public List<SARPoint> Route { get; set; }
    }

    #endregion

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
        protected class Node
        {
            public IPoint point;
            public double GCost { get; set; } //costo dall'origine
            public double FCost { get; set; } //costo aggregato g + h

            public Node(IPoint point)
            {
                this.point = point;
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
    
    class RoutePlanner
    {

    }

    class PlanRunner
    { }
    class EnvironmentUpdater
    { }

    
    /// <summary>
    /// La pianificazione si suddivide nelle seguenti fasi:
    /// 1- Analisi dell'ambiente (SARGrid) e selezione del goal
    ///     1.1- Implementare Funzione Utilità U() per la stima della qualità di un punto (SARPoint)
    ///          in base ai livelli Danger e Confidence nell'intorno
    ///     1.2- Creazione mappa di utilità (metodi per logging)
    ///     1.3- Routine di selezione punto "migliore"
    ///     1.4- Routine di aggiornamento della mappa
    /// 2- Applicare A* per trovare il percorso ottimo dalla posizione attuale fino al goal
    /// </summary>
    

    //cambia nome
    public class SARMissionPlanner : SARPlanner
    {
        /// <summary>
        /// Generazione di una missione per il raggiungimento del goal
        /// </summary>
        /// <returns></returns>
        public override ISARMission GenerateMission() //Rivedere implementazione
        {
            ///PIANIFICATORE
            ///1) creazione mappa valore utilità delle celle (funzione utilità sulla griglia)
            ///2) selezione del goal (massima utilità)
            ///3) calcolo del percorso dalla posizione attuale al goal tramite A*          

            //adapter
            var _currentPos = _start;

            //1) creazione mappa di utilità
            SortedDictionary<SARPoint, double> _utilityMap = new SortedDictionary<SARPoint, double>();
            BuildUtilityMap(_utilityMap, _utilityFunc);

            //2) seleziono il goal
            var _currentGoal = _strategy.SelectNextTarget(_utilityMap);

            //3) calcolo del percorso per il goal attuale
            List<IPoint> route = FindRoute(_currentPos, _currentGoal);

            //conversione in SARRoute
            var sarRoute = new SARRoute();
            foreach (var p in route)
            {
                sarRoute.Route.Add(_environment.GetPoint(p.X, p.Y));
            }

            //genero missione
            var mission = new SARMission.SARMission(_environment, sarRoute, _start);

            return mission;
        }

        /// <summary>
        /// Routine di aggiornamento della mappa di utilità relativa alle posizioni
        /// dell'ambiente di ricerca
        /// </summary>
        /// <param name="map"></param>
        /// <param name="function"></param>
        private void BuildUtilityMap(SortedDictionary<SARPoint, double> map, IUtilityFunction function)
        {
            var mapTmp = new SortedDictionary<SARPoint, double>();
            foreach (var cell in map)
            {
                mapTmp.Add(cell.Key, function.ComputeUtility(cell.Key));
            }

            //gli elementi vengono posti in ordine decrescente rispetto al valore di utilità 
            mapTmp.OrderByDescending(x => x.Value);

            map = mapTmp;
        }

        /// <summary>
        /// Applicazione di A* per il calcolo del percorso
        /// </summary>
        /// <param name="currentPos"></param>
        /// <param name="currentGoal"></param>
        /// <returns></returns>
        private List<IPoint> FindRoute(IPoint start, IPoint goal)
        {
            List<Node> openNodes = new List<Node>();//nodi valutati
            List<Node> closedNodes = new List<Node>();//nodi valutati ed espansi
            Dictionary<Node, Node> cameFrom = new Dictionary<Node, Node>();//mappa Arrivo -> Partenza

            #region Funzioni
            Func<Node> _minFCostNode = delegate ()
            {
                return (openNodes.Select(x => Tuple.Create(x, x.FCost))).Min().Item1;
            };
            Func<Node, List<IPoint>> _pathToPoint = delegate (Node endPoint)
            {
                var path = new List<IPoint>
                {
                    endPoint.point
                };

                while (cameFrom.ContainsKey(endPoint))
                {
                    endPoint = cameFrom[endPoint];
                    if (endPoint != null)
                    {
                        path.Add(endPoint.point);
                    }
                }
                return path;
            };
            Func<IPoint, IPoint, double> _distanceBetween = delegate (IPoint p1, IPoint p2)
            {
                return Math.Abs(p1.X - p2.X) + Math.Abs(p1.Y - p2.Y);
            };
            #endregion

            //inizializzo frontiera
            var s = new Node(start)
            {
                GCost = 0
            };
            s.FCost = _costFunc.EvaluateCost(s.point, goal);
            openNodes.Add(s);

            //espansione
            while (openNodes.Count > 0)
            {
                //nodo a costo minimo
                Node current = _minFCostNode();
                if (current.point == goal)
                    return _pathToPoint(current); //ricostruzione cammino minimo fino al goal

                openNodes.Remove(current);
                closedNodes.Add(current);

                //espansione frontiera                
                foreach (var nearPoint in (_environment.GetNeighbors(current.point)))
                {
                    if (closedNodes.Select(x => x.point).Contains(nearPoint))
                    {
                        break;
                    }
                    else
                    {
                        if (!openNodes.Select(x => x.point).Contains(nearPoint))
                        {
                            //è un nodo mai visitato in precedenza
                            var nNode = new Node(_environment.GetPoint(nearPoint.X, nearPoint.Y));
                            openNodes.Add(nNode);

                            cameFrom.Add(nNode, null);
                        }
                        else //verifico di aver trovato un percorso migliore
                        {
                            double newGCost = current.GCost + _distanceBetween(current.point, nearPoint);
                            var nearNode = openNodes.Where(x => x.point.Equals(nearPoint)).First();
                            if (newGCost.CompareTo(nearNode.GCost) < 0)
                            {
                                cameFrom[nearNode] = current;
                                nearNode.GCost = newGCost;
                                nearNode.FCost = nearNode.GCost + _costFunc.EvaluateCost(nearPoint, goal);
                            }
                        }
                    }

                }
            }
            return null;
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
