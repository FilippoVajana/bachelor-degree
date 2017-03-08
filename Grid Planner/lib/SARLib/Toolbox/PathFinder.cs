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
        List<SARPoint> FindRoute(SARPoint start, SARPoint goal, SARGrid environment, ICostFunction costFunction, decimal dangerThreshold = 1);
    }

    /// <summary>
    /// Classe che implementa algoritimi per il calcolo di un percorso
    /// </summary>
    class PathFinder : ISARPathFinder
    {
        ISARSearchAlgoritm _algoritm;
        SARGrid _env;
        ICostFunction _costFunc;
        decimal _dangerThreshold;

        public PathFinder(ISARSearchAlgoritm searchAlgoritm, SARGrid environment, ICostFunction costFunction, decimal dangerThreshold = 1)
        {
            _algoritm = searchAlgoritm;
            _env = environment;
            _costFunc = costFunction;
            _dangerThreshold = dangerThreshold;
        }

        /// <summary>
        /// Applica la strategia di ricerca
        /// </summary>
        /// <param name="start"></param>
        /// <param name="goal"></param>
        /// <returns></returns>
        public List<SARPoint> FindRoute(SARPoint start, SARPoint goal)
        {
            return _algoritm.FindRoute(start, goal, _env, _costFunc, _dangerThreshold);
        }
    }

    public class AStar : ISARSearchAlgoritm
    {
        SARGrid _env;
        ICostFunction _costFunc;
        SARPoint _start;
        SARPoint _goal;
        decimal _dangerThreshold;
        //Debug
        public List<decimal> _dangerThresholdLog = new List<decimal>();

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
        
        /// <summary>
        /// Filtra la frontiera usando una soglia di pericolo adattiva
        /// </summary>
        /// <param name="border"></param>
        /// <returns></returns>
        private HashSet<SARPoint> FilterBorderDanger(HashSet<SARPoint> border)
        {
            var filteredBorder = new HashSet<SARPoint>();
            var dangerThreshold = _dangerThreshold;

            //controllo validità frontiera
            if (border.Count > 0)
            {
                //controllo frontiera
                while (filteredBorder.Count <= 0)
                {
                    //Debug
                    _dangerThresholdLog.Add(dangerThreshold);

                    //filtro frontiera
                    var fb = border.Where<SARPoint>(x => (x.Danger <= (double)dangerThreshold));
                    foreach (var p in fb)
                    {
                        filteredBorder.Add(p);
                    }

                    //innalzo soglia limite
                    dangerThreshold += dangerThreshold * (decimal)0.10; //+10% alla soglia
                }
            }

            return filteredBorder;
        }
        
        /// <summary>
        /// Calcola il percorso ottimale fino al goal
        /// </summary>
        /// <param name="start"></param>
        /// <param name="goal"></param>
        /// <param name="environment"></param>
        /// <param name="costFunction"></param>
        /// <param name="dangerThreshold"></param>
        /// <returns></returns>
        public List<SARPoint> FindRoute(SARPoint start, SARPoint goal, SARGrid environment, ICostFunction costFunction, decimal dangerThreshold = 1)
        {
            //inizializzazione parametri
            _env = environment;
            _costFunc = costFunction;
            _start = start;
            _goal = goal;
            _dangerThreshold = dangerThreshold;
                        

            //inizializzazione strutture dati per A*
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
                //filtro soglia di pericolo adattiva
                _openSet = FilterBorderDanger(_openSet);

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
                    //nodo già visitato
                    if (_closedSet.Contains(neighbor))
                    {
                        continue;
                    }

                    //calcolo costo g fino al vicino
                    var tentativeGScore = _gScore[current] + _env.Distance(current, _goal);

                    //nuovo nodo
                    if (!_openSet.Contains(neighbor))
                    {
                        _openSet.Add(neighbor as SARPoint);
                    }
                    else if (tentativeGScore >= _gScore[neighbor as SARPoint])//percorso peggiore
                    {
                        continue;
                    }

                    //aggiorno parametri di costo
                    _cameFrom[neighbor as SARPoint] = current;
                    _gScore[neighbor as SARPoint] = tentativeGScore;
                    _fScore[neighbor as SARPoint] = _gScore[neighbor as SARPoint] + _costFunc.EvaluateCost(neighbor as SARPoint, _goal);
                }
            }
            return new List<SARPoint>() { };
        }
    }

}
