using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grid_Planner
{
    interface IGrid
    {
        int Distance(APoint p1, APoint p2);
        APoint[] GetNeighbors(APoint p);
        APoint GetPoint(int x, int y);
        String ToString();
    }

    public class Grid : IGrid
    {
        private int _sizeX, _sizeY;
        private SARPoint[][] _grid;

        public Grid(int sizeX, int sizeY)
        {
            _sizeX = sizeX;
            _sizeY = sizeY;
            //_grid = BuildGrid();
        }

        public int Distance(APoint p1, APoint p2)
        {
            return (Math.Abs(p1.X - p2.X) + Math.Abs(p1.Y - p2.Y));
        }

        public APoint[] GetNeighbors(APoint p)
        {
            if (isGridPoint(p))
            {
                List<APoint> neighbors = new List<APoint>
                {
                    _grid[p.X + 1][p.Y],
                    _grid[p.X - 1][p.Y],
                    _grid[p.X][p.Y + 1],
                    _grid[p.X][p.Y - 1]
                };
                return neighbors.FindAll(x => isGridPoint(x)).ToArray();
            }
            return null;
        }

        private bool isGridPoint(APoint p)
        {
            return (0 <= p.X && p.X < _sizeX) && (0 <= p.Y && p.Y < _sizeY);
        }

        public APoint GetPoint(int x, int y)
        {
            if (isGridPoint(new SARPoint(x, y)))
            {
                return _grid[x][y];
            }
            return null;
        }

        override public string ToString()
        {
            //  row = Y
            //  ^
            //  |
            //  |
            //  |
            //  ----------> col = X

            if (_grid != null)
            {
                for (int row = 0; row < _sizeY; row++)
                {
                    for (int col = 0; col < _sizeX; col++)
                    {
                        _grid[row][col].ToString();
                    }
                }
            }
            return null;
        }
    }
    
    /// <summary>
    /// Schema for Grid point object
    /// </summary>
    public abstract class APoint
    {
        public int X { get; set; }
        public int Y { get; set; }

        public APoint(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    public class SARPoint : APoint
    {
        public enum PointType { Obstacle, Target, Clear }
        public SARPoint(int x, int y) : base(x, y)
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
                    return " ";
                    //break;
            }
        }
    }

}
