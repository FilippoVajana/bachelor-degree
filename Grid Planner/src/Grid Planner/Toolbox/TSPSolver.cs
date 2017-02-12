using System;
using System.Collections.Generic;
using System.Text;

namespace GridPlanner.Toolbox
{
    class TSPSolver
    {
        private SARGrid _env;
        private int _radius;
        private List<IPoint> _openNodes; //da espandere
        private List<IPoint> _closedNodes; //espansi
        

        public TSPSolver(SARGrid environment, SARPoint start, int radius, ISARPointFilter sarFilter )
        {
            ///STEP 1
            ///espansione circolare della frontiera
            #region Step_1

            //inizializzazione
            _openNodes.Add(start);
            int counter = radius;

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
            
            var _envNodes = new List<SARPoint>(_closedNodes.Count);
            _closedNodes.ForEach(x => _envNodes.Add(_env.GetPoint(x.X, x.Y))); //cast dei nodi

            var _targetNodes = sarFilter.Filter(_envNodes);

            ///STEP 3
            ///calcolo delle permutazioni
            
            var _possiblePaths = new VajanaAlgorithm<SARPoint>().Permute(new List<SARPoint>(_targetNodes));

            ///STEP 4
            ///selezione percorso a costo minimo

            var _pathCostMap = new Dictionary<List<SARPoint>, double>(_possiblePaths.Count);
            foreach (var path in _possiblePaths)
            {
                var cost = 0;
                //calcolo distanza reciproca
                for (int i = 0; i < (path.Count - 1); i++)
                {
                    cost += _env.Distance(path[i], path[i + 1]);
                }
            }
            //var _bestPath = _possiblePaths.Find(x => )
        }
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
