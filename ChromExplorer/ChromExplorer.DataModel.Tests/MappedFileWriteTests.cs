using System.IO;
using ChromExplorer.DataModel.DAL;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChromExplorer.DataModel.Tests
{
    [TestClass]
    public class MappedFileWriteTests
    {
        private int _spectrums = 1000;
        private int _points = 5000;
        private double _ppd = 25;
        private double _mFrom = 200;

        [TestMethod]
        public void WriteShort()
        {
            Write("short_model.mmf");
        }


        private void Write(string filename)
        {
            int id = 1;
            using (var stream = new FileStream(filename, FileMode.Create))
            {
                using (var writer = new BinaryWriter(stream))
                {
                    var header = new FileMappedSpectrumData.Header();
                    header.Spectrums = _spectrums;
                    header.Points = _points;
                    header.PPD = _ppd;
                    header.MFrom = _mFrom;
                    header.Write(writer);
       
                    for (int i = 0; i < _spectrums; i++)
                    {
                        var meta = new FileMappedSpectrumData.SpectrumMetaData() { EllapsedMilliseconds = i, TIC = 1000 + i };
                        meta.Write(writer);
                        for (int j = 0; j < _points; j++)
                        {
                            int value = id++;
                            writer.Write(value);
                            //Console.WriteLine("Write: S = {0}, P = {1}, Data = {2}", i, j, value);
                        }
                    }

                    writer.Close();
                }
            }
        }
    }
}
