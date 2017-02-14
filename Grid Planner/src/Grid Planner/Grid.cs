using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GridPlanner
{
    /// <summary>
    /// Schema for a generic two-dimensional point
    /// </summary>
    public interface IPoint
    {
        int X { get; set; }
        int Y { get; set; }
    }

    /// <summary>
    /// Schema for a generic two-dimensional grid
    /// </summary>
    public interface IGrid
    {
        int Distance(IPoint p1, IPoint p2);
        IPoint[] GetNeighbors(IPoint point);

        String SaveToFile(bool debugMode);        
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
        public SARPoint[,] _grid;

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

        
        /// <summary>
        /// Costruttore default usato da JSON
        /// </summary>
        public SARGrid()
        {   }

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
                return neighbors.FindAll(x => x != null && x.Type != SARPoint.PointType.Obstacle).ToArray();
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
                    gridString += String.Format("\\n");
                }
            }
            return gridString;
        }
        
        public void FillGridRandom(int seed, int iterations)
        {            
            Random rnd = new Random(seed);
            int iterCount = 0;
            var types = Enum.GetValues(typeof(SARPoint.PointType));

            while (iterCount < iterations)
            {
                _grid[rnd.Next(_numCol), rnd.Next(_numRow)].Type = (SARPoint.PointType) rnd.Next(types.Length);
                iterCount++;
            }
        }

        private static SARGrid LoadFromFile(string path)
        {
            string gridFile = File.ReadAllText(path);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<SARGrid>(gridFile);
        }

        public string SaveToFile(bool debugMode)//metodo di default
        {
            var model = this;

            //serializzo l'istanza corrente della classe Grid
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(model);

            //creo la directory per gli output
            string appLocalFolder;
            if (!debugMode)
                appLocalFolder = Directory.GetCurrentDirectory();
            else
                appLocalFolder = Path.GetFullPath(@"C:\Users\filip\Dropbox\Unimi\pianificazione\Grid Planner\Grid Planner - Unit Test\bin\Debug\netcoreapp1.0");
            var outputDir = Directory.CreateDirectory(Path.Combine(appLocalFolder, "Output"));
            
            //calcolo hash della griglia
            var hashFunc = System.Security.Cryptography.MD5.Create();
            var stringBuffer = Encoding.ASCII.GetBytes(model.ConvertToConsoleString());
            byte[] hashValue = hashFunc.ComputeHash(stringBuffer);
            
            //creo il file di output
            var outFileName = $"{BitConverter.ToString(hashValue).Replace("-", "")}_SARGRID.json";
            string outputFilePath = $"{outputDir.FullName}\\{outFileName}";
            File.WriteAllText(outputFilePath, json, Encoding.ASCII);

            return outputFilePath;
        }        
    }
    
    

    public class SARPoint : IPoint
    {
        private PointType _type;
        private int _dangerLevel;
        private int _confidenceLevel;

        public int X { get; set; }
        public int Y { get; set; }

        public PointType Type
        {
            get
            {
                return _type;
            }
            set
            {
                if (value == PointType.Clear || value == PointType.Obstacle || value == PointType.Target)
                {
                    _type = value;
                }
            }
        }
        public int Danger
        {
            get
            {
                return _dangerLevel;
            }
            set
            {
                if (0 <= value && value <= 10)
                {
                    _dangerLevel = value;
                }
            }
        }
        public int Confidence
        {
            get
            {
                return _confidenceLevel;
            }
            set
            {
                if (0 <= value && value <= 10)
                {
                    _confidenceLevel = value;
                }
            }
        }

        public enum PointType { Obstacle, Target, Clear }

        public SARPoint(int x, int y)
        {
            X = x;
            Y = y;
            Type = PointType.Clear;
            Danger = 0;
            Confidence = 0;
        }       
        
        public String PrintConsoleChar()
        {
            switch (Type)
            {
                case PointType.Obstacle:
                    return "#";
                    //break;
                case PointType.Target:
                    return "$";
                    //break;                
                default:
                    return "%";
                    //break;
            }
        }
    }

}
