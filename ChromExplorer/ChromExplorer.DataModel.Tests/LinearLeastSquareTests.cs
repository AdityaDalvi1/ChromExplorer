using System;
using MathNet.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChromExplorer.DataModel.Tests
{
    [TestClass]
    public class LinearLeastSquareTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            var x = new[] {1.0, 2.0, 3.0, 4.0, 5.0, 6.0};
            var y = new[] {4.986, 2.347, 2.061, -2.995, 2.352, -5.782};

            var p = Fit.Line(x, y);
            var offset = p.Item1;
            var slope = p.Item2;
        }

        [TestMethod]
        public void SteroidDataTest()
        {
            var x = new[]
            {
                1386441.67,
                956505.72,
                531347.95,
                243328.14,
                1859642.71,
                884532.77,
                515035.06,
                4629083.36
            };

            var y = new[]
            {
                5.00,
                2.50,
                1.25,
                0.63,
                5.00,
                2.50,
                1.25,
                12.00
            };

            var p = Fit.Line(x, y);
            var offset = p.Item1;
            var slope = p.Item2;

            Console.WriteLine(@"Intesect {0}, slope {1}", offset, slope);

            var computed = new double[x.Length];

            for (int i = 0; i < x.Length; i++)
            {
                computed[i] = slope*x[i] + offset;
            }

            Console.WriteLine("R^2 = {0}", GoodnessOfFit.RSquared(computed, y));

        }
    }
}