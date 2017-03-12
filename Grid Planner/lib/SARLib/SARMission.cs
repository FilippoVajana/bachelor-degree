using System;
using System.Collections.Generic;
using System.Text;
using SARLib.SAREnvironment;
using SARLib.SARPlanner;

namespace SARLib.SARMission
{
    public interface ISARMission
    {
        string SaveToFile(string dstPath);
        ISARMission LoadFromFile(string srcPath);

        //Proprietà Missione SAR
        SARGrid Environment { get; set; }
        List<SARPoint> Route { get; set; }

        SARPoint Start { get; set; }
        List<SARPoint> Goals { get; set; }
                
    }

    /// <summary>
    /// Classe rappresentante una missione SAR
    /// riferimento per il visualizzatore
    /// </summary>
    public class SARMission : ISARMission
    {
        #region Proprietà
        public SARGrid Environment { get; set; }
        public List<SARPoint> Route { get; set; }
        public SARPoint Start { get; set; }
        public List<SARPoint> Goals { get; set; }
        #endregion

        #region Costruttori
        //costruttore usato dal serializzatore JSON
        public SARMission() { }
        /// <summary>
        /// Costruttore principale
        /// </summary>
        /// <param name="env"></param>
        /// <param name="route"></param>
        /// <param name="start"></param>
        public SARMission(SARGrid env, List<SARPoint> route, SARPoint start)
        {
            Environment = env;
            Route = route;
            Start = start;
            Goals = env._estimatedTargetPositions;
        }
        //genera missione random
        public SARMission(int envRow, int envCol, int numTargets)
        {
            Environment = new SARGrid(envCol, envRow);
            Route = null;
            Start = null;
            Goals = new List<SARPoint>(numTargets);

            RandomizeMission(numTargets);
        }
        #endregion

        #region Load/Save
        //metodi di IO
        public string SaveToFile(string dstPath)
        {
            throw new NotImplementedException();
        }

        public ISARMission LoadFromFile(string srcPath)
        {
            throw new NotImplementedException();
        }
        #endregion

        private void RandomizeMission(int numTargets)
        {
            //ambiente random
            Environment.RandomizeGrid(10, Environment._numCol * Environment._numRow);
            //piano di ricerca random

            //punto di partenza random

            //obiettivi random

        }

        private SARPoint PickRandomGridPoint(SARGrid grid, SARPoint.PointTypes type)
        {
            Random rnd = new Random(10);
            SARPoint point = grid.GetPoint(rnd.Next(0, grid._numCol - 1), rnd.Next(grid._numRow - 1));

            while (point.Type != type)
            {
                point = grid.GetPoint(rnd.Next(0, grid._numCol - 1), rnd.Next(grid._numRow - 1));
            }

            return point;
        }


    }
}
