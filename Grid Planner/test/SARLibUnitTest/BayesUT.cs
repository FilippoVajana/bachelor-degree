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
            var prior = 0.8;

            var filtered = bayes.Filter(new List<int>() { 1 }, prior);
            Assert.AreEqual(0.941.ToString("N3"), filtered.ToString("N3"));

            filtered = bayes.Filter(new List<int>() { 0 }, prior);
            Assert.AreEqual(0.059.ToString("N3"), filtered.ToString("N3"));
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
        public void PositiveConfidencePropagation()
        {
            //var bayesEngine = new BayesEngine();
            var bayes = new BayesEngine.BayesFilter(0.2, new BayesEngine.Logger());
            var prior = 0.8;

            //creo ambiente
            var envGrid = new SARGrid(5, 10);
            var viewer = new SARViewer(envGrid);            
            //Debug.WriteLine(envGrid.ConvertToConsoleString());

            //imposto punto di sensing #1
            var trueTarget = envGrid.GetPoint(0, 0);
            trueTarget.Type = SARPoint.PointTypes.Target;
            trueTarget.Confidence = prior;
            envGrid._targets.Add(trueTarget);

            //imposto punto di sensing #2
            var falseTarget = envGrid.GetPoint(4, 9);
            falseTarget.Type = SARPoint.PointTypes.Clear;
            falseTarget.Confidence = prior;

            //visualizzo la griglia prima dell'aggiornamento
            viewer.DisplayProperty(SARViewer.SARPointAttributes.Confidence);

            //aggiorno la griglia
            var updatedGrid = bayes.UpdateConfidence(envGrid, trueTarget);

            Assert.AreEqual(0.941.ToString("N3"), trueTarget.Confidence.ToString("N3"));
            Assert.AreEqual(0.769.ToString("N3"), falseTarget.Confidence.ToString("N3"));

            //DEBUG
            //visualizzo la griglia aggiornata
            viewer = new SARViewer(updatedGrid);
            viewer.DisplayProperty(SARViewer.SARPointAttributes.Confidence);
            viewer.DisplayProperty(SARViewer.SARPointAttributes.Danger);
            //updatedGrid.SaveToFile(@"C:\Users\filip\Dropbox\Unimi\pianificazione\Grid Planner\test\SARLibUnitTest");

        }

        [TestMethod]
        public void NegativeConfidencePropagation()
        {
            //var bayesEngine = new BayesEngine();
            var bayes = new BayesEngine.BayesFilter(0.2, new BayesEngine.Logger());
            var prior = 0.8;

            //creo ambiente
            var envGrid = new SARGrid(5, 10);
            var viewer = new SARViewer(envGrid);
            //Debug.WriteLine(envGrid.ConvertToConsoleString());

            //imposto punto di sensing #1
            var trueTarget = envGrid.GetPoint(0, 0);
            trueTarget.Type = SARPoint.PointTypes.Target;
            trueTarget.Confidence = prior;
            envGrid._targets.Add(trueTarget);

            //imposto punto di sensing #2
            var falseTarget = envGrid.GetPoint(4, 9);
            falseTarget.Type = SARPoint.PointTypes.Clear;
            falseTarget.Confidence = prior;

            //visualizzo la griglia prima dell'aggiornamento
            viewer.DisplayProperty(SARViewer.SARPointAttributes.Confidence);

            //aggiorno la griglia
            var updatedGrid = bayes.UpdateConfidence(envGrid, falseTarget);

            Assert.AreEqual(0.759.ToString("N3"), trueTarget.Confidence.ToString("N3"));
            Assert.AreEqual(0.059.ToString("N3"), falseTarget.Confidence.ToString("N3"));

            //DEBUG
            //visualizzo la griglia aggiornata
            viewer = new SARViewer(updatedGrid);
            viewer.DisplayProperty(SARViewer.SARPointAttributes.Confidence);
            viewer.DisplayProperty(SARViewer.SARPointAttributes.Danger);
            //updatedGrid.SaveToFile(@"C:\Users\filip\Dropbox\Unimi\pianificazione\Grid Planner\test\SARLibUnitTest");

        }
    }
}
