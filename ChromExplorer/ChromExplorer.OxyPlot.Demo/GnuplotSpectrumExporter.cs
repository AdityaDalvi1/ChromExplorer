using System.IO;
using ChromExplorer.DataModel.Spectrum;

namespace ChromExplorer.OxyPlot.Demo
{
    public class GnuplotSpectrumExporter
    {
        private readonly string _filename;
        private StreamWriter _writer;

        public GnuplotSpectrumExporter(string filename)
        {
            _filename = filename;
            _writer = new StreamWriter(_filename);
        }

        public bool ExportChromatogramPlot(ISpectrum spectrum, string title)
        {
            if (!_writer.BaseStream.CanWrite)
                return false;

            _writer.WriteLine("$SpectrumData << EOD");

            foreach (var point in spectrum.Points.Points)
            {
                _writer.WriteLine($"{point.X} {point.Y}");
            }

            _writer.WriteLine("EOD");
            _writer.WriteLine();

            _writer.WriteLine($"set key off");
            _writer.WriteLine($"set title '{title}'");
            _writer.WriteLine("set xlabel 'm/z (Da)'");
            _writer.WriteLine("set ylabel 'Intensity (au)'");
            _writer.WriteLine("set xrange [*:*]");
            _writer.WriteLine("set yrange [*:*]");
            _writer.WriteLine("plot $SpectrumData using 1:2 with lines linecolor 'blue'");


           _writer.Close();



            return true;
        }
    }
}