using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace SARLib.Toolbox
{
    public class BayesianInferenceEngine
    {

    }

    class BayesFilter
    {        
        double[] H;
        double prior; //p(H) uniforme sugli Hi
        double likelihood; //(1 - a)
        double pD; //p(D)

        public BayesFilter(double likelihood_parm)
        {   
            H = new double[]{ 0, 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1 };
            prior = 1 / H.Length;
            likelihood = 1 - likelihood_parm;

            H.ToList<double>().ForEach(x => pD += likelihood * prior); //calcolo pD
        }

        public double Filter(double input)
        {            
            IDictionary<double, double> posterior = new Dictionary<double, double>(H.Length);
            
            foreach (var Hi in H)
            {
                var post = (likelihood * prior) / pD;
                posterior.Add(Hi, post);
            }

            return posterior.OrderByDescending(x => x.Value).ElementAt(0).Value;
        }
    }
}
