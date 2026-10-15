using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChromExplorer.ModasReader.Tests
{
    [TestClass]
    public class RemoveFromArrayTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            var longArray = new List<int>()
                                {
                                    1,
                                    2,
                                    3,
                                    4,
                                    5,
                                    6,
                                    7,
                                    8
                                };

            var shortArray = new List<int>()
                                 {
                                     1, 2, 3, 4
                                 };

            longArray.RemoveRange(shortArray.Count, longArray.Count - shortArray.Count);
        }
    }
}
