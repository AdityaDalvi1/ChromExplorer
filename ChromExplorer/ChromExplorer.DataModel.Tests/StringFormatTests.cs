using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChromExplorer.DataModel.Tests
{

    [TestClass]
    public class StringFormatTests
    {
        string _format = "{0:#.#E+0}";

        [TestMethod]
        public void TestMethod1()
        {
            Console.WriteLine(_format, 3.1415533E+12);
            Console.WriteLine("{0:###.000E-00}", 3.1415533E+04);
            Console.WriteLine("{0:#.0000000E+000}", 2.553939939E+101);
        }
    }
}
