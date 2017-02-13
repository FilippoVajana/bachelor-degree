using System;
using System.Collections.Generic;
using System.Linq;
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
        void FillGridRandom(int seed, int iterations);
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

        private int _sizeCol, _sizeRow;
        private SARPoint[,] _grid;

        public SARGrid(int columns, int rows)
        {
            _sizeCol = Math.Abs(columns);
            _sizeRow = Math.Abs(rows);
            _grid = new SARPoint[_sizeCol, _sizeRow]; //colonna X riga

            for (int col = 0; col < _sizeCol; col++)
            {
                for (int row = 0; row < _sizeRow; row++)
                {                    
                    _grid[col, row] = new SARPoint(col, row);
                }
            }
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
                return neighbors.FindAll(x => x != null && x.Type != SARPoint.PointType.Obstacle).ToArray();
            }
            return null;
        }

        private bool IsValidPoint(IPoint p)
        {
            return (0 <= p.X && p.X < _sizeCol) && (0 <= p.Y && p.Y < _sizeRow);
        }

        public SARPoint GetPoint(int x, int y)
        {
            if (IsValidPoint(new SARPoint(x, y)))
            {
                return _grid[x, y];
            }
            return null;
        }

        override public string ToString()
        {
            string gridString = "";

            if (_grid != null)
            {
                for (int r = 0; r < _sizeRow; r++)
                {
                    for (int c = 0; c < _sizeCol; c++)
                    {
                        gridString += String.Format("{0}", _grid[c, r].ToString());
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
                _grid[rnd.Next(_sizeCol), rnd.Next(_sizeRow)].Type = (SARPoint.PointType) rnd.Next(types.Length);
                iterCount++;
            }
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
        
        override public String ToString()
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
