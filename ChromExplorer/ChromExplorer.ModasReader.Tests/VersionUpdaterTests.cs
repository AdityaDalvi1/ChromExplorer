using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mindscape.LightSpeed;

namespace ChromExplorer.ModasReader.Tests
{
    [TestClass]
    public class VersionUpdaterTests
    {
        private const string SourceFile = @"TestReader.db3";
        private const string WrongFile = @"WrongTables.db3";
        private const string WorkingFile = @"WorkingTestReader.db3";
        private const string WorkingWrongFile = @"WorkingWrongTables.db3";

        [TestInitialize]
        public void Initialize()
        {
            try
            {
                File.Copy(SourceFile, WorkingFile, true);
                File.Copy(WrongFile, WorkingWrongFile, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        [TestMethod]
        public void TableList()
        {
            var context = new LightSpeedContext<ModasDataModelUnitOfWork>
                              {
                                  ConnectionString = string.Format("Data Source=\"{0}\"", WorkingFile),
                                  DataProvider = DataProvider.SQLite3,
                                  IdentityMethod = IdentityMethod.KeyTable
                              };


            var updater = new VersionUpdater(context);

            var tables = updater.GetTables();

            Assert.IsNotNull(tables);

            foreach (var table in tables)
            {
                Console.Write("\"{0}\",", table);
            }
        }

        [TestMethod]
        public void ModasFileCheck()
        {
            var context = new LightSpeedContext<ModasDataModelUnitOfWork>
            {
                ConnectionString = string.Format("Data Source=\"{0}\"", WorkingFile),
                DataProvider = DataProvider.SQLite3,
                IdentityMethod = IdentityMethod.KeyTable
            };


            var updater = new VersionUpdater(context);

            Assert.IsTrue(updater.IsModasFile);
            Assert.IsTrue(updater.NeedUpdate);
        }

        [TestMethod]
        public void NoModasFileCheck()
        {
            var context = new LightSpeedContext<ModasDataModelUnitOfWork>
            {
                ConnectionString = string.Format("Data Source=\"{0}\"", WorkingWrongFile),
                DataProvider = DataProvider.SQLite3,
                IdentityMethod = IdentityMethod.KeyTable
            };


            var updater = new VersionUpdater(context);

            Assert.IsFalse(updater.IsModasFile);
            Assert.IsFalse(updater.NeedUpdate);
        }

    }
}
