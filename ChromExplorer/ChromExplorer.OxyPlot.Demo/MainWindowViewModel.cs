using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;
using ChromExplorer.Infrastructure.Interfaces;
using log4net;
using log4net.Filter;
using Microsoft.Practices.Prism.Commands;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Wpf;

namespace ChromExplorer.OxyPlot.Demo
{
    public class MainWindowViewModel
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly SimpleChromProvider _provider = new SimpleChromProvider();


        public MainWindowViewModel()
        {
            //_provider.Load("TestData\\Axcend LCMS mix4 DDS flow 0_5 N2 11min 002 18.mtd");
            _provider.Load("TestData\\DD mix200 IT50_100 LC MS Apr 20 02.mtd");
          

            IChromDataProvider chromProvider = _provider as IChromDataProvider;


            ChromPlotModel = new ChromatogramPlotModel(_provider.OxyChromatogram);
            ChromPlotController = PlotControllerFactory.Create();
            // Create two line series (markers are hidden by default)

            SpectrumModel = new SpectrumPlotModel(_provider.OxyChromatogram);
            SpectrumController = PlotControllerFactory.Create(ControllerType.SpectrumPlot);

            Chromatogram = _provider.OxyChromatogram;

            StartTime = 6.5;
            StopTime = 6.7;
            ParentIon = 152;

            TestCommand = new DelegateCommand(OnTestCommand);
            CopyToClipboardCommand = new DelegateCommand(OnCopyToClipboard);
            FitCommand = new DelegateCommand(OnFit);
            RemoveXRangesCommand = new DelegateCommand(OnRemoveXRange);
            FilterChromCommand = new DelegateCommand<string>(OnFilterChrom);
            SaveAllChromCommand = new DelegateCommand(OnSaveAllChroms);
            SaveMsMsCommand = new DelegateCommand(OnSaveMsMs);
            LCPlotCommand = new DelegateCommand(OnLCPlot);

        }

        private void OnLCPlot()
        {
            var list = new List<Tuple<double, double>>
            {
                // new Tuple<double, double>(194.6, 195.4),
                // new Tuple<double, double>(180.2, 181.1),
                // new Tuple<double, double>(166.3, 166.8),
                // new Tuple<double, double>(152.3, 152.9),
                new Tuple<double, double>(530.5, 532),
                new Tuple<double, double>(573.5, 575),
                new Tuple<double, double>(648, 651.5),
                new Tuple<double, double>(904.8, 905.8),
                new Tuple<double, double>(1046, 1048),
            };

            foreach (var tuple in list)
            {
                var annotation = new SpectrumXRangeAnnotation()
                {
                    MinimumX = tuple.Item1,
                    MaximumX = tuple.Item2
                };

                //SpectrumModel.AddRange(annotation);
                //SpectrumModel.UpdateRange(annotation);
                SpectrumModel.CreateNewRange(annotation);
            }
            
        }

        private void OnSaveMsMs()
        {
            log.Info($"OnSaveMsMs(): startTime = {StartTime}, stopTime = {StopTime}");

            var filters = new FilterCollection();
            filters.Add(new TimeFilter(StartTime, StopTime));
            filters.Add(new MsPowerFilter(2));
            filters.Add(new ParentIonFilter(ParentIon));



            var points = Chromatogram.GetChromatogram(filters);

            log.Info($"OnSaveMsMs(): points size = {points.Count}");


            foreach (var point in points)
            {
                log.Info($"Parent Ion = {point.SpectrumMeta.ParentIon}, MSPower = {point.SpectrumMeta.MsPower}");
            }

            var spectrum = Chromatogram.GetAggregatedSpectrum(points);

            var exporter = new GnuplotSpectrumExporter($"spectrum_{ParentIon:0}_{StartTime:0.#}.plt");
            exporter.ExportChromatogramPlot(spectrum, $"MSMS Spectrum, {ParentIon}, {StartTime:0.#} - {StopTime:0.#} min");

            log.Info($"Spectrum size {spectrum.Points.Length}, parent Ion = {spectrum.ParentIon}");
        }

        private void OnSaveAllChroms()
        {
            log.Info("OnSaveAllChroms() called");
            //ChromPlotModel.DebugSaveAllChromatograms();
            Chromatogram.ExportChromPlots("chrom.plt");
        }

        private void OnFilterChrom(string levelStr)
        {
            if (!int.TryParse(levelStr, out var level))
                return;

            if (Chromatogram == null)
                return;

            var filter = new MsPowerFilter(level);
            Chromatogram.FilterChromatogram(filter);
        }

        private void OnRemoveXRange()
        {
            (ChromPlotModel as IXRangeAware).RemoveAll();
        }

        private void OnFit()
        {
            ChromPlotModel.AutoFitPlot();
            ChromPlotModel.InvalidatePlot(true);
        }

        private void OnCopyToClipboard()
        {
            var pngExporter = new PngExporter { Width = 600, Height = 400 };
            var bitmap = pngExporter.ExportToBitmap(ChromPlotModel);
            Clipboard.SetImage(bitmap);
        }

        private void OnTestCommand()
        {
            log.Debug("Test command has been called");
            ChromPlotModel.DebugPrint();
            ChromPlotModel.ShowTitle(false);
        }


        public DelegateCommand TestCommand { get; private set; }
        public DelegateCommand CopyToClipboardCommand { get; private set; }
        public DelegateCommand FitCommand { get; private set; }
        public DelegateCommand RemoveXRangesCommand { get; private set; }

        public DelegateCommand<string> FilterChromCommand { get; private set; }

        public DelegateCommand SaveAllChromCommand { get; private set; }

        public DelegateCommand SaveMsMsCommand { get; private set; }

        public DelegateCommand LCPlotCommand { get; private set; }

        //LCPlotCommand



        public IOxyChromatogramModel Chromatogram { get; private set; }
        public ChromatogramPlotModel ChromPlotModel { get; private set; }
        public PlotController ChromPlotController { get; private set; }

        public SpectrumPlotModel SpectrumModel { get; private set; }

        public PlotController SpectrumController { get; private set; }

        public double StartTime { get; set; }
        public double StopTime { get; set; }

        public double ParentIon { get; set; }


    }
}