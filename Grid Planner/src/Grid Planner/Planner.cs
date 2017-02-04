using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Grid_Planner
{
    internal interface IHeuristic
    {
        double EvaluateCost(APoint point, APoint goal);
    }

    internal interface IPlanningAction
    {        
        string ToString();
    }

    internal interface IPlanner
    {
        Plan ComputePlan(APoint start, APoint goal, IHeuristic heuristic);
        bool SavePlan(Plan plan);        
    }

    public class Plan
    {
        private List<IPlanningAction> _plan;
        //private List<>
    }

    class AStar_Planner
    {
           

        /// <summary>
        /// Rappresenta un punto dell'ambiente valutato durante la pianificazione
        /// </summary>
        class Node : APoint //rimuovi implementazione, sostituire con aggregazione
        {
            public double GCost { get; set; } //costo dall'origine
            public double FCost { get; set; } //costo aggregato g + h

            public Node(int column, int row) : base(column, row)
            {
                GCost = 0;
                FCost = 0;
            }            
        }


        private Plan _plan;
        public Plan Plan { get => _plan;}

        private IGrid _environment;
        private IHeuristic _heuristic;
        //private APoint _start;
        //private APoint _goal;

        public AStar_Planner(IGrid env, IHeuristic heuristic)
        {
            _environment = env;
            _start = start;
            _goal = goal;
            _heuristic = heuristic;
        }

        
        public Path FindPath(APoint start, APoint goal)
        {
            List<Node> openNodes = new List<Node>();//nodi valutati
            List<Node> closedNodes = new List<Node>();//nodi valutati ed espansi
            Dictionary<Node, Node> cameFrom = new Dictionary<Node, Node>();//mappa Arrivo -> Partenza
                       
            Func<Node> _minimumCostNode = delegate ()
            {
                return (openNodes.Select(x => Tuple.Create(x, x.FCost))).Min().Item1;
            };


            Func<Node, List<Node>> _pathToPoint = delegate (Node endPoint)
            {
                var path = new List<Node>();
                path.Add(endPoint);
                //var prev = cameFrom[endPoint];

                while (cameFrom.ContainsKey(endPoint))
                {
                    endPoint = cameFrom[endPoint];
                    if (endPoint != null)
                    {
                        path.Add(endPoint);
                    }
                }
                return (new Path(path));
            };

            //inizializzo frontiera
            var s = new Node(start.Y, start.X)
            {
                GCost = 0
            };
            s.FCost = _heuristic.EvaluateCost(s, goal);
            openNodes.Add(s);

            //espansione
            while (openNodes.Count > 0)
            {
                //nodo a costo minimo
                Node current = _minimumCostNode();
                if(current == goal)
                    return _pathToPoint(current); //ricostruzione cammino minimo fino al goal

                openNodes.Remove(current);
                closedNodes.Add(current);

                //espansione frontiera                
                foreach(var neighbor in (_environment.GetNeighbors(current)))
                {
                    if (!closedNodes.Contains(neighbor))
                    {

                    }
                }
            }            
        }
    }

    

    

     
    class Move : IPlanningAction
    {
        private Grid_Planner.APoint _start, _end;

        public Move(Grid_Planner.APoint start, Grid_Planner.APoint end)
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
