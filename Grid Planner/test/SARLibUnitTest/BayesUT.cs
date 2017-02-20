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
        public void BayesFilterTest()
        {
            var bayes = new BayesFilter(0.1, 0.4);
            var prior = new Dictionary<int, double>()
            {
                {1, 0.9 },
                {0, 0.1 }
            };
            var filtered = bayes.Filter(0, prior);
            
        }
    }
}
