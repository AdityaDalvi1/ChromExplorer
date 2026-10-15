using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChromExplorer.ModasReader.Tests
{
    [TestClass]
    public class MethodFileLiteTests
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
        public void ReadMethodTest()
        {
            var reader = new ModasDataReader(WorkingFile);

            var methods = reader.GetLiteMethods();

            Assert.IsNotNull(methods);
            Assert.AreEqual(2, methods.Count);

            var method = methods[0];

            Assert.AreEqual(method.MFrom, 250, 1e-10);
            Assert.AreEqual(method.MTo, 1000, 1e-10);
            Assert.AreEqual(method.MainRFSampleRate, 23656.25, 1e-10);

            Assert.AreEqual(4, method.Actions.Count);

            var durations = new double[] {0.005, 0.005, 0.002, 0.28787318361955083};

            for(int i = 0; i < durations.Length; i++)
            {
                Assert.AreEqual(durations[i], method.Actions[i].Duration, 1e-10);
            }

            Assert.IsFalse(method.Actions[0].IsDetection);
            Assert.IsFalse(method.Actions[1].IsDetection);
            Assert.IsFalse(method.Actions[2].IsDetection);
            Assert.IsTrue(method.Actions[3].IsDetection);


            Assert.AreEqual(1, method.Actions[0].Triggers.Count);
            Assert.AreEqual(1, method.Actions[1].Triggers.Count);
            Assert.AreEqual(1, method.Actions[2].Triggers.Count);
            Assert.AreEqual(2, method.Actions[3].Triggers.Count);

            Assert.AreEqual(0,    method.Actions[0].Triggers[0].Delay);
            Assert.AreEqual((uint)1492, method.Actions[0].Triggers[0].StartLevel);
            Assert.AreEqual(15,   method.Actions[0].Triggers[0].StepSize);
            Assert.AreEqual((uint)108, method.Actions[0].Triggers[0].Steps);

            Assert.AreEqual(0, method.Actions[1].Triggers[0].Delay);
            Assert.AreEqual((uint)2571, method.Actions[1].Triggers[0].StartLevel);
            Assert.AreEqual(0, method.Actions[1].Triggers[0].StepSize);
            Assert.AreEqual((uint)1, method.Actions[1].Triggers[0].Steps);

            Assert.AreEqual(0, method.Actions[2].Triggers[0].Delay);
            Assert.AreEqual((uint)4763, method.Actions[2].Triggers[0].StartLevel);
            Assert.AreEqual(0, method.Actions[2].Triggers[0].StepSize);
            Assert.AreEqual((uint)1, method.Actions[2].Triggers[0].Steps);

            Assert.AreEqual(0, method.Actions[3].Triggers[0].Delay);
            Assert.AreEqual((uint)4395, method.Actions[3].Triggers[0].StartLevel);
            Assert.AreEqual(2, method.Actions[3].Triggers[0].StepSize);
            Assert.AreEqual((uint)6799, method.Actions[3].Triggers[0].Steps);

            Assert.AreEqual(0.28753500660501979, method.Actions[3].Triggers[1].Delay, 1e-10);
            Assert.AreEqual((uint)0, method.Actions[3].Triggers[1].StartLevel);
            Assert.AreEqual(0, method.Actions[3].Triggers[1].StepSize);
            Assert.AreEqual((uint)1, method.Actions[3].Triggers[1].Steps);
        }

        [TestMethod]
        public void MassListTest()
        {
            var reader = new ModasDataReader(WorkingFile);

            var methods = reader.GetLiteMethods();

            Assert.IsNotNull(methods);
            Assert.AreEqual(2, methods.Count);

            var method = methods[0];

            double digSampleRate = 97656.0;
            int samples = 28113;

            method.BuildMassList(samples, digSampleRate, null);

            Assert.AreEqual(28064, method.MassList.Count);
        }
    }
}
