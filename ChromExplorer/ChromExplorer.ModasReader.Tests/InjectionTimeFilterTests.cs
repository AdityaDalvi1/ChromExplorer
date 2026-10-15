using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mindscape.LightSpeed;
using Mindscape.LightSpeed.Querying;

namespace ChromExplorer.ModasReader.Tests
{
    [TestClass]
    public class InjectionTimeFilterTests
    {
        private const string SourceFile = @"agc_on.mtd";

        [TestMethod]
        public void LoadFileTest()
        {
            var reader = new ModasDataReader(SourceFile);

            Assert.IsTrue(reader.IsContextInitialized);
            Assert.IsTrue(reader.IsModasFile);

            using (var uof = reader.Context.CreateUnitOfWork())
            {
                var result = uof.Find<Spectrum>();
            }

        }

        [TestMethod]
        public void SelectDistinInjectionTimesTest()
        {
            var reader = new ModasDataReader(SourceFile);

            Assert.IsTrue(reader.IsContextInitialized);
            Assert.IsTrue(reader.IsModasFile);

            var query = new Query(typeof(Spectrum));
            query.Projection.Add("InjectionTime");
            query.Distinct = true;

            using (var uof = reader.Context.CreateUnitOfWork())
            {
                var result = uof.Project<double>(query);
            }

        }

        [TestMethod]
        public void GetInjectionTimesTest()
        {
            var reader = new ModasDataReader(SourceFile);

            Assert.IsTrue(reader.IsContextInitialized);
            Assert.IsTrue(reader.IsModasFile);

            var it = reader.GetInjectionTimes();

            Assert.AreEqual(2, it.Count);

        }

        [TestMethod]
        public void InjectionTimesStringFormatTest()
        {
            var reader = new ModasDataReader(SourceFile);

            Assert.IsTrue(reader.IsContextInitialized);
            Assert.IsTrue(reader.IsModasFile);

            var it = reader.GetInjectionTimes();

            foreach (var i in it)
            {
                Console.WriteLine("{0:0.####}", i);
            }

        }
    }
}

/*
 * var query = new Query(typeof(Contribution));
query.Projection.Add("ContributorId");
query.Distinct = true;
var results = unitOfWork.Project<int>(query);
 * */
