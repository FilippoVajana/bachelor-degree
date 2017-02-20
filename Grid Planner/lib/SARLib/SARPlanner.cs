using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SARLib.SAREnvironment;

namespace SARLib.SARPlanner
{
    public interface IHeuristic
    {
        double EvaluateCost(IPoint point, IPoint goal);
    }

    public interface IAction
    {
        string ToString();
    }

    #region PLAN
    public abstract class APlan
    {
        public List<IAction> Plan { get; }
        public List<IPoint> Path { get; }
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
    public interface IPlanner
    {
        APlan ComputePlan(IPoint start, IPoint goal, IHeuristic heuristic);        
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

            public Node(int column, int row)
            {
                point = new SARPoint(column, row);
                GCost = double.MaxValue;
                FCost = 0;
            }
        }
        public abstract APlan ComputePlan(IPoint start, IPoint goal, IHeuristic heuristic);        
    }
    #endregion

    public class AStar_Planner : Planner
    {
        private IGrid _environment;
        private IHeuristic _heuristic;
        //Provvisori
        //private APoint _start;
        //private APoint _goal;

        public AStar_Planner(IGrid env)
        {
            _environment = env;
        }

        public override APlan ComputePlan(IPoint start, IPoint goal, IHeuristic heuristic)
        {
            //imposto euristica
            _heuristic = heuristic;

            //calcolo percorso ottimo
            var path = FindPath(start, goal);

            //estraggo il piano
            return (new PlanningResult(path, null));
        }
        
        private List<IPoint> FindPath(IPoint start, IPoint goal)
        {
            List<Node> openNodes = new List<Node>();//nodi valutati
            List<Node> closedNodes = new List<Node>();//nodi valutati ed espansi
            Dictionary<Node, Node> cameFrom = new Dictionary<Node, Node>();//mappa Arrivo -> Partenza

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

            //inizializzo frontiera
            var s = new Node(start.Y, start.X)
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
                            var nNode = new Node(nearPoint.X, nearPoint.Y);
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
