using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grid_Planner
{
    interface IGrid
    {
        int ManhattanDistance(Point p1, Point p2);
        Point[] GetNeighbors(Point p);
        Point GetPoint(int x, int y);
    }

    public class Grid : IGrid
    {
        private int _sizeX, _sizeY;
        private Point[][] _grid;

        public Grid(int sizeX, int sizeY)
        {
            _sizeX = sizeX;
            _sizeY = sizeY;
            //_grid = BuildGrid();
        }

        public int ManhattanDistance(Point p1, Point p2)
        {
            return (Math.Abs(p1.X - p2.X) + Math.Abs(p1.Y - p2.Y));
        }

        public Point[] GetNeighbors(Point p)
        {
            if (isGridPoint(p))
            {
                List<Point> neighbors = new List<Point>();
                neighbors.Add(_grid[p.X + 1][p.Y]);
                neighbors.Add(_grid[p.X - 1][p.Y]);
                neighbors.Add(_grid[p.X][p.Y + 1]);
                neighbors.Add(_grid[p.X][p.Y - 1]);

                return neighbors.FindAll(x => isGridPoint(x)).ToArray();
            }
            return null;
        }

        private bool isGridPoint(Point p)
        {
            return (0 <= p.X && p.X < _sizeX) && (0 <= p.Y && p.Y < _sizeY);
        }

        public Point GetPoint(int x, int y)
        {
            if (isGridPoint(new Point(x, y)))
            {
                return _grid[x][y];
            }
            return null;
        }
    }

    /// <summary>
    /// Schema for Grid point object
    /// </summary>
    public class Point
    {
        public int X { get; set; }
        public int Y { get; set; }

        private int dangerLevel;
        public int Danger
        {
            get
            {
                return dangerLevel;
            }
            set
            {
                if (0 <= value && value <= 10 )
                {
                    dangerLevel = value;
                }
            }
        }

        private int confidenceLevel;
        public int Confidence
        {
            get
            {
                return confidenceLevel;
            }
            set
            {
                if (0 <= value && value <= 10)
                {
                    confidenceLevel = value;
                }
            }
        }

        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
}
