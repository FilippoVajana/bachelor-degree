using System;
using System.Collections.Generic;
using System.Text;
using SARLib.SAREnvironment;
using SARLib.SARMission;
using System.Linq;
using System.IO;

namespace SARLib.Toolbox
{
    public class SimulationLogger
    {
        string _logDirectory;
        bool _verboseMode;
        public string instanceID;
        public int instanceMID;
        SARGrid _env;
        SARMission.SARMission _missionResult;

        //LOGS
        DateTime _startTime;
        DateTime _endTime;

        //verbose
        List<double[,]> posteriorHist = new List<double[,]>();
        List<SARPoint> positionHist = new List<SARPoint>();
        List<SARPoint> goalSelectedHist = new List<SARPoint>();

        //non verbose
        int searchSteps;//f
        TimeSpan searchDuration;//f
        double coverage;//f
        int avgMultiplicity;//f
        List<decimal> dangerHist = new List<decimal>();

        //analisi rapida
        bool timeout = false;

        public SimulationLogger(string instanceId, int instanceMID, SARGrid environment, string logDir, bool verbose)
        {
            this.instanceID = instanceId;
            this.instanceMID = instanceMID;
            _env = environment;
            _logDirectory = logDir;
            _verboseMode = verbose;
        }

        #region Log parametri
        public void LogPosterior(SARGrid env)
        {
            var gridPost = new double[env._numCol, env._numRow];

            for (int y = 0; y < env._numRow; y++)
            {
                for (int x = 0; x < env._numCol; x++)
                {
                    gridPost[x, y] = env._grid[x, y].Confidence;
                }
            }

            posteriorHist.Add(gridPost);
        }
        public void LogPosition(SARPoint positionSnap)
        {
            positionHist.Add(positionSnap);
        }
        public void LogGoal(SARPoint goalSnap)
        {
            goalSelectedHist.Add(goalSnap);
        }
        public void LogDanger(decimal dangerSnap)
        {
            dangerHist.Add(dangerSnap);
        }

        public void LogMissionResult(SARMission.SARMission mission)
        {
            _missionResult = mission;
            LogRunningResults();
        }

        public void LogMissionStart()
        {
            _startTime = DateTime.Now;
        }
        public void LogMissionEnd()
        {
            _endTime = DateTime.Now;
        }
        public void LogMissionTimeout()
        {
            timeout = true;
        }

        private void LogRunningResults()
        {
            searchSteps = _missionResult.Route.Count;
            searchDuration = _endTime.Subtract(_startTime);
            coverage = GetCoverage();
            avgMultiplicity = GetMultiplicity();
        }
        private int GetMultiplicity()
        {
            var mult = new List<int>();

            foreach (var p in _missionResult.Route)
            {
                var m = _missionResult.Route.Where(x => x == p).Count();
                mult.Add(m);
            }

            return mult.Sum() / mult.Count();
        }
        private double GetCoverage()
        {
            var visited = new HashSet<SARPoint>();
            _missionResult.Route.ForEach(x => visited.Add(x));

            var envArea = _env._numCol * _env._numRow;

            double cov = (double)visited.Count / (double)envArea;

            return cov;

        } 
        #endregion

        public void SaveToFile()
        {
            //cartella log            
            var logFolder = Directory.CreateDirectory(Path.Combine(_logDirectory, instanceID, instanceMID.ToString()));

            //salvataggio files non verbose
            CreateNonVerboseFiles(logFolder);
            SaveNonVerbose(logFolder);

            //salvataggio files verbose
            if (_verboseMode)
            {
                SaveVerbose(logFolder);
            }

        }

        private void SaveVerbose(DirectoryInfo folder)
        {
            //cartella log
            var logFolder = folder;

            //file log posterior            
            //string p = $"PosteriorHist[{positionHist.Count}]{Environment.NewLine}";
            //posteriorHist.ForEach(x => 
            //{
            //    string post = string.Empty;
            //    foreach (var item in x)
            //    {
            //        post += $"{item.ToString("N3")} ";
            //    }

            //    p += post;
            //});

            //File.WriteAllText(Path.Combine(logFolder.FullName, "PosteriorHist.txt"), p);

            //file log position
            string pos = $"PositionHist{Environment.NewLine}" +
                $"X Y{Environment.NewLine}";
            positionHist.ForEach(x => pos += $"{x.X} {x.Y}{Environment.NewLine}");

            File.WriteAllText(Path.Combine(logFolder.FullName, "PositionHist.txt"), pos);

            //file log goal
            string g = $"GoalHist{Environment.NewLine}" +
                $"X Y{Environment.NewLine}";
            goalSelectedHist.ForEach(x => g += $"{x.X} {x.Y}{Environment.NewLine}");

            File.WriteAllText(Path.Combine(logFolder.FullName, "GoalHist.txt"), g);
        }

        private void CreateNonVerboseFiles(DirectoryInfo folder)
        {
            //cartella log
            var logFolder = folder;

            //genero files vuoti per i log
            File.WriteAllText(Path.Combine(logFolder.FullName, "SearchPositions.txt"), $"label X Y{Environment.NewLine}");
            File.WriteAllText(Path.Combine(logFolder.FullName, "SearchSteps.txt"), $"SearchSteps{Environment.NewLine}");
            File.WriteAllText(Path.Combine(logFolder.FullName, "SearchDuration.txt"), $"SearchDuration{Environment.NewLine}");
            File.WriteAllText(Path.Combine(logFolder.FullName, "Coverage.txt"), $"Coverage%{Environment.NewLine}");
            File.WriteAllText(Path.Combine(logFolder.FullName, "AvgMultiplicity.txt"), $"AvgMultiplicity{Environment.NewLine}");
            File.WriteAllText(Path.Combine(logFolder.FullName, "DangerHist.txt"), $"Danger{Environment.NewLine}");

        }

        private void SaveNonVerbose(DirectoryInfo folder)
        {
            var logFolder = folder;

            var p = $"Start {_missionResult.Route[0].X} {_missionResult.Route[0].Y}{Environment.NewLine}" +
                $"End {_missionResult.Route.Last().X} {_missionResult.Route.Last().Y}{Environment.NewLine}" +
                $"Target {_env._realTarget.X} {_env._realTarget.Y} {Environment.NewLine}";
            File.AppendAllText(Path.Combine(logFolder.FullName, "SearchPositions.txt"), p);

            var s = $"{searchSteps}{Environment.NewLine}";
            File.AppendAllText(Path.Combine(logFolder.FullName, "SearchSteps.txt"), s);

            var t = $"{searchDuration}{Environment.NewLine}";
            File.AppendAllText(Path.Combine(logFolder.FullName, "SearchDuration.txt"), t);

            //file log coverage
            var c = $"{coverage}{Environment.NewLine}";
            File.AppendAllText(Path.Combine(logFolder.FullName, "Coverage.txt"), c);

            //file log multiplicity
            var m = $"{avgMultiplicity}{Environment.NewLine}";
            File.AppendAllText(Path.Combine(logFolder.FullName, "AvgMultiplicity.txt"), m);

            //log timeout
            var tout = "Timeout: " + timeout.ToString() + Environment.NewLine;
            if (timeout == true)
            {
                File.WriteAllText(Path.Combine(logFolder.FullName, "Timeout.txt"), timeout.ToString());
            }

            string i = p + "steps: " + s + "time: " + t + "coverage: " + c + "multeplicity: " + m + tout;
            File.WriteAllText(Path.Combine(logFolder.FullName, "SearchIndexes.txt"), i);


            //file log risk
            string r = string.Empty;
            dangerHist.ForEach(x => r += $"{x}{Environment.NewLine}");
            File.AppendAllText(Path.Combine(logFolder.FullName, "DangerHist.txt"), r);
        }
    }
}
