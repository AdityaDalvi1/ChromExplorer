using System.Collections.Generic;
using ChromExplorer.ModasReader;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChromExplorer.Modules.ModasChromProvider.Tests
{
    [TestClass]
    public class MethodFileLiteTests
    {
        private string testFile = @"c:\Users\knovoselov\Documents\ITMS\Data\TestReader.db3";
        private IList<Method> _methods;


        //[TestInitialize]
        //public void Init()
        //{
        //    var reader = new ModasDataReader(testFile);
        //    _methods = reader.GetMethods();    
        //}

        //[TestMethod]
        //public void MassRangeTest()
        //{
        //    var method = new MethodFileLite(_methods[0]);

        //    Assert.AreEqual(250, method.MFrom);
        //    Assert.AreEqual(1000.0, method.MTo);
        //}
    }
}
