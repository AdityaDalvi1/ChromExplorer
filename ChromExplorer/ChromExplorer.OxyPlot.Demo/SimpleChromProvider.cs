using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Media;
using ChromExplorer.DataModel.Chromatogram;
using ChromExplorer.Infrastructure;
using ChromExplorer.Infrastructure.Interfaces;
using ChromExplorer.ModasReader;
using log4net;
using Mindscape.LightSpeed.Querying;

namespace ChromExplorer.OxyPlot.Demo
{
    public class SimpleChromProvider : IChromDataProvider
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private ModasDataReader _data;

        public SimpleChromProvider()
        {
            ImportSettingsViewKey = new Uri("SimpleChromProvider", UriKind.Relative);  
            //Filename = filename;
        }

        public string ObjectKey { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Tooltip { get; set; }
        public ImageSource Icon { get; set; }
        public Uri ImportSettingsViewKey { get; }
        
        public bool Load(string filename, bool async = true)
        {
            log.InfoFormat("ModasChromProvider: Load({0}) called", filename);

            Filename = filename;
            var data = new ModasDataReader(filename);

            if (!data.IsModasFile)
            {
                return false;
            }

            if (data.UpdateRequired)
            {
                data.UpdateFromPreviousVersions();
            }

            data.LoadChromPoints(TimeUnits.Min, null);
            data.UpdateStatistic();

            bool recalcTic = true;
            bool removeBaseline = true;
            double nfl = ModasDataReader.DefaultNoiseFilterLevel;
            double ppd = 10;
            TimeUnits units = TimeUnits.Min;


            var points = BindToFile(data, units, recalcTic, data.MassFrom, data.MassTo, ppd, null, removeBaseline, nfl, async);

            var fi = new FileInfo(_data.FileName);

            this.Chromatogram = new GenericChromatogram(_data, _data, points, null, null)
            {
                Title = fi.Name,
                Description = fi.FullName,
                Filename = fi.FullName,
                PPD = ppd
            };

            this.OxyChromatogram = new ChromatogramDataModel(_data, _data, points)
            {
                Title = fi.Name,
                Description = fi.FullName,
                Filename = fi.FullName,
                PPD = ppd
            };



            return true;
        }

        public GenericChromatogram Chromatogram { get; private set; }
        public IOxyChromatogramModel OxyChromatogram { get; private set; }
        public string Filename { get; set; }

        private IList<ChromatogramPoint> BindToFile(ModasDataReader reader, TimeUnits units, bool recalcTic, double mzMin, double mzMax, double ppd, QueryExpression query, bool removeBaseLine, double nfl, bool async = true)
        {
            if (_data != null)
            {
                _data.Dispose();
            }

            _data = reader;

            _data.SpectrumFilter = query;
            var chrom = _data.ReadChromatogram(units);

            _data.LoadData(mzMin, mzMax, ppd, recalcTic, null, removeBaseLine, nfl);

            if (recalcTic)
                _data.RebuildTic();

            return chrom;
        }
    }
}