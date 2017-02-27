using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SARLib.SAREnvironment;

namespace SARLib.SARPlanner
{
    /// <summary>
    /// Funzione costo del percorso
    /// </summary>
    public interface ICostFunction
    {
        double EvaluateCost(IPoint point, IPoint goal);
    }

    /// <summary>
    /// Valuta l'appetibilità a priori di una posizione
    /// </summary>
    public interface IUtilityFunction
    {
        double EvaluateUtility(IPoint point);
    }

    /// <summary>
    /// Strategia per la selezione del prossimo goal
    /// </summary>
    public interface ISARStrategy
    {
        SARPoint SelectNextTarget(SARGrid environment);
    }

    public interface IAction
    {
        string ToString();
    }

    #region PLAN
    public abstract class APlan
    {
        public List<IAction> Plan { get; } //lista delle azioni da compiere
        public List<IPoint> Path { get; } //lista dei punti della griglia da visitare
        public SearchLogger.SearchLog SearchEngineLog { get; }              

        /// <summary>
        /// Adapter per SARLib.SaveToFile
        /// </summary>
        /// <param name="destinationPath"></param>
        /// <returns></returns>
        public string SaveToFile(string destinationPath) //estrarre in classe dedicata
        {
            return SARLib.Toolbox.Saver.SaveToFile(this, destinationPath, ".json");
        }
        //deserializza la classe
        public APlan LoadFromFile(string path) 
        {            
            string plan = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<APlan>(plan);
        }
    }
    internal class PlanningResult : APlan
    {
        private List<IAction> _plan;
        private List<IPoint> _path;
        private SearchLogger.SearchLog _log;

        internal PlanningResult(List<IPoint> path, SearchLogger logger)
        {
            _path = path;
            _log = logger.Log;
            _plan = ExtractPlan(_path);
        }

        private List<IAction> ExtractPlan(List<IPoint> path)
        {
            throw new NotImplementedException();
        }
    }
    #endregion

    #region PLANNER
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
    public interface IPlanner
    {
        APlan ComputePlan(IPoint start, ICostFunction heuristic); //il goal viene calcolato internamente       
    }

    public abstract class Planner : IPlanner
    {        
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
        public abstract APlan ComputePlan(IPoint start, ICostFunction heuristic);//il goal viene calcolato internamente  
    }
    #endregion

    public class AStarPlanner : Planner
    {
        private SARGrid _environment;
        private ICostFunction _heuristic;

        public AStarPlanner(SARGrid env)
        {
            _environment = env;
        }

        public override APlan ComputePlan(IPoint start, ICostFunction heuristic)
        {
            //imposto funzione di costo euristica
            _heuristic = heuristic;


            ///COMPONENTE DI SIMULAZIONE
            ///1)Definizione della strategia per la selezione del goal (nodo con max(Confidence)) 
            ///2)Costruire simulatore come classe esterna; il simulatore ad ogni ciclo chiamerà il metodo ComputePlan
            ///     passando la posizione attuale e la funzione di costo euristica

            //calcolo il nodo della griglia con Confidence massima
            var maxPoint = _environment._grid[0, 0];
            foreach (var node in _environment._grid)
            {
                if (node.Confidence >= maxPoint.Confidence)
                    maxPoint = node;
            }

            //imposto goal
            var goal = maxPoint;
            ///END
            
            //calcolo percorso ottimo
            var path = FindPath(start, goal);

            //estraggo il piano
            return (new PlanningResult(path, null));
        }

        /// <summary>
        /// Tramite algoritmo A* calcola il percorso ottimo per il goal
        /// </summary>
        /// <param name="start"></param>
        /// <param name="goal"></param>
        /// <returns></returns>
        private List<IPoint> FindPath(IPoint start, IPoint goal)
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
            s.FCost = _heuristic.EvaluateCost(s.point, goal);
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
                                nearNode.FCost = nearNode.GCost + _heuristic.EvaluateCost(nearPoint, goal);
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

    //class Move : IPlanningAction
    //{
    //    private IPoint _start, _end;

    //    public Move(IPoint start, IPoint end)
    //    {
    //        _start = start;
    //        _end = end;
    //    }

    //    //public void Execute()
    //    //{
    //    //    throw new NotImplementedException();
    //    //}

    //    public override string ToString()
    //    {
    //        return String.Format("Move {0} -> {1}", _start, _end);
    //    }
    //}

}
