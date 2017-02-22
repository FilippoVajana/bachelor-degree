using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace SARLib.SAREnvironment
{
    /// <summary>
    /// Schema for a generic two-dimensional point
    /// </summary>
    public interface IPoint
    {
        int X { get; set; }
        int Y { get; set; }
    }
    public class SARPoint : IPoint
    {
        private PointTypes _type;
        private double _dangerLevel;
        private double _confidenceLevel;

        public int X { get; set; }
        public int Y { get; set; }

        public PointTypes Type
        {
            get
            {
                return _type;
            }
            set
            {
                //togliere if
                if (value == PointTypes.Clear || value == PointTypes.Obstacle || value == PointTypes.Target)
                {
                    switch (value)
                    {
                        case PointTypes.Obstacle:
                            Danger = 1;
                            Confidence = 0;
                            break;
                        case PointTypes.Target:
                            Confidence = 1;                            
                            break;
                        case PointTypes.Clear:
                            break;
                        default:
                            break;
                    }
                    _type = value;
                }
            }
        }
        public double Danger
        {
            get
            {
                return _dangerLevel;
            }
            set
            {
                if (0 <= value && value <= 1)
                {
                    _dangerLevel = value;
                }
            }
        }
        public double Confidence
        {
            get
            {
                return _confidenceLevel;
            }
            set
            {
                if (0 <= value && value <= 1)
                {
                    _confidenceLevel = value;
                }
            }
        }

        public enum PointTypes { Obstacle, Target, Clear }

        public SARPoint(int x, int y)
        {
            X = x;
            Y = y;
            Type = PointTypes.Clear;
            Danger = 0;
            Confidence = 0;
        }

        public String PrintConsoleChar()
        {
            switch (Type)
            {
                case PointTypes.Obstacle:
                    return "#";
                //break;
                case PointTypes.Target:
                    return "$";
                //break;                
                default:
                    return "%";
                    //break;
            }
        }
    }

    /// <summary>
    /// Schema for a generic two-dimensional grid
    /// </summary>
    public interface IGrid
    {
        int Distance(IPoint p1, IPoint p2);
        IPoint[] GetNeighbors(IPoint point);

        String SaveToFile(string destinationPath);
    }
    public class SARGrid : IGrid
    {
        //  row = Y
        //  ^
        //  |
        //  |
        //  |
        //  ----------> col = X

        //{ a a a a a a }
        //{ a b b b b a }
        //{ a a c c c c }

        //rivedere nomi e modificatori di accesso
        public int _numCol, _numRow;
        ///rappresenta sia la topografia dell'ambiente che la distribuzione di probabilità degli obiettivi
        public SARPoint[,] _grid;
        ///rappresenta le posizioni reali dei target
        public List<IPoint> _targets = new List<IPoint>(); 

        /// <summary>
        /// Costruttore default usato da JSON
        /// </summary>
        public SARGrid()
        { }
        public SARGrid(int _numCol, int _numRow)
        {
            this._numCol = Math.Abs(_numCol);
            this._numRow = Math.Abs(_numRow);
            _grid = new SARPoint[this._numCol, this._numRow]; //colonna X riga

            for (int col = 0; col < this._numCol; col++)
            {
                for (int row = 0; row < this._numRow; row++)
                {
                    _grid[col, row] = new SARPoint(col, row);
                }
            }
        }
        public SARGrid(string gridFilePath)
        {
            var grid = LoadFromFile(gridFilePath);

            _grid = grid._grid;
            _numCol = grid._numCol;
            _numRow = grid._numRow;
        }

        public int Distance(IPoint p1, IPoint p2)
        {
            if (IsValidPoint(p1) && IsValidPoint(p2))
            {
                return (Math.Abs(p1.X - p2.X) + Math.Abs(p1.Y - p2.Y));
            }
            throw new IndexOutOfRangeException("Invalid Points");
        }

        public IPoint[] GetNeighbors(IPoint p)
        {
            if (IsValidPoint(p))
            {
                List<SARPoint> neighbors = new List<SARPoint>
                {
                    GetPoint(p.X + 1,p.Y),
                    GetPoint(p.X - 1,p.Y),
                    GetPoint(p.X,p.Y + 1),
                    GetPoint(p.X,p.Y - 1)
                };
                return neighbors.FindAll(x => x != null && x.Type != SARPoint.PointTypes.Obstacle).ToArray();
            }
            return null;
        }

        private bool IsValidPoint(IPoint p)
        {
            return (0 <= p.X && p.X < _numCol) && (0 <= p.Y && p.Y < _numRow);
        }

        public SARPoint GetPoint(int x, int y)
        {
            if (IsValidPoint(new SARPoint(x, y)))
            {
                return _grid[x, y];
            }
            return null;
        }

        public string ConvertToConsoleString()
        {
            string gridString = "";

            if (_grid != null)
            {
                for (int r = 0; r < _numRow; r++)
                {
                    for (int c = 0; c < _numCol; c++)
                    {
                        gridString += String.Format("{0}", _grid[c, r].PrintConsoleChar());
                    }
                    gridString += System.Environment.NewLine;
                }
            }
            return gridString;
        }

        public void RandomizeGrid(int seed, int shuffles)
        {
            Random rnd = new Random(seed);
            int iterCount = 0;
            var types = Enum.GetValues(typeof(SARPoint.PointTypes));

            while (iterCount < shuffles)
            {
                _grid[rnd.Next(_numCol), rnd.Next(_numRow)].Type = (SARPoint.PointTypes)rnd.Next(types.Length);
                iterCount++;
            }
        }

        public void RandomizeGrid(int seed, int numTarget, float clearAreaRatio)
        {
            const float CONFIDENCE_SPREAD_FACTOR = 0.5F;
            Random randomizer = new Random(seed);
            int shufflesCount = 0;
            var cellTypes = Enum.GetValues(typeof(SARPoint.PointTypes));

            SARPoint[] targets = new SARPoint[numTarget];
            //seleziono le celle target   
            SARPoint _tmpTarget;
            for (int i = 0; i < numTarget; i++)
            {
                _tmpTarget = _grid[randomizer.Next(_numCol), randomizer.Next(_numRow)];
                _tmpTarget.Type = SARPoint.PointTypes.Target;
                targets[i] = _tmpTarget;
            }
            //PROBE
            Debug.WriteLine(ConvertToConsoleString());

            //propago la confidence
            foreach (var t in targets)
            {
                var neighbors = GetNeighbors(t);
                //propagazione lineare valore di confidence
                foreach (var n in neighbors)
                {
                    var cell = n as SARPoint;
                    cell.Confidence = (int)(t.Confidence * CONFIDENCE_SPREAD_FACTOR);
                }
            }

            //seleziono le celle obstacle
            while (shufflesCount < (_numCol * _numRow) * (1 - clearAreaRatio))
            {
                var _tmpObstacle = _grid[randomizer.Next(_numCol), randomizer.Next(_numRow)];
                if (!targets.Contains(_tmpObstacle))
                {
                    shufflesCount++;
                    _tmpObstacle.Type = SARPoint.PointTypes.Obstacle;
                }
            }
            //PROBE
            Debug.WriteLine(ConvertToConsoleString());
        }

        private static SARGrid LoadFromFile(string path)
        {
            string gridFile = File.ReadAllText(path);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<SARGrid>(gridFile);
        }

        /// <summary>
        /// Adapter per SARLib.SaveToFile
        /// </summary>
        /// <param name="destinationPath"></param>
        /// <returns></returns>
        public string SaveToFile(string destinationPath)
        {
            return SARLib.Toolbox.Saver.SaveToFile(this, destinationPath, ".json");            
        }
    }

    public class SARViewer
    {
        public enum SARPointAttributes { Confidence, Danger }

        SARGrid _env;        

        public SARViewer(SARGrid environment)
        {
            _env = environment;
        }

        public void DisplayProperty(SARPointAttributes attribute)
        {
            switch (attribute)
            {
                case SARPointAttributes.Confidence:
                    Debug.WriteLine($"CONFIDENCE PROBABILITY DISTRIBUTION \n\n" +
                        $"{PrintConfidence(_env)}");
                    break;
                case SARPointAttributes.Danger:
                    Debug.WriteLine($"DANGER PROBABILITY DISTRIBUTION \n\n" +
                        $"{PrintDanger(_env)}");
                    break;
                default:
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
    }
}
