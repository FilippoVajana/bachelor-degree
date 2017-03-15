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

        //log temporali
        DateTime _startTime;
        DateTime _endTime;

        //verbose
        //List<double[,]> posteriorHist = new List<double[,]>();
        string posteriorHPath = string.Empty;
        StreamWriter posteriorFile = null;
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

            if(_verboseMode)
            {
                //cartella log            
                var logFolder = Directory.CreateDirectory(Path.Combine(_logDirectory, instanceID, instanceMID.ToString()));

                //header
                var posteriorH = $"Rows{_env._numRow}, Cols{_env._numCol}, [Matlab] reshape(P,[{_env._numRow},{_env._numCol}]){Environment.NewLine}";

                //file
                posteriorHPath = Path.Combine(logFolder.FullName, "posteriorHistory.txt");
                posteriorFile = File.CreateText(posteriorHPath);
                posteriorFile.WriteLine(posteriorH);
            }
        }

        #region Log parametri
        public void LogPosterior(SARGrid env)
        {      
            //salvataggio del file
            if(_verboseMode)
            {
                //posterior
                foreach (var e in env._grid)
                {
                    posteriorFile.Write($"{e.Confidence.ToString("N3")} ");
                }
                posteriorFile.WriteLine();                
            }
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
            ExtractRunResult();
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

        private void ExtractRunResult()
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

            return cov*100;

        }
        #endregion

        public void SaveLogs()
        {
            Console.WriteLine($"SAVING LOGS FOR INSTANCE {instanceID}({instanceMID})");

            //cartella log            
            var logFolder = Directory.CreateDirectory(Path.Combine(_logDirectory, instanceID, instanceMID.ToString()));

            //salvataggio files non verbose
            SaveNormalLogs(logFolder);

            //salvataggio files verbose
            if (_verboseMode)
            {
                SaveVerbose(logFolder);
            }

            posteriorFile.Dispose();
            Console.WriteLine($"SAVED LOGS FOR INSTANCE {instanceID}({instanceMID})");
        }

        private void WriteLogFile(string dst, string content)
        {
            //creazione file
            var fileWriter = File.CreateText(dst);

            //salvataggio informazioni
            fileWriter.WriteLine(content);
            fileWriter.Flush();

            //dispose
            fileWriter.Dispose();            
        }
        
        private void SaveNormalLogs(DirectoryInfo folder)
        {
            //headers per i file
            var summary = $"Steps_Num Coverage% AvgMultiplicity Duration Xs Ys Xe Ye Xt Yt Timeout{Environment.NewLine}";
            var riskH = $"Risk{Environment.NewLine}";

            //percorso file
            var summaryPath = Path.Combine(folder.FullName, "summary.txt");
            var riskHistPath = Path.Combine(folder.FullName, "riskHistory.txt");

            //generazione contenuti
            //summary
            var startX = positionHist[0].X;
            var startY = positionHist[0].Y;
            var endX = positionHist.Last().X;
            var endY = positionHist.Last().Y;
            var tgtX = _env._realTarget.X;
            var tgtY = _env._realTarget.Y;

            summary += $"{searchSteps} " +
                $"{coverage} " +
                $"{avgMultiplicity} " +
                $"{searchDuration} " +
                $"{startX} " +
                $"{startY} " +
                $"{endX} " +
                $"{endY} " +
                $"{tgtX} " +
                $"{tgtY} " +
                $"{timeout}";

            //riskHistory
            dangerHist.ForEach(x => riskH += $"{x}{Environment.NewLine}");

            //salvataggio dei log su file
            WriteLogFile(summaryPath, summary);
            WriteLogFile(riskHistPath, riskH);
        }

        private void SaveVerbose(DirectoryInfo folder)
        {
            //headers dei file            
            var positionH = $"Xp Yp{Environment.NewLine}";
            var goalH = $"Xg Yg{Environment.NewLine}";

            //percorso file            
            var positionHPath = Path.Combine(folder.FullName, "positionHistory.txt");
            var goalHPath = Path.Combine(folder.FullName, "goalHistory.txt");

            //generazione contenuti
            
            //position
            foreach (var e in positionHist)
            {
                positionH += $"{e.X} {e.Y}{Environment.NewLine}";
            }

            //goals
            foreach (var e in goalSelectedHist)
            {
                goalH += $"{e.X} {e.Y}{Environment.NewLine}";
            }

            //salvataggio file
            WriteLogFile(positionHPath, positionH);
            WriteLogFile(goalHPath, goalH);            
        }
    }
}
