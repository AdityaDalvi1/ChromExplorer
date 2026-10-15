using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChromExplorer.WileySearch.Tests
{
    [TestClass]
    public class WrapperTests
    {

        static string WileyLibPath
        {
            get
            {
                string fullPath = System.Reflection.Assembly.GetAssembly(typeof(WrapperTests)).Location;
                string theDirectory = Path.GetDirectoryName(fullPath);

                return theDirectory != null ? Path.Combine(theDirectory, "x86", "MSLibProcessor.dll") : string.Empty;
            }
        }

        static string WileyPositivePath
        {
            get
            {
                string fullPath = System.Reflection.Assembly.GetAssembly(typeof(WrapperTests)).Location;
                string theDirectory = Path.GetDirectoryName(fullPath);

                return theDirectory != null ? Path.Combine(theDirectory, "positive_mode.sdf.bin") : string.Empty;
            }
        }

        [TestMethod]
        public void PringPath()
        {
            string fullPath = System.Reflection.Assembly.GetAssembly(typeof(WrapperTests)).Location;

            //get the folder that's in
            string theDirectory = Path.GetDirectoryName(fullPath);

            Console.WriteLine("{0}", theDirectory);

            Console.WriteLine("{0}", WileyLibPath);
        }

        [TestMethod]
        public void ConstructorTest()
        {
            var lib = new Wiley.MSforID(WileyLibPath);
            lib.InitializeMSLibrary(WileyPositivePath);
        }
    }
}
