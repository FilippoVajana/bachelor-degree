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
        #region Parametri
        string runLogFolder;
        bool verbose;
        public string instanceID;
        public int instanceMID;
        SARGrid environment;
        SARMission.SARMission missionResult;

        //log temporali
        long runDurationTicks;
        //DateTime _startTime;
        //DateTime _endTime;

        //verbose        
        string posteriorHPath = string.Empty;
        StreamWriter posteriorSW = null;
        List<SARPoint> positionHist = new List<SARPoint>();
        List<SARPoint> goalSelectedHist = new List<SARPoint>();

        //non verbose
        int searchSteps;//f
        //TimeSpan searchDuration;//f
        double coverage;//f
        int avgMultiplicity;//f
        StreamWriter riskSW = null;
        bool missionSuccess = false;

        //analisi rapida
        bool timeout = false; 
        #endregion

        public SimulationLogger(string instanceId, int instanceMID, SARGrid environment, string logDir, bool verbose)
        {
            this.instanceID = instanceId;
            this.instanceMID = instanceMID;
            this.environment = environment;
            this.verbose = verbose;

            //creazione folder per i log della run
            runLogFolder = Directory.CreateDirectory(Path.Combine(logDir, instanceID, instanceMID.ToString())).FullName;
            
            //salvataggio ambiente iniziale            
            Saver.SaveToJsonFile(this.environment, runLogFolder, "env");

            //creazione file per il log di dangerHistory
            var riskHeader = "Danger";
            riskSW = File.CreateText(Path.Combine(runLogFolder, "riskHistory.txt"));
            riskSW.WriteLine(riskHeader);
                       
            //creazione file per il log di posteriorHistory
            if(this.verbose)
            {                
                //header
                var posteriorHeader = $"Rows{this.environment._numRow}, Cols{this.environment._numCol}, [Matlab] reshape(P,[{this.environment._numRow},{this.environment._numCol}]){Environment.NewLine}";

                //file                
                posteriorSW = File.CreateText(Path.Combine(runLogFolder, "posteriorHistory.txt"));
                posteriorSW.WriteLine(posteriorHeader);
            }
        }

        #region Log parametri
        public void LogPosterior(SARGrid env)
        {      
            //salvataggio del file
            if(verbose)
            {
                //posterior
                foreach (var e in env._grid)
                {
                    posteriorSW.Write($"{e.Confidence.ToString("N1")} ");
                }
                posteriorSW.WriteLine();                
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
            riskSW.WriteLine(dangerSnap);
        }

        public void LogMissionResult(SARMission.SARMission mission)
        {
            missionResult = mission;
            ExtractRunResult();
        }

        public void LogCicleExecutionTime(long cicleExecTime)
        {
            runDurationTicks += cicleExecTime;
        }
        
        public void LogMissionTimeout()
        {
            timeout = true;
        }

        /// <summary>
        /// Estrae i dati per la singola run
        /// </summary>
        private void ExtractRunResult()
        {
            searchSteps = missionResult.Route.Count;
            //searchDuration = runDuration;
            coverage = GetCoverage();
            avgMultiplicity = GetMultiplicity();

            //controllo successo missione            
            if (missionResult.Route.Last() == environment._realTarget)
            {
                missionSuccess = true;
            }
        }
        
        /// <summary>
        /// Estrae il sommario della simulazione
        /// </summary>
        public static void ExtractSimulationResult(string simulationLogFolder)
        {
            //creo file sommario
            var simSummary = File.CreateText(Path.Combine(simulationLogFolder, "simulation_summary.txt"));//file
            var simSummaryHeader = "Steps_Num Coverage% AvgMultiplicity Duration Success Xs Ys Xe Ye Xt Yt Timeout M_Size Prior_Type Danger_Type Risk_Coeff Danger_Threshold";//header provvisiorio
            simSummary.WriteLine(simSummaryHeader);

            //lettura file summary per le singole istanze di simulazione
            var summaryFiles = Directory.GetFiles(simulationLogFolder, "summary.txt", SearchOption.AllDirectories);
            Console.WriteLine($"Generating Simulation Summary . . .{Environment.NewLine}" +
                $"Found {summaryFiles.Count()} Files");

            //scrittura dati
            foreach (var file in summaryFiles)
            {
                try
                {
                    //leggo file
                    var fileLines = File.ReadAllLines(file);

                    //salvo valori
                    simSummary.WriteLine(fileLines[1]);
                }
                catch (Exception w_ex)
                {
                    continue;
                }
            }

            simSummary.Flush();
            simSummary.Dispose();
        }

        private int GetMultiplicity()
        {
            var mult = new List<int>();

            foreach (var p in missionResult.Route)
            {
                var m = missionResult.Route.Where(x => x == p).Count();
                mult.Add(m);
            }

            return mult.Sum() / mult.Count();
        }
        private double GetCoverage()
        {
            var visited = new HashSet<SARPoint>();
            missionResult.Route.ForEach(x => visited.Add(x));

            var envArea = environment._numCol * environment._numRow;

            double cov = (double)visited.Count / (double)envArea;

            return cov*100;

        }
        #endregion

        public void SaveLogs()
        {
            Console.WriteLine($"[{DateTime.Now.ToLocalTime()}] SAVING LOGS FOR INSTANCE {instanceID}({instanceMID})");

            //cartella log            
            //var logFolder = Directory.CreateDirectory(Path.Combine(_logDirectory, instanceID, instanceMID.ToString()));

            //salvataggio files non verbose
            SaveNormalLogs();

            //salvataggio files verbose
            if (verbose)
            {
                SaveVerbose();
            }

            posteriorSW?.Dispose();
            riskSW?.Dispose();
            Console.WriteLine($"[{DateTime.Now.ToLocalTime()}] SAVED LOGS FOR INSTANCE {instanceID}({instanceMID})");            
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
        
        private void SaveNormalLogs()
        {
            //headers per i file
            var summary = $"Steps_Num Coverage% AvgMultiplicity Duration Success Xs Ys Xe Ye Xt Yt Timeout M_Size Prior_Type Danger_Type Risk_Coeff Danger_Threshold{Environment.NewLine}";
            //var riskH = $"Risk{Environment.NewLine}";

            //percorso file
            var summaryPath = Path.Combine(runLogFolder, "summary.txt");
            //var riskHistPath = Path.Combine(folder.FullName, "riskHistory.txt");
            //var startEnvPath = Path.Combine(folder.FullName, "environment.json");

            //generazione contenuti
            //summary
            var startX = positionHist[0].X;
            var startY = positionHist[0].Y;
            var endX = positionHist.Last().X;
            var endY = positionHist.Last().Y;
            var tgtX = environment._realTarget.X;
            var tgtY = environment._realTarget.Y;
            var instanceIDComponents = instanceID.Split('_');
            var runDuration = TimeSpan.FromTicks(runDurationTicks).TotalSeconds;
            summary += $"{searchSteps} " +
                $"{coverage} " +
                $"{avgMultiplicity} " +
                $"{runDuration} " +
                $"{missionSuccess} " +
                $"{startX} " +
                $"{startY} " +
                $"{endX} " +
                $"{endY} " +
                $"{tgtX} " +
                $"{tgtY} " +
                $"{timeout} " +
                $"{instanceIDComponents[1]} " +
                $"{instanceIDComponents[2]} " +
                $"{instanceIDComponents[3]} " +
                $"{instanceIDComponents[5]} " +
                $"{instanceIDComponents[6]} ";

            //riskHistory
            //dangerHist.ForEach(x => riskH += $"{x}{Environment.NewLine}");
            

            //salvataggio dei log su file
            WriteLogFile(summaryPath, summary);
            //WriteLogFile(riskHistPath, riskH);            
        }

        private void SaveVerbose()
        {
            //headers dei file            
            var positionH = $"Xp Yp{Environment.NewLine}";
            var goalH = $"Xg Yg{Environment.NewLine}";

            //percorso file            
            var positionHPath = Path.Combine(runLogFolder, "positionHistory.txt");
            var goalHPath = Path.Combine(runLogFolder, "goalHistory.txt");

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
