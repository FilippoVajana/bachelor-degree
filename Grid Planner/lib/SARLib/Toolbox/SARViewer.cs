using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace SARLib.SAREnvironment
{
    public class SARViewer
    {
        public enum SARPointAttributes { Confidence, Danger }

        SARGrid _env;        

        public SARViewer(SARGrid environment)
        {
            _env = environment;
        }

        //Visualizzazione topologia ambiente
        public string DisplayEnvironment()
        {
            int _numRow = _env._numRow;
            int _numCol = _env._numCol;
            string gridString = "";

            if (_env != null)
            {
                for (int r = _numRow - 1; r >= 0; r--)
                {
                    for (int c = 0; c < _numCol; c++)
                    {
                        gridString += String.Format(" {0} ", _env._grid[c, r].PrintConsoleFriendly());
                    }
                    gridString += Environment.NewLine;
                }
            }
            Debug.WriteLine($"{_env.GetType().Name} ({_numCol} x {_numRow})");

            return gridString;
        }

        //Visualizzazione proprietà ambiente
        public string DisplayProperty(SARPointAttributes attribute)
        {
            switch (attribute)
            {
                case SARPointAttributes.Confidence:
                    Debug.WriteLine($"CONFIDENCE PROBABILITY DISTRIBUTION \n\n" +
                        $"{PrintConfidence(_env)}");
                    return PrintConfidence(_env);
                    break;
                case SARPointAttributes.Danger:
                    Debug.WriteLine($"DANGER PROBABILITY DISTRIBUTION \n\n" +
                        $"{PrintDanger(_env)}");
                    return PrintDanger(_env);
                    break;
                default:
                    return String.Empty;
                    break;
            }
        }
        Func<SARGrid, string> PrintConfidence = delegate (SARGrid env)
        {
            string gridString = string.Empty;
            var _numRow = env._numRow - 1;
            var _numCol = env._numCol;

            for (int r = _numRow; r >= 0 ; r--)
            {
                for (int c = 0; c < _numCol ; c++)
                {
                    gridString += String.Format(" {0:0.000} ", env._grid[c, r].Confidence);
                }
                gridString += System.Environment.NewLine;
            }

            return gridString;
        };
        Func<SARGrid, string> PrintDanger = delegate (SARGrid env)
        {
            string gridString = string.Empty;
            var _numRow = env._numRow - 1;
            var _numCol = env._numCol;

            for (int r = _numRow; r >= 0; r--)
            {
                for (int c = 0; c < _numCol; c++)
                {
                    gridString += String.Format(" {0:0.000} ", env._grid[c, r].Danger);
                }
                gridString += System.Environment.NewLine;
            }

            return gridString;
        };

        //Visualizzazione mappe ambiente
        public string DisplayMap(Dictionary<SARPoint, double> map)
        {
            int _numRow = _env._numRow;
            int _numCol = _env._numCol;
            string gridString = "";

            if (_env != null)
            {
                for (int r = _numRow - 1; r >= 0; r--)
                {
                    for (int c = 0; c < _numCol; c++)
                    {
                        var p = _env.GetPoint(c, r);
                        gridString += String.Format(" {0:0.000} ", map[p]);
                    }
                    gridString += Environment.NewLine;
                }
            }
            Debug.WriteLine($"{map.GetType().Name} ({_numCol} x {_numRow})");

            return gridString;
        }
    }
}
