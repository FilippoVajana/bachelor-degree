using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Diagnostics;

namespace SARLib.Toolbox
{
    public class BayesianInferenceEngine
    {

    }

    public class BayesFilter
    {
        Dictionary<int, double> sensorModel; //d,h -> p(d|h)
        
        public BayesFilter(double errorPOS, double errorNEG)
        {
            sensorModel = new Dictionary<int, double>()
            {
                {1, errorPOS }, //p(1|0)
                {0, errorNEG },     //p(0|1)           
            };            
        }       

        public double Filter(int input, Dictionary<int, double> prior) //state == prior
        {
            //aggiorno p(D)
            double pr_D = 0;
            foreach (var h in prior)
            {
                pr_D += sensorModel[h.Key] * h.Value; 
            }

            double posterior = sensorModel[input] * prior[input] / pr_D;

            //DEBUG
            Debug.WriteLine($"IN: {input}\t" +
                $"p(H): {prior[input]}\t" +
                $"p(D|H): {sensorModel[input]}\t" +
                $"p(D): {pr_D}\n" +
                $"-----------\n" +
                $"p(H|D): {posterior}");
            return posterior; //p(H=1|D)
        }
    }
}
