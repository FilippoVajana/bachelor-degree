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
            var bayes = new BayesEngine.BayesFilter(0.2, null);
            var prior = 0.9;

            var filtered = bayes.Filter(new List<int>() { 1 }, prior);
            Assert.AreEqual(0.973.ToString("N3"), filtered.ToString("N3"));

            filtered = bayes.Filter(new List<int>() { 0 }, prior);
            Assert.AreEqual(0.027.ToString("N3"), filtered.ToString("N3"));
        }

        [TestMethod]
        public void FilterMultipleInput()
        {
            var bayes = new BayesEngine.BayesFilter(0.2, new BayesEngine.Logger());
            var prior = 0.9;

            //creo lista di interi come input
            var inputData = new List<int>();
            var rnd = new Random(1);
            for (int i = 0; i < 30; i++)
            {
                inputData.Add(rnd.Next(0, 2));
            }

            var filtered = bayes.Filter(inputData, prior);
            //Assert.AreEqual(0.ToString("N3"), filtered.ToString("N3"));
        }
    }
}
