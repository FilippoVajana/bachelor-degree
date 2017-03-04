using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.IO;
using SARLib.SAREnvironment;

namespace SARLib.Toolbox
{
    public class BayesEngine
    {

        public class BayesFilter
        {
            //Dictionary<int, double> _likelihood; //d,h -> p(d|h)            
            double FILTER_ALFA = 0; //falsi negativi
            double FILTER_BETA = 0; //falsi positivi
            Dictionary<int, Func<int, double>> FILTER_MODEL = null; //modello del filtro di Bayes

            //public BayesFilter(double errorRate)
            //{                
            //    //Likelihood
            //    _likelihood = new Dictionary<int, double>()
            //    {
            //        {1, 1 - errorRate }, //p(1|1)
            //        {0, errorRate }, //p(1|0)           
            //    };

            //    _logger = logger;
            //}

            public BayesFilter(double falseNegRatio, double falsePosRatio)
            {
                FILTER_ALFA = falseNegRatio;
                FILTER_BETA = falsePosRatio;

                #region Modello del filtro
                ///<summary>
                ///Funzione per modellare i falsi negativi
                ///</summary>
                Func<int, double> pos_H = delegate (int input)
                        {
                            return ((1 - input) * FILTER_ALFA + input * (1 - FILTER_ALFA));
                        };
                ///<summary>
                ///Funzione per modellare i falsi positivi
                /// </summary>
                Func<int, double> neg_H = delegate (int input)
                {
                    return ((1 - input) * (1 - FILTER_BETA) + input * FILTER_BETA);
                };                 

                FILTER_MODEL = new Dictionary<int, Func<int, double>>
                {
                    { 1, pos_H },
                    { 0, neg_H }
                };
                #endregion
            }

            Func<SARGrid, IPoint, int> simulate_sensor_confidence_reading = delegate (SARGrid env, IPoint sensePoint)
            {
                sensePoint = env.GetPoint(sensePoint.X, sensePoint.Y);
                if (env._realTargets.Contains(sensePoint))
                    return 1;
                else
                    return 0;                
            };

            /// <summary>
            /// Calcola Confidence di un punto a seguito di una lettura sensoriale
            /// </summary>
            /// <param name="sensePoint"></param>
            /// <param name="updatingPoint"></param>
            /// <param name="senseResult"></param>
            /// <returns></returns>
            public double ComputeConfidencePosterior(SARPoint sensePoint, SARPoint updatingPoint, int senseResult)
            {
                var sensePost = sensePoint.Confidence;
                var updatingPrior = updatingPoint.Confidence;

                //verifico che il punto da aggiornare non sia un ostacolo
                if (updatingPoint.Type == SARPoint.PointTypes.Obstacle)
                    return 0;

                if (sensePoint == updatingPoint)
                {
                    var num = FILTER_MODEL[1](senseResult) * updatingPrior;
                    var denom = FILTER_MODEL[0](senseResult) * (1 - sensePost) + FILTER_MODEL[1](senseResult) * sensePost;

                    return (num / denom);
                }
                else
                {
                    var num = FILTER_MODEL[0](senseResult) * updatingPrior;
                    var denom = FILTER_MODEL[0](senseResult) * (1 - sensePost) + FILTER_MODEL[1](senseResult) * sensePost;

                    return (num / denom);
                }

            }

            /// <summary>
            /// Calcola aggiornamento Confidence dell'ambiente a seguito di una lettura sensoriale
            /// </summary>
            /// <param name="environment"></param>
            /// <param name="sensePoint"></param>
            /// <returns></returns>
            public SARGrid UpdateEnvironmentConfidence(SARGrid environment, IPoint sensePoint)
            {
                var senseResult = simulate_sensor_confidence_reading(environment, sensePoint);

                //aggiornamento posterior per sensePoint
                var senseP = environment.GetPoint(sensePoint.X, sensePoint.Y);
                senseP.Confidence = ComputeConfidencePosterior(senseP, senseP, senseResult);

                //aggiornamento posterior per l'ambiente
                foreach (var point in environment._grid)
                {
                    point.Confidence = ComputeConfidencePosterior(senseP, point, senseResult);
                }

                return environment;
            }
            public SARGrid UpdateEnvironmentConfidence_SPY(SARGrid environment, IPoint sensePoint, int senseResult)
            {
                //var senseResult = simulate_sensor_confidence_reading(environment, sensePoint);

                //aggiornamento posterior per sensePoint
                var senseP = environment.GetPoint(sensePoint.X, sensePoint.Y);
                senseP.Confidence = ComputeConfidencePosterior(senseP, senseP, senseResult);

                //aggiornamento posterior per l'ambiente
                foreach (var point in environment._grid)
                {
                    point.Confidence = ComputeConfidencePosterior(senseP, point, senseResult);
                }

                return environment;
            }

            public override string ToString()
            {
                return $"False Positive Ratio: {FILTER_BETA}{Environment.NewLine}" +
                    $"False Negative Ratio: {FILTER_ALFA}";
            }

            //private double Filter(int input, double prior) //prior = p(H=1)
            //{
            //    //tabella della prior
            //    var pH = new Dictionary<int, double>()
            //    {
            //        {1, prior },
            //        {0, 1 - prior },
            //    };

            //    //calcolo p(D)
            //    double pD = 0;
            //    foreach (var e in _likelihood)
            //    {
            //        pD += e.Value * pH[e.Key];
            //    }

            //    //calcolo posterior = p(1!D)
            //    double posterior = (_likelihood[input] * pH[input]) / pD;
            //    //logging
            //    _logger?.LogPosterior(input, prior, posterior, _likelihood[1]);

            //    return posterior;
            //}

            ///// <summary>
            ///// Applicazione Teorema di Bayes per aggiornamento della posterior
            ///// </summary>
            ///// <param name="input"></param>
            ///// <param name="prior"></param>
            ///// <returns></returns>
            //public double Filter(List<int> input, double prior)
            //{
            //    double finalPosterior = 0;
            //    foreach (int data in input)
            //    {
            //        //filtro dato in input
            //        var post = Filter(data, prior);

            //        //aggiorno la prior per il prossimo ciclo
            //        prior = post;
                    
            //        //salvo la posterior attuale
            //        finalPosterior = post;                    
            //    }

            //    //logging
            //    _logger?.SaveFile();

            //    return finalPosterior;
            //}

            ///// <summary>
            ///// Aggiorna la distribuzione di probabilità nell'ambiente di ricerca del parametro Confidence
            ///// </summary>
            ///// <param name="environment"></param>
            ///// <param name="sensePoint"></param>
            ///// <returns></returns>
            //public SARGrid UpdateConfidence(SARGrid environment, IPoint sensePoint)
            //{
            //    Func<double, double, double, string> PrintUpdateParameters = delegate (double pr, double d, double post)
            //    {
            //        string result = string.Empty;
            //        result = string.Format("UPDATING CONFIDENCE\n" +
            //            "POINT: ({0},{1})\n" +
            //            "PRIOR: {2:0.000}\n" +
            //            "DELTA: {3:0.000}\n" +
            //            "POSTERIOR: {4:0.000}\n", sensePoint.X, sensePoint.Y, pr, d, post);

            //        return result;
            //    };

            //    ///1- lettura prior cella p(H)
            //    var prior = environment.GetPoint(sensePoint.X, sensePoint.Y).Confidence;

            //    ///2- lettura presenza target D (lista targets)
            //    var sensorRead = (environment._realTargets.Contains(sensePoint)) ? 1 : 0; //OMG!! ;(

            //    ///3- calcolo posterior p(H|D) con Bayes
            //    var posterior = Filter(sensorRead, prior);
            //    environment.GetPoint(sensePoint.X, sensePoint.Y).Confidence = posterior;

            //    ///4- ottengo una copia della griglia ambiente
            //    SARPoint[,] envGrid = (SARPoint[,]) environment._grid.Clone();

            //    ///5- aggiornamento prior per i POI (?come?)
            //    var delta = posterior - prior; //valuto Δp nella posizione di rilevamento

            //    //DEBUG
            //    Debug.WriteLine(PrintUpdateParameters(prior, delta, posterior));

            //    foreach (var cell in envGrid)
            //    {
            //        if (cell.Type != SARPoint.PointTypes.Obstacle)
            //        {
            //            //calcolo entità aggiornamento                    
            //            double post = ComputePosteriorPropagation(cell, delta, environment.Distance(sensePoint, cell));

            //            //attuo l'aggiornamento della probabilità                    
            //            environment.GetPoint(cell.X, cell.Y).Confidence = post; //provvisorio - portare a double/decimal 
            //        }
            //    }
                
            //    return environment;
            //}

            //#region Formule propagazione aggiornamento posterior
            //private Func<double, double, int, double> NegDeltaProp = delegate (double Pk, double dPn, int distance)
            //    {
            //        var norm = Math.Abs(dPn / Math.Sqrt(distance));
            //        return Pk + (1 - Pk) * norm;
            //    };
            //private Func<double, double, int, double> PosDeltaProp = delegate (double Pk, double dPn, int distance)
            //{
            //    var norm = Math.Abs(dPn / Math.Sqrt(distance));
            //    return Pk - Pk * norm;
            //    //return Pk - (1 - Pk) * (norm);
            //};                        
            //#endregion

            //private double ComputePosteriorPropagation(SARPoint t, double delta, int distance)
            //{
            //    //discrimino sul valore del delta
            //    if (delta >= 0)
            //    {
            //        var d = PosDeltaProp(t.Confidence, delta, distance);
            //        return d;
            //    }
            //    else
            //    {
            //        var d = NegDeltaProp(t.Confidence, delta, distance);
            //        return d;
            //    }
            //}            
        }        
    }

    
}
