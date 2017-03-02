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
        private void DebugConsolePrint(SARViewer viewer, SARGrid environment)
        {
            viewer = new SARViewer();
            viewer.DisplayProperty(environment, SARViewer.SARPointAttributes.Confidence);
            viewer.DisplayProperty(environment, SARViewer.SARPointAttributes.Danger);
            //updatedGrid.SaveToFile(@"C:\Users\filip\Dropbox\Unimi\pianificazione\Grid Planner\test\SARLibUnitTest");
        }

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
        public void PositiveConfidenceDeltaPropagation()
        {
            //var bayesEngine = new BayesEngine();
            var bayes = new BayesEngine.BayesFilter(0.2, new BayesEngine.Logger());
            var prior = 0.8;

            //creo ambiente
            var envGrid = new SARGrid(5, 10);
            var viewer = new SARViewer();
            //Debug.WriteLine(envGrid.ConvertToConsoleString());

            //imposto punto di sensing #1
            var p = envGrid.GetPoint(0, 0);
            var trueTarget = envGrid.BuildSARPoint(p.X, p.Y, prior, p.Danger, SARPoint.PointTypes.Target);            
            
            //imposto punto di sensing #2
            p = envGrid.GetPoint(4, 9);
            var falseTarget = envGrid.BuildSARPoint(p.X, p.Y, prior, p.Danger, SARPoint.PointTypes.Clear);            
            
            //visualizzo la griglia prima dell'aggiornamento
            viewer.DisplayProperty(envGrid, SARViewer.SARPointAttributes.Confidence);

            //aggiorno la griglia
            var updatedGrid = bayes.UpdateConfidence(envGrid, trueTarget);

            Assert.AreEqual(0.941.ToString("N3"), trueTarget.Confidence.ToString("N3"));
            Assert.AreEqual(0.769.ToString("N3"), falseTarget.Confidence.ToString("N3"));

            //DEBUG
            DebugConsolePrint(viewer, updatedGrid);
        }

        [TestMethod]
        public void NegativeConfidenceDeltaPropagation()
        {
            //var bayesEngine = new BayesEngine();
            var bayes = new BayesEngine.BayesFilter(0.2, new BayesEngine.Logger());
            var prior = 0.8;

            //creo ambiente
            var envGrid = new SARGrid(5, 10);
            var viewer = new SARViewer();
            //Debug.WriteLine(envGrid.ConvertToConsoleString());

            //imposto punto di sensing #1
            var p = envGrid.GetPoint(0, 0);
            var trueTarget = envGrid.BuildSARPoint(p.X, p.Y, prior, p.Danger, SARPoint.PointTypes.Target);

            //imposto punto di sensing #2
            p = envGrid.GetPoint(4, 9);
            var falseTarget = envGrid.BuildSARPoint(p.X, p.Y, prior, p.Danger, SARPoint.PointTypes.Clear);

            //DEBUG
            //visualizzo la griglia prima dell'aggiornamento
            DebugConsolePrint(viewer, envGrid);

            //aggiorno la griglia
            var updatedGrid = bayes.UpdateConfidence(envGrid, falseTarget);

            Assert.AreEqual(0.841.ToString("N3"), trueTarget.Confidence.ToString("N3"));
            Assert.AreEqual(0.059.ToString("N3"), falseTarget.Confidence.ToString("N3"));

            //DEBUG
            DebugConsolePrint(viewer, updatedGrid);
        }

        [TestMethod]
        public void PositiveConfidenceDeltaPropagationGrid()
        {
            //var bayesEngine = new BayesEngine();
            var bayes = new BayesEngine.BayesFilter(0.2, new BayesEngine.Logger());
            var prior = 0.8;

            //creo ambiente
            var envGrid = new SARGrid(5, 10);
            envGrid.RandomizeGrid(10, 25);
            var viewer = new SARViewer();
            //Debug.WriteLine(envGrid.ConvertToConsoleString());

            //imposto punto di sensing #1
            var p = envGrid.GetPoint(0, 5);
            var trueTarget = envGrid.BuildSARPoint(p.X, p.Y, prior, p.Danger, SARPoint.PointTypes.Target);

            //aggiorno la griglia
            var updatedGrid = bayes.UpdateConfidence(envGrid, trueTarget);

            //DEBUG
            DebugConsolePrint(viewer, updatedGrid);
        }

        [TestMethod]
        public void NegativeConfidenceDeltaPropagationGrid()
        {
            //var bayesEngine = new BayesEngine();
            var bayes = new BayesEngine.BayesFilter(0.2, new BayesEngine.Logger());
            var prior = 0.8;

            //creo ambiente
            var envGrid = new SARGrid(5, 10);
            envGrid.RandomizeGrid(10, 25);
            var viewer = new SARViewer();
            //Debug.WriteLine(envGrid.ConvertToConsoleString());

            //imposto punto di sensing #1
            var p = envGrid.GetPoint(2, 5);
            var falseTarget = envGrid.BuildSARPoint(p.X, p.Y, prior, p.Danger, SARPoint.PointTypes.Clear);

            //aggiorno la griglia
            var updatedGrid = bayes.UpdateConfidence(envGrid, falseTarget);

            //DEBUG
            DebugConsolePrint(viewer, updatedGrid);
        }
    }
}
