﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grid_Planner
{
    interface IGrid
    {
        int Distance(GridPoint p1, GridPoint p2);
        GridPoint[] GetNeighbors(GridPoint point);
        GridPoint GetPoint(int x, int y);
        String ToString();
        void Randomize(int seed, int iterations);
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

        public int Distance(GridPoint p1, GridPoint p2)
        {
            return (Math.Abs(p1.X - p2.X) + Math.Abs(p1.Y - p2.Y));
        }

        public GridPoint[] GetNeighbors(GridPoint p)
        {
            if (isGridPoint(p))
            {
                List<GridPoint> neighbors = new List<GridPoint>
                {
                    GetPoint(p.X + 1,p.Y),
                    GetPoint(p.X - 1,p.Y),
                    GetPoint(p.X,p.Y + 1),
                    GetPoint(p.X,p.Y - 1)
                };
                return neighbors.FindAll(x => x != null).ToArray();
            }
            return null;
        }

        private bool isGridPoint(GridPoint p)
        {
            return (0 <= p.X && p.X < _sizeCol) && (0 <= p.Y && p.Y < _sizeRow);
        }

        public GridPoint GetPoint(int x, int y)
        {
            if (isGridPoint(new SARPoint(x, y)))
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
        
        public void Randomize(int seed, int iterations)
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
    
    /// <summary>
    /// Schema for Grid point object
    /// </summary>
    public class GridPoint
    {
        public int X { get; set; }
        public int Y { get; set; }

        public GridPoint(int column, int row)
        {
            X = column;
            Y = row;
        }
    }

    public class SARPoint : GridPoint
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
                    return "%";
                    //break;
            }
        }
    }

}
