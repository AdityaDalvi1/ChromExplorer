using System.Collections.Generic;
using System.Globalization;
using System.IO;
using OxyPlot;

namespace ChromExplorer.OxyPlot.Demo
{
    public class GnuplotChromExporter
    {
        private const string ticDataKey = "$TIC";
        private readonly string _filename;
        private readonly HashSet<string> _dataNames = new HashSet<string>();
        
        private StreamWriter _writer;

        public IList<OxyColor> DefaultColors { get; set; }
        public double Threshold { get; set; }
       

        public GnuplotChromExporter(string filename)
        {
            _filename = filename;

            _writer = new StreamWriter(filename);

            this.DefaultColors = new List<OxyColor>
            {
                OxyColor.FromRgb(0x4E, 0x9A, 0x06),
                OxyColor.FromRgb(0xC8, 0x8D, 0x00),
                OxyColor.FromRgb(0xCC, 0x00, 0x00),
                OxyColor.FromRgb(0x20, 0x4A, 0x87),
                OxyColors.Red,
                OxyColors.Orange,
                OxyColors.Yellow,
                OxyColors.Green,
                OxyColors.Blue,
                OxyColors.Indigo,
                OxyColors.Violet
            };

            Threshold = 0;
            
        }

        public bool ExportChromatogramPlot(IOxyChromatogramModel model)
        {
            if (!_writer.BaseStream.CanWrite)
                return false;

            WriteDataSets(model);
            WriteSicData(model);
            WriteCommonSettings(model);
            WritePlotSection(model);

            _writer.Close();

           

            return true;
        }

        private void WritePlotSection(IOxyChromatogramModel model)
        {
            int colorIndex = 0;
            // this.DefaultColors[this.currentColorIndex++ % this.DefaultColors.Count];
            //string color = ToGnuPlotColor(this.DefaultColors[colorIndex++ % this.DefaultColors.Count]);
            _writer.WriteLine($"plot {ticDataKey} using 1:2 with lines title 'TIC' linecolor 'blue', \\");

            foreach (var descriptor in model.SICDescriptors)
            {
                string lc = ToGnuPlotColor(this.DefaultColors[colorIndex++ % this.DefaultColors.Count]);
                var minX = descriptor.LineSeries.DataRangeX.Minimum;
                string title = $"{descriptor.LineSeries.Title} [{descriptor.Annotation.MinimumX:0.#}, {descriptor.Annotation.MaximumX:0.#}]";
                _writer.WriteLine(
                    $"     ${descriptor.LineSeries.Title} using 1:(($2>={Threshold}) ? $2 : 0) with lines title '{title}' linecolor rgb '{lc}', \\");

            }
        }

        private string ToGnuPlotColor(OxyColor c)
        {
            return string.Format(
                CultureInfo.InvariantCulture, "#{0:x2}{1:x2}{2:x2}", c.R, c.G, c.B);
        }

        private void WriteCommonSettings(IOxyChromatogramModel model)
        {
            _writer.WriteLine($"set title '{model.Title}'");
            _writer.WriteLine("set xlabel 'Time (min)'");
            _writer.WriteLine("set ylabel 'Intensity (au)'");
            _writer.WriteLine("set xrange [*:*]");
            _writer.WriteLine("set yrange [*:*]");

        }

        private void WriteSicData(IOxyChromatogramModel model)
        {
            foreach (var descriptor in model.SICDescriptors)
            {
                _writer.WriteLine($"${descriptor.LineSeries.Title} << EOD");
                descriptor.LineSeries.WriteData(_writer, ", ");
                _writer.WriteLine("EOD");
                _writer.WriteLine();
            }
        }

        private void WriteDataSets(IOxyChromatogramModel model)
        {
            WriteTic(model);
        }

        private void WriteTic(IOxyChromatogramModel model)
        {
            _writer.WriteLine($"{ticDataKey} << EOD");
            
            foreach (var point in model.InstrumentChromatogram)
            {
                _writer.WriteLine($"{point.Time}, {point.TIC}");
            }

            _writer.WriteLine("EOD");
            _writer.WriteLine();
        }
    }
}