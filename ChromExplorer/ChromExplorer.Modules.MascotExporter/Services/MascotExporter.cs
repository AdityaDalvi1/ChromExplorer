using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ChromExplorer.Assets;
using ChromExplorer.DataModel.Chromatogram;
using ChromExplorer.DataModel.Core;
using ChromExplorer.DataModel.Interfaces;
using ChromExplorer.DataModel.Spectrum;
using ChromExplorer.Infrastructure;
using ChromExplorer.Infrastructure.Events;
using ChromExplorer.Infrastructure.Interfaces;
using log4net;
using Microsoft.Practices.Prism.Events;

namespace ChromExplorer.Modules.MascotExporter.Services
{
    [Export(typeof(IChromDataExporter))]
    [ExactTypeMetadata(typeof(MascotExporter))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class MascotExporter : IChromDataExporter
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IEventAggregator _eventAggregator;
        private readonly IStatusUpdateService _statusService;
        private readonly ChromDataProviderFactory _factory;
        private readonly BackgroundWorker _exportWorker = null;

        [ImportingConstructor]
        public MascotExporter(IEventAggregator eventAggregator, IStatusUpdateService statusService, ChromDataProviderFactory factory)
        {
            _eventAggregator = eventAggregator;
            _statusService = statusService;
            _factory = factory;

            InitDescriptors();
            ExportSettingsViewKey = new Uri("MascotExportSettingsView", UriKind.Relative);

            _exportWorker = new BackgroundWorker();
            _exportWorker.DoWork += new DoWorkEventHandler(ExportAsyncHandler);
            _exportWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(ExportCompletedHandler);
        }

        public bool IsBusy {
            get { return _exportWorker.IsBusy; }
        }

        private void ExportCompletedHandler(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_statusService != null)
            {
                _statusService.SetReadyStatus();
                _statusService.SetWaitCursor(false);
            }

            if (_eventAggregator != null)
            {
                _eventAggregator.GetEvent<ChromExportCompeted>().Publish(null);
            }
        }

        private void ExportAsyncHandler(object sender, DoWorkEventArgs e)
        {
            try
            {
                var settings = e.Argument as MascotExportSettings;

                if(settings == null)
                    return;

                var filename = settings.Filename;

                if (string.IsNullOrEmpty(filename))
                    return;

                if(_factory.Current == null)
                    return;


                log.InfoFormat("Exporting Mascot file {0}", filename);

                var chrom = _factory.Current.Chromatogram;

                double progressStep = 0;

                if (_statusService != null)
                {
                    progressStep = (_statusService.ProgressMax - _statusService.ProgressMin) / chrom.ChromPoints.Count;

                    if (settings.SelectedSpectraOnly)
                    {
                        int count = chrom.SelectedIndexes == null ? 1 : chrom.SelectedIndexes.Count;
                        progressStep = (_statusService.ProgressMax - _statusService.ProgressMin)/count;
                    }
                }

                using (var outfile = new StreamWriter(filename))
                {
                    outfile.WriteLine("# Export from file \"{0}\"", _factory.Current.Filename);
                    outfile.WriteLine("# Exported by MascotExporter {0}", typeof(MascotExporter).Assembly.GetName().Version);
                    outfile.WriteLine("# PPD = {0}", chrom.PPD);
                    outfile.WriteLine("# Mass Type = {0}", settings.MassType);
                    if (settings.SelectedSpectraOnly)
                    {
                        var list = chrom.SelectedIndexes ?? new List<int>() { chrom.SelectedIndex };
                  
                        for (int i = 0; i < list.Count; i++)
                        {
                            ExportChromPoint(outfile, chrom, list[i], settings);

                            if (_statusService != null)
                            {
                                _statusService.ReportProgress(i * progressStep);
                            }
                        }
                    }
                    else
                    {
                        int size = chrom.ChromPoints.Count;
                        for (int i = 0; i < size; i++)
                        {
                            ExportChromPoint(outfile, chrom, i, settings);

                            if (_statusService != null)
                            {
                                _statusService.ReportProgress(i * progressStep);
                            }
                        }
                    }

                }

               

            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
            finally
            {
                
            }
        }

        
        private void ExportChromPoint(StreamWriter outfile, GenericChromatogram chrom, int index, MascotExportSettings settings)
        {
            var point = chrom.ChromPoints[index];
            var spectrum = chrom.Spectrums[index];

            SimpleDataSource ds = PreprocessSpectrum(spectrum, settings);

            outfile.WriteLine("BEGIN IONS");
            outfile.WriteLine("TITLE=Spectrum {0}", index);
            outfile.WriteLine("PEPMASS={0}", spectrum.ParentIon);
            outfile.WriteLine("RTINSECONDS={0}", point.SpectrumMeta.EllapsedMilliseconds / 1000.0);

            switch (settings.MassType)
            {
                    case MassTypes.Exact:
                    WriteExact(outfile, ds, settings);
                    break;

                    case MassTypes.Nominal:
                    WriteNominal(outfile, ds, settings);
                    break;
            }

            //for (int i = 0; i < spectrum.Points.Length; i++)
            //{
             //   outfile.WriteLine("{0} {1:0.0}", spectrum.Points.Xvals[i], spectrum.Points.Yvals[i]);
            //}

            //outfile.WriteLine("m/z inten");
            //chrom.SelectedIndex = 

           

            outfile.WriteLine("END IONS");
        }

        private SimpleDataSource PreprocessSpectrum(MsSpectrum spectrum, MascotExportSettings settings)
        {
            if (!settings.Normalize && !settings.ShiftToZero)
            {
                return spectrum.Points;
            }

            double max = spectrum.Max;
            double min = spectrum.Min;

            double offset = 0;
            double scale = 1.0;

            if (settings.ShiftToZero)
            {
                offset = min;
            }


            if (settings.Normalize)
            {
                scale = max/settings.NormLevel;

                if(settings.ShiftToZero)
                    scale = (max - min) / settings.NormLevel;
            }

            var x = new double[spectrum.Points.Length];
            var y = new double[spectrum.Points.Length];

            for (int i = 0; i < x.Length; i++)
            {
                x[i] = spectrum.Points.Xvals[i];
                y[i] = (spectrum.Points.Yvals[i] - offset) / scale;
            }

            return new SimpleDataSource(x, y);
            
        }

        private void WriteNominal(StreamWriter outfile, SimpleDataSource points, MascotExportSettings settings)
        {
            int firstMass = -1;
            double firstInt = 0.0;
            for (int i = 0; i < points.Length; i++)
            {
                if (points.Yvals[i] > 0)
                {
                    int mass = (int)Math.Round(points.Xvals[i]);

                    if (firstMass == -1)
                    {
                        firstMass = mass;
                        firstInt = points.Yvals[i];
                    }
                    else
                    {
                        if (mass != firstMass)
                        {
                            outfile.WriteLine("{0}\t{1}", firstMass, firstInt.ToString("F2"));
                            firstMass = mass;
                            firstInt = points.Yvals[i];
                        }
                        else
                        {
                            firstInt += points.Yvals[i];
                        }

                    }
                }

                if (i == points.Length - 1 && firstMass > 0)
                {
                    outfile.WriteLine("{0}\t{1}", firstMass, firstInt.ToString("F2"));
                }

            }
        }

        private void WriteExact(StreamWriter outfile, SimpleDataSource points, MascotExportSettings settings)
        {
            for (int i = 0; i < points.Length; i++)
            {
                if (settings.WriteZero || points.Yvals[i] > 0)
                    outfile.WriteLine("{0} {1:0.0}", points.Xvals[i], points.Yvals[i]);
            }
        }


        /*
           if (spectrum == null)
                return null;

            double max = spectrum.Max;
            double min = spectrum.Min;

            double offset = 0;
            double scale = max;
            
            if(_configuration.ShiftToZero)
            {
                offset = min;
                scale = (max - min)/_configuration.NormalizeTo;
            }
   
            var x = new double[spectrum.Points.Length];
            var y = new double[spectrum.Points.Length];

            for (int i = 0; i < x.Length; i++)
            {
                x[i] = spectrum.Points.Xvals[i];
                y[i] = (spectrum.Points.Yvals[i] - offset)/ scale;
            }

            spectrum.Points = new SimpleDataSource(x, y);
            return spectrum;

         * */

        #region Implementation of IResourceDescriptor

        public string ObjectKey { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Tooltip { get; set; }
        public ImageSource Icon { get; set; }

        #endregion

        #region Implementation of IChromDataExporter

        public Uri ExportSettingsViewKey { get; private set; }
        public void Export(IExportSettings settings)
        {
            if (_exportWorker.IsBusy)
            {
                log.Warn("export wroker is busy");
                return;
            }

            log.InfoFormat("Begin export to file {0}", settings.Filename);

            if (_statusService != null)
            {
                _statusService.AppStatus = string.Format("Exporting...");
                _statusService.SetWaitCursor(true);
            }

            _exportWorker.RunWorkerAsync(settings);
        }

        #endregion


        private void InitDescriptors()
        {
            ObjectKey = "MascotExportService";
            Title = "Mascot Chromatogram Exporter";
            Tooltip = "Mascot Chromatogram Exporter";
            Icon = new BitmapImage(AssetExtension.GetResourceUri("LargeIcons/Devices/network.png"));
        }
    }
}
