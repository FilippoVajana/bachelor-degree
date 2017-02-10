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
        public TSPSolver(SARGrid environment, SARPoint start, int radius )
        {
            //inizializzazione
            _openNodes.Add(start);
            int counter = radius;

            //espansione della frontiera
            while (_openNodes.Count > 0 && counter >= 0)
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
            }
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

        private class ListHandler
        {
            public static List<List<Object>> Permute(List<Object> list, IPermuteAlgorithm algorithm)
            {
                List<List<Object>> result = algorithm.Permute(list);
                return result;
            }
        }
    }

    public interface IPermuteAlgorithm
    {
        List<List<Object>> Permute(List<Object> list);
    }

    public class VajanaAlgorithm : IPermuteAlgorithm
    {
        public List<List<object>> Permute(List<object> list)
        {
            List<List<Object>> result = new List<List<object>>(list.Count);
            //result.Add(list);
            
            while (list.Count > 2)
            {
                var head = list[0];
                var subPerm = Permute(list.GetRange(1, list.Count - 1));

                foreach (var p in subPerm)
                {
                    var ls = new List<Object>(p);
                    ls.Insert(0, head);
                    foreach (var e in ls)
                    {
                        var tmp = new List<Object>(ls);                        
                        result.Add(SwapItems(0, tmp.IndexOf(e), tmp));
                    }
                }
                return result;
            }
            if (list.Count <= 2)
            {
                List<List<Object>> res = new List<List<object>>();                
                var tmp = new List<Object>(list);
                SwapItems(0, 1, tmp);

                res.Add(tmp);
                res.Add(list);
                return res;
            }
            return null;
        }

        private List<Object> SwapItems(int aIndex, int bIndex, List<Object> list)
        {
            var a = list[aIndex];
            var b = list[bIndex];

            list[aIndex] = b;
            list[bIndex] = a;

            return list;
        }
    }
}
