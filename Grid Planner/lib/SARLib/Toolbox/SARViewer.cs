using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SARLib.SAREnvironment
{
    public class SARViewer
    {
        public enum SARPointAttributes { Confidence, Danger }
        
        //Visualizzazione topologia ambiente
        public string DisplayEnvironment(SARGrid environment)
        {
            var _env = environment;
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
        public string DisplayProperty(SARGrid environment, SARPointAttributes attribute)
        {
            var _env = environment;
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
                    gridString += String.Format("{0:0.000} ", env._grid[c, r].Confidence);
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
                    gridString += String.Format("{0:0.000} ", env._grid[c, r].Danger);
                }
                gridString += System.Environment.NewLine;
            }

            return gridString;
        };

        //Visualizzazione mappe ambiente
        public string DisplayMap(SARGrid environment, Dictionary<SARPoint, double> map)
        {
            var _env = environment;
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
                        gridString += String.Format("{0:0.000} ", map[p]);
                    }
                    gridString += Environment.NewLine;
                }
            }
            Debug.WriteLine($"{map.GetType().Name} ({_numCol} x {_numRow})");

            return gridString;
        }

        //Creazione file csv mappe ambiente
        const string DATA_DST_PATH = @"C:\Users\filip\Dropbox\Unimi\pianificazione\Grid Planner\test\SARLibUnitTest\Output\Data";
        /// <summary>
        /// Costruisce e salva il file .csv relativo alla mappa. 
        /// </summary>
        /// <param name="map"></param>
        /// <param name="mapName"></param>
        /// <param name="destinationPath"></param>
        /// <returns></returns>        
        public string BuildMapCsv(SARGrid environment, Dictionary<SARPoint, double> map, string mapName, string destinationPath = DATA_DST_PATH)
        {
            //costruisco root
            var date = DateTime.Now.DayOfYear;
            var root = Directory.CreateDirectory(Path.Combine(destinationPath, "CSV", date.ToString())).FullName;

            //definisco nome file
            var MD5Hash = System.Security.Cryptography.MD5.Create();
            var hash = MD5Hash.ComputeHash(Encoding.ASCII.GetBytes(map.ToString()));

            var csvName = $"{mapName}_{BitConverter.ToString(hash).Replace("-", "")}.csv";

            var mapStr = DisplayMap(environment, map);
            mapStr = mapStr.Replace(" ", ";");

            //salvataggio nella cartella Data
            
            var path = Path.Combine(root, csvName);
            File.WriteAllText(path, mapStr);

            return mapStr;
        }

        
    }
}
