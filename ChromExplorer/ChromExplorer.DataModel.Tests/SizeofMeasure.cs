using System;
using System.IO;
using ChromExplorer.DataModel.DAL;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChromExplorer.DataModel.Tests
{
    [TestClass]
    public class SizeofMeasure
    {
        [TestMethod]
        public void SpectrumMetaDataTest()
        {
            var data = new FileMappedSpectrumData.SpectrumMetaData();

            using (var stream = new MemoryStream())
            {
                using(var writer = new BinaryWriter(stream))
                {
                   data.Write(writer);
                }

                var bytes = stream.ToArray();
                Console.WriteLine(bytes.Length);
            }
        }
    }
}
