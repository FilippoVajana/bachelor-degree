using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace GridPlanner
{
    public interface IHeuristic
    {
        double EvaluateCost(GridPoint point, GridPoint goal);
    }

    public interface IPlanningAction
    {        
        string ToString();
    }

    public interface IPlanner
    {
        PlanningResult ComputePlan(GridPoint start, GridPoint goal, IHeuristic heuristic);
        bool SavePlan(PlanningResult plan);        
    }

    public class PlanningResult
    {
        private List<IPlanningAction> _plan;
        private List<GridPoint> _path;
        private SearchLogger.SearchLog _log;

        public List<IPlanningAction> Plan { get { return _plan; } }
        public List<GridPoint> Path { get { return _path; } }
        public SearchLogger.SearchLog ExecutionLog { get { return _log; } }

        protected PlanningResult(List<GridPoint> path, SearchLogger logger)
        {
            _path = path;
            _log = logger.Log;
            _plan = ExtractPlan(_path);
        }

        private List<IPlanningAction> ExtractPlan(List<GridPoint> path)
        {
            throw new NotImplementedException();
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

    

    public class Planner
    {
        /// <summary>
        /// Rappresenta un nodo del grafo usato per l'esplorazione
        /// </summary>
        protected class Node
        {
            public GridPoint point;
            public double GCost { get; set; } //costo dall'origine
            public double FCost { get; set; } //costo aggregato g + h

            public Node(int column, int row)
            {
                point = new GridPoint(column, row);
                GCost = double.MaxValue;
                FCost = 0;
            }
        }
    }

    class AStar_Planner : Planner
    {
        private IGrid _environment;
        private IHeuristic _heuristic;
        //Provvisori
        //private APoint _start;
        //private APoint _goal;

        public AStar_Planner(IGrid env, IHeuristic heuristic)
        {
            _environment = env;
            _heuristic = heuristic;
        }

        
        
        private List<GridPoint> FindPath(GridPoint start, GridPoint goal)
        {
            List<Node> openNodes = new List<Node>();//nodi valutati
            List<Node> closedNodes = new List<Node>();//nodi valutati ed espansi
            Dictionary<Node, Node> cameFrom = new Dictionary<Node, Node>();//mappa Arrivo -> Partenza
                       
            Func<Node> _minFCostNode = delegate ()
            {
                return (openNodes.Select(x => Tuple.Create(x, x.FCost))).Min().Item1;
            };

            Func<Node, List<GridPoint>> _pathToPoint = delegate (Node endPoint)
            {
                var path = new List<GridPoint>
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

            Func<GridPoint, GridPoint, double> _distanceBetween = delegate (GridPoint p1, GridPoint p2)
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
                if(current.point == goal)
                    return _pathToPoint(current); //ricostruzione cammino minimo fino al goal

                openNodes.Remove(current);
                closedNodes.Add(current);

                //espansione frontiera                
                foreach(var nearPoint in (_environment.GetNeighbors(current.point)))
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

       
    class Move : IPlanningAction
    {
        private GridPoint _start, _end;

        public Move(GridPoint start, GridPoint end)
        {
            _start = start;
            _end = end;
        }

        //public void Execute()
        //{
        //    throw new NotImplementedException();
        //}

        public override string ToString()
        {
            return String.Format("Move {0} -> {1}", _start, _end);
        }
    }


}
