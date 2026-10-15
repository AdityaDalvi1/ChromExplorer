using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChromExplorer.ModasReader.Tests
{
    [TestClass]
    public class ReaderGeneralTests
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
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        [TestMethod]
        public void MissedFileTest()
        {
            var reader = new ModasDataReader("abrvalg");

            Assert.IsFalse(reader.IsContextInitialized);
            Assert.IsFalse(reader.IsInitialized);
            Assert.IsFalse(reader.IsModasFile);
            Assert.IsFalse(reader.UpdateRequired);
        }

        [TestMethod]
        public void WrongFileTest()
        {
            const string filename = "wrong_file";
            using (var writer = new StreamWriter(filename))
            {
                writer.WriteLine("FileContent");
            }

            var reader = new ModasDataReader(filename);

            Assert.IsTrue(reader.IsContextInitialized);
            Assert.IsFalse(reader.IsModasFile);
            Assert.IsFalse(reader.UpdateRequired);
            Assert.IsFalse(reader.IsInitialized);
        }

        [TestMethod]
        public void ConvertFromOldFormat()
        {
            var reader = new ModasDataReader(WorkingFile);

            Assert.IsTrue(reader.IsContextInitialized);
            Assert.IsTrue(reader.IsModasFile);

            //Assert.IsTrue(reader.UpdateRequired);

            //reader.UpdateFromPreviousVersions();

            //Assert.IsFalse(reader.UpdateRequired);
        }

        [TestMethod]
        public void NonModasFileFormat()
        {
            var reader = new ModasDataReader(WorkingWrongFile);

            Assert.IsTrue(reader.IsContextInitialized);
            Assert.IsFalse(reader.IsModasFile);
            Assert.IsFalse(reader.UpdateRequired);

            //reader.UpdateFromPreviousVersions();

            //Assert.IsFalse(reader.UpdateRequired);
        }

    }
}
