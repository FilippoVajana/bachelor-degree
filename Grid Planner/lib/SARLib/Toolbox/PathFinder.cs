using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using SARLib.SAREnvironment;
using SARLib.SARPlanner;

namespace SARLib.Toolbox
{
    public interface ISARPathFinder
    {
        List<SARPoint> FindRoute(SARPoint start, SARPoint goal);
    }
    public interface ISARSearchAlgoritm
    {
        List<SARPoint> FindRoute(SARPoint start, SARPoint goal, SARGrid environment, ICostFunction costFunction);
    }

    /// <summary>
    /// Classe che implementa algoritimi per il calcolo di un percorso
    /// </summary>
    class PathFinder : ISARPathFinder
    {
        ISARSearchAlgoritm _algoritm;
        SARGrid _env;
        ICostFunction _costFunc;

        public PathFinder(ISARSearchAlgoritm searchAlgoritm, SARGrid environment, ICostFunction costFunction)
        {
            _algoritm = searchAlgoritm;
            _env = environment;
            _costFunc = costFunction;
        }

        /// <summary>
        /// Applica la strategia di ricerca
        /// </summary>
        /// <param name="start"></param>
        /// <param name="goal"></param>
        /// <returns></returns>
        public List<SARPoint> FindRoute(SARPoint start, SARPoint goal)
        {
            return _algoritm.FindRoute(start, goal, _env, _costFunc);
        }
    }

    public class AStar : ISARSearchAlgoritm
    {
        SARGrid _env;
        ICostFunction _costFunc;
        SARPoint _start;
        SARPoint _goal;

        HashSet<SARPoint> _closedSet, _openSet;
        Dictionary<SARPoint, SARPoint> _cameFrom; //(destinazione,sorgente)
        Dictionary<SARPoint, double> _gScore; //costo del percorso dallo start al punto attuale
        Dictionary<SARPoint, double> _fScore; //costo totale dallo start fino al goal

        Func<IEnumerable<KeyValuePair<SARPoint, double>>, SARPoint> min_cost_point = delegate (IEnumerable<KeyValuePair<SARPoint, double>> set)
        {
            SARPoint minimum = null;

            minimum = set.OrderBy(e => e.Value).First().Key;

            return minimum;
        };
        Func<Dictionary<SARPoint, SARPoint>, SARPoint, List<SARPoint>> reconstruct_path = delegate (Dictionary<SARPoint, SARPoint> cameFrom, SARPoint current)
         {
             var totalPath = new List<SARPoint>() { current };

             //ripercorro a ritroso il cammino calcolato
             while (cameFrom.ContainsKey(current))
             {
                 current = cameFrom[current];
                 totalPath.Add(current);
             }

             //ordino correttamente
             totalPath.Reverse();

             return totalPath;
         };

        public List<SARPoint> FindRoute(SARPoint start, SARPoint goal, SARGrid environment, ICostFunction costFunction)
        {
            //inizializzazione
            _env = environment;
            _costFunc = costFunction;
            _start = start;
            _goal = goal;
            _closedSet = new HashSet<SARPoint>();
            _openSet = new HashSet<SARPoint>() { _start};
            _cameFrom = new Dictionary<SARPoint, SARPoint>();
            _gScore = new Dictionary<SARPoint, double>(); 
            _fScore = new Dictionary<SARPoint, double>();

            //inizializzo g_cost a +INF
            foreach (var node in _env._grid)
            {
                _gScore.Add(node, double.PositiveInfinity);
            }

            //calcolo g_cost per il punto start
            _gScore[_start] = 0;

            //inizializzo f_cost a +INF
            foreach (var node in _env._grid)
            {
                _fScore.Add(node, double.PositiveInfinity);
            }

            //calcolo f_cost per il punto start
            _fScore[_start] = _costFunc.EvaluateCost(_start, _goal);
            
            //espansione e valutazione della frontiera
            while (_openSet.Count > 0)
            {
                //seleziono i costi f dei nodi aperti
                var openCosts = _fScore.Where(x => _openSet.Contains(x.Key));   
                
                //seleziono il nodo aperto a costo minimo
                var current = min_cost_point(openCosts);

                if (current == _goal)
                    return reconstruct_path(_cameFrom, current);

                //chiudo nodo corrente
                _openSet.Remove(current);
                _closedSet.Add(current);

                //espando la frontiera
                var neighbors = _env.GetNeighbors(current);
                foreach (var neighbor in neighbors)
                {
                    //escludo nodi chiusi
                    if (_closedSet.Contains(neighbor))
                    {
                        //non fare nulla
                    }
                    //apro nuovi nodi
                    else if (!_openSet.Contains(neighbor))
                    {
                        _openSet.Add(neighbor as SARPoint);
                    }
                    else //valuto nodo aperto
                    {
                        //calcolo costo g fino al vicino
                        var tentativeGScore = _gScore[current] + _env.Distance(current, _goal);

                        //aggiorno cammino/costo fino al vicino
                        if (tentativeGScore < _gScore[neighbor as SARPoint])
                        {
                            _cameFrom[neighbor as SARPoint] = current;
                            _gScore[neighbor as SARPoint] = tentativeGScore;
                            _fScore[neighbor as SARPoint] = _gScore[neighbor as SARPoint] + _costFunc.EvaluateCost(neighbor as SARPoint, _goal);
                        }
                    }
                }
            }

            return null;
        }
    }

}
