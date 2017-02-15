using System;
using System.Collections.Generic;
using System.Text;
using SAREnvironmentLibrary;

namespace GridPlanner.Toolbox
{
    class TSPSolver
    {
        private SARGrid _env;
        private int _radius;///raggio di espansione della frontiera
        private ISARPointFilter _sarFilter;///definisce una soglia per la selezione dei punti di interesse

        private List<IPoint> _openNodes; ///nodi della frontiera da espandere
        private List<IPoint> _closedNodes; ///nodi della frontiera espansi        

        public TSPSolver(SARGrid environment, int radius, ISARPointFilter sarFilter)
        {
            ///Passi fondamentali
            ///1-esplorare area
            ///     -all'interno di un raggio fissato
            ///     -n espansioni della frontiera
            ///     
            ///2-generare lista waypoint
            ///     -utilizzo di un filtro  
            ///          
            ///3-calcolare permutazioni lista wp
            ///     -algoritmo Vajana
            ///     -algoritmo Heap
            ///     
            ///4-selezionare permutazione a costo minimo
            ///     -calcolo delle distanze reciproche

            _env = environment;
            _radius = radius;
            _sarFilter = sarFilter;            
        }
        
        public Tuple<List<SARPoint>, double> Solve(SARPoint start)
        {
            ///STEP 1
            ///espansione circolare della frontiera
            #region Step_1

            //inizializzazione
            _openNodes.Add(start);
            int counter = _radius;

            //espansione della frontiera
            while (_openNodes.Count > 0 && counter > 0)
            {
                var tmpOpenNodes = new List<IPoint>(_openNodes);
                foreach (var node in _openNodes)
                {
                    var neighbors = new List<IPoint>(_env.GetNeighbors(node));
                    neighbors = neighbors.FindAll(x => !_closedNodes.Contains(x) && !_openNodes.Contains(x)); //rimuovo i nodi già visitati
                    tmpOpenNodes.AddRange(neighbors);

                    tmpOpenNodes.Remove(node);
                    _closedNodes.Add(node); //chiudo il nodo
                }
                _openNodes = tmpOpenNodes;
                counter--;
            }
            #endregion

            ///STEP 2
            ///generazione lista punti di interesse
            #region Step_2
            var _envNodes = new List<SARPoint>(_closedNodes.Count);
            _closedNodes.ForEach(x => _envNodes.Add(_env.GetPoint(x.X, x.Y))); //cast dei nodi

            var _targetNodes = _sarFilter.Filter(_envNodes);
            #endregion

            ///STEP 3
            ///calcolo delle permutazioni
            #region Step_3
            var _possiblePaths = new VajanaAlgorithm<SARPoint>().Permute(new List<SARPoint>(_targetNodes));
            #endregion

            ///STEP 4
            ///selezione percorso a costo minimo
            #region Step_4
            var _pathCostMap = new Dictionary<List<SARPoint>, double>(_possiblePaths.Count); //mappa Percorso -> Costo           
            Tuple<List<SARPoint>, double> _bestPath = new Tuple<List<SARPoint>, double>(null, double.MaxValue);

            foreach (var path in _possiblePaths) //calcolo costo complessivo dei singoli percorsi
            {
                double cost = 0;
                //calcolo distanza reciproca
                for (int i = 0; i < (path.Count - 1); i++)
                {
                    cost += _env.Distance(path[i], path[i + 1]);
                    if (cost <= _bestPath.Item2)
                    {
                        _bestPath = new Tuple<List<SARPoint>, double>(path, cost);
                    }
                }
                _pathCostMap.Add(path, cost);
            }
            #endregion

            return _bestPath;
        }

        private IPoint[] ExpandNode(SARPoint node)
        {
            return _env.GetNeighbors(node);
        }

        private class ListHandler<T>
        {
            public static List<List<T>> Permute(List<T> list, ISARPermutationsAlgorithm<T> algorithm)
            {
                List<List<T>> result = algorithm.Permute(list);
                return result;
            }
        }
    }

    public interface ISARPointFilter
    {
        List<SARPoint> Filter(List<SARPoint> list);
    }
    public class SARFilter : ISARPointFilter
    {
        private int _confidenceThreshold;

        public SARFilter(int confidenceThreshold)
        {
            _confidenceThreshold = confidenceThreshold;
        }

        public List<SARPoint> Filter(List<SARPoint> list)
        {
            return list.FindAll(x => x.Confidence >= _confidenceThreshold);
        }
    }

    #region Permutazioni
    public interface ISARPermutationsAlgorithm<T>
    {
        List<List<T>> Permute(List<T> list);
    }

    public class VajanaAlgorithm<T> : ISARPermutationsAlgorithm<T>
    {
        public List<List<T>> Permute(List<T> list)
        {
            List<List<T>> result = new List<List<T>>(list.Count);
            //result.Add(list);

            while (list.Count > 2)
            {
                var head = list[0];
                var subPerm = Permute(list.GetRange(1, list.Count - 1));

                foreach (var p in subPerm)
                {
                    var ls = new List<T>(p);
                    ls.Insert(0, head);
                    foreach (var e in ls)
                    {
                        var tmp = new List<T>(ls);
                        result.Add(SwapItems(0, tmp.IndexOf(e), tmp));
                    }
                }
                return result;
            }
            if (list.Count <= 2)
            {
                List<List<T>> res = new List<List<T>>();
                var tmp = new List<T>(list);
                SwapItems(0, 1, tmp);

                res.Add(tmp);
                res.Add(list);
                return res;
            }
            return null;
        }

        private List<T> SwapItems(int aIndex, int bIndex, List<T> list)
        {
            var a = list[aIndex];
            var b = list[bIndex];

            list[aIndex] = b;
            list[bIndex] = a;

            return list;
        }
    } 
    #endregion
}
