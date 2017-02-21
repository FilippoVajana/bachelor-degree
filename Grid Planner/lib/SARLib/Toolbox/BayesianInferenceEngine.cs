using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.IO;

namespace SARLib.Toolbox
{
    public class BayesEngine
    {
        public class BayesFilter
        {
            Dictionary<int, double> likelihood; //d,h -> p(d|h)
            Logger _logger;
            
            public BayesFilter(double errorRate, Logger logger)
            {                
                //Likelihood
                likelihood = new Dictionary<int, double>()
                {
                    {1, 1 - errorRate }, //p(1|1)
                    {0, errorRate }, //p(1|0)           
                };

                _logger = logger;
            }

            public override string ToString()
            {
                return $"p(D=1|H=1) {likelihood[1]}; p(D=1|H=0) {likelihood[0]}";
            }

            private double Filter(int input, double prior) //prior = p(H=1)
            {
                //tabella della prior
                var pH = new Dictionary<int, double>()
                {
                    {1, prior },
                    {0, 1 - prior },
                };

                //calcolo p(D)
                double pD = 0;
                foreach (var e in likelihood)
                {
                    pD += e.Value * pH[e.Key];
                }

                //calcolo posterior = p(1!D)
                double posterior = (likelihood[input] * pH[input]) / pD;
                //logging
                _logger?.LogPosterior(input, prior, posterior, likelihood[1]);

                return posterior;
            }

            /// <summary>
            /// Aggiorna la probabilità finale p(H=1) mediante l'applicazione del teorema di Bayes
            /// </summary>
            /// <param name="input"></param>
            /// <param name="prior"></param>
            /// <returns></returns>
            public double Filter(List<int> input, double prior)
            {
                double finalPosterior = 0;
                foreach (int data in input)
                {
                    //filtro dato in input
                    var post = Filter(data, prior);

                    //aggiorno la prior per il prossimo ciclo
                    prior = post;
                    
                    //salvo la posterior attuale
                    finalPosterior = post;                    
                }

                //logging
                _logger?.SaveFile();

                return finalPosterior;
            }

            public SAREnvironment.SARGrid UpdateTargetProbabilities(SAREnvironment.SARGrid environment, SAREnvironment.IPoint sensingPoint)
            {
                ///1- lettura prior cella p(H)
                ///2- lettura presenza target D (lista targets)
                ///3- calcolo posterior p(H|D) con Bayes
                ///4- lettura lista POI p(H=1) > soglia
                ///5- aggiornamento prior per i POI (?come?)
                ///
                return null;
            }
        }

        public class Logger
        {
            List<string> _logDiary;

            public Logger()
            {
                _logDiary = new List<string>();
            }
                        
            public void LogPosterior(int input, double prior, double posterior, double errorRate)
            {
                string log = string.Empty;
                log = $"{errorRate};{input};{prior};{posterior}";
                _logDiary.Add(log);
            }

            public void SaveFile()
            {
                //definizione file path
                var path = Path.GetFullPath(@"C:\Users\filip\Dropbox\Unimi\pianificazione\Grid Planner\lib\SARLib\Toolbox\Logs");
                path = Path.Combine(path, $"Bayeslog.txt");         

                //inserimento dati
                string log = string.Empty;
                foreach (var s in _logDiary)
                {
                    log += s + "\n";
                }

                File.WriteAllText(path, log);
            }
        }
    }

    
}
