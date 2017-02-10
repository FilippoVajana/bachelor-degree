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
        void BuildRandomGrid(int seed, int iterations);
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
            _sizeCol = columns;
            _sizeRow = rows;
            _grid = new SARPoint[_sizeRow, _sizeCol]; //riga X colonna

            for (int row = 0; row < _sizeRow; row++)
            {
                for (int col = 0; col < _sizeCol; col++)
                {                    
                    _grid[row, col] = new SARPoint(row, col);
                }
            }
        }

        public int Distance(IPoint p1, IPoint p2)
        {
            return (Math.Abs(p1.X - p2.X) + Math.Abs(p1.Y - p2.Y));
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
                return _grid[y,x];
            }
            return null;
        }

        override public string ToString()
        {
            string gridString = "";

            if (_grid != null)
            {
                for (int row = 0; row < _sizeRow; row++)
                {
                    gridString += "\t\t";
                    for (int col = 0; col < _sizeCol; col++)
                    {
                        gridString += String.Format("{0} ", _grid[row, col].ToString());
                    }
                    gridString += "\n";
                }
            }
            return gridString;
        }
        
        public void BuildRandomGrid(int seed, int iterations)
        {            
            Random rnd = new Random(seed);
            int iterCount = 0;
            var types = Enum.GetValues(typeof(SARPoint.PointType));

            while (iterCount < iterations)
            {
                _grid[rnd.Next(_sizeRow), rnd.Next(_sizeCol)].Type = (SARPoint.PointType) rnd.Next(types.Length);
                iterCount++;
            }
        }
    }
    
    

    public class SARPoint : IPoint
    {
        public int X { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int Y { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public enum PointType { Obstacle, Target, Clear }
        public SARPoint(int x, int y)
        {
            Type = PointType.Clear;
            Danger = 0;
            Confidence = 0;
        }
        
        private PointType _type;
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

        private int _dangerLevel;
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

        private int _confidenceLevel;
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
