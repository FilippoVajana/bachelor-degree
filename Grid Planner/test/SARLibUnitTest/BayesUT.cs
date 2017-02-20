using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SARLib.Toolbox;

namespace SARLibUnitTest
{
    [TestClass]
    public class BayesUT
    {

        [TestMethod]
        public void FilterSingleInput()
        {
            var bayes = new BayesFilter(0.1, 0.4);
            var prior = new Dictionary<int, double>()
            {
                {1, 0.9 },
                {0, 0.1 }
            };

            var filtered = bayes.Filter(0, prior);
            Assert.AreEqual(0.307692307692308.ToString("N3"), filtered.ToString("N3"));
        }

        [TestMethod]
        public void FilterMultipleInput()
        {
            var bayes = new BayesFilter(0.1, 0.4);
            var prior = new Dictionary<int, double>()
            {
                {1, 0.9 },
                {0, 0.1 }
            };

            //creo lista di interi come input
            var inputData = new List<int>();
            var rnd = new Random(1);
            for (int i = 0; i < 10; i++)
            {
                inputData.Add(rnd.Next(0, 2));
            }

            var filtered = bayes.Filter(inputData, prior);
            //Assert.AreEqual(0.ToString("N3"), filtered.ToString("N3"));
        }
    }
}
