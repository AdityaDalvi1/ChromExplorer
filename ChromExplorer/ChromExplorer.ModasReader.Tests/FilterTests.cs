using System;
using ChromExplorer.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mindscape.LightSpeed;

//System.Linq.Dynamic


namespace ChromExplorer.ModasReader.Tests
{
    [TestClass]
    public class FilterTests
    {
        private const string SourceFile = @"explorer_test.db3";

        [TestMethod]
        public void MethodIdFilterTest()
        {
            var reader = new ModasDataReader(SourceFile);
            int id = 2;

            var filter = Entity.Attribute("MethodId") == id;
            

            using(var uof = reader.Context.CreateUnitOfWork())
            {
                var records = uof.Find<Spectrum>(filter);

                foreach (var spectrum in records)
                {
                    Console.WriteLine(spectrum);
                }
            }
        }

        [TestMethod]
        public void ParseExpressionTest()
        {
            var reader = new ModasDataReader(SourceFile);
            int id = 2;

            string queryStr = "InjectionTime > 0.001";
            var expr = DynamicExpression.ParseLambda<Spectrum, bool>(queryStr); 

            //QueryExpression filter = expr;


            using (var uof = reader.Context.CreateUnitOfWork())
            {
                var records = uof.Find<Spectrum>() ;

                foreach (var spectrum in records)
                {
                    Console.WriteLine(spectrum);
                }
            }
        }

        [TestMethod]
        public void MethodFilterSettingsTest()
        {
            string file = "method_filter.db3";
            var reader = new ModasDataReader(file);

            var fs = reader.GetFilterSettings();

            Assert.IsNotNull(fs);
            Assert.IsNotNull(fs.Methods);
            Assert.AreEqual(3, fs.Methods.Count);
        }
    }
}
