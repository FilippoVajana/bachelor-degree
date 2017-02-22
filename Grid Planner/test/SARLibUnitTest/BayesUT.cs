using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SARLib.Toolbox;
using SARLib.SAREnvironment;
using System.Diagnostics;

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

            //creo la sequenza degli input (letture del sensore)
            var inputData = new List<int>();
            var rnd = new Random(1);
            for (int i = 0; i < 30; i++)
            {
                inputData.Add(rnd.Next(0, 2));
            }

            var filtered = bayes.Filter(inputData, prior);
            Assert.AreEqual(0.640.ToString("N3"), filtered.ToString("N3"));
        }

        [TestMethod]
        public void ConfidencePropagation()
        {
            //var bayesEngine = new BayesEngine();
            var bayes = new BayesEngine.BayesFilter(0.2, new BayesEngine.Logger());
            var prior = 0.8;

            //creo ambiente
            var envGrid = new SARGrid(5, 10);
            Debug.WriteLine(envGrid.ConvertToConsoleString());

            //imposto punto di sensing #1
            var trueTarget = envGrid.GetPoint(1, 3);
            trueTarget.Type = SARPoint.PointTypes.Target;
            trueTarget.Confidence = (int) (prior * 10);
            envGrid._targets.Add(trueTarget);

            //imposto punto di sensing #2
            var falseTarget = envGrid.GetPoint(0, 9);
            falseTarget.Type = SARPoint.PointTypes.Clear;
            falseTarget.Confidence = (int) (prior * 10);

            var updatedGrid = bayes.UpdateConfidence(envGrid, trueTarget);

            var viewer = new SARViewer(updatedGrid);
            viewer.DisplayProperty(SARViewer.SARPointAttributes.Confidence);
            //updatedGrid.SaveToFile(@"C:\Users\filip\Dropbox\Unimi\pianificazione\Grid Planner\test\SARLibUnitTest");
            
        }
    }
}
