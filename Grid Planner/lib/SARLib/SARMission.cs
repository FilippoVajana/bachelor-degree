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

        SARGrid Environment { get; set; }
        APlan Plan { get; set; }

        IPoint Start { get; set; }
        List<IPoint> Goals { get; set; }
    }

    /// <summary>
    /// Classe rappresentante una missione SAR
    /// riferimento per il visualizzatore
    /// </summary>
    public class SARMission : ISARMission
    {
        #region Proprietà
        public SARGrid Environment { get; set; }
        public APlan Plan { get; set; }
        public IPoint Start { get; set; }
        public List<IPoint> Goals { get; set; }
        #endregion

        #region Costruttori
        //costruttore usato dal serializzatore JSON
        public SARMission() { }
        public SARMission(SARGrid env, APlan searchPlan, IPoint start)
        {
            Environment = env;
            Plan = searchPlan;
            Start = start;
            Goals = env._realTargets;
        }
        //genera missione random
        public SARMission(int envRow, int envCol, int numTargets)
        {
            Environment = new SARGrid(envCol, envRow);
            Plan = null;
            Start = null;
            Goals = new List<IPoint>(numTargets);

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
