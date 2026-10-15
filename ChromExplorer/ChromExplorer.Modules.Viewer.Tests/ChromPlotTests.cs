using System;
using System.Collections.Generic;
using System.Windows.Threading;
using ChromExplorer.DataModel.Builders;
using ChromExplorer.DataModel.Chromatogram;
using ChromExplorer.DataModel.Core;
using ChromExplorer.DataModel.Spectrum;
using ChromExplorer.Modules.TestChromProvider.Model;
using ChromExplorer.Modules.Viewer.Tests.Forms;
using ChromExplorer.Modules.Viewer.ViewModels;
using Microsoft.Practices.Prism.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChromExplorer.Modules.Viewer.Tests
{
    public class TestDataContext
    {
        private readonly TestSpectrumCollection _spectrums = new TestSpectrumCollection();
        private GenericChromatogram _chromatogram;

        public ChromatogramPlotViewModel ChromVM { get; private set; }
        public SpectrumPlotViewModel SpectrumVM { get; private set; }
        public GenericChromatogram Chromatogram
        {
            get { return _chromatogram; }
        }

        public bool EnableMultiSelect { get; set; }

        public DelegateCommand<object> TestCommand { get; private set; }

        public TestDataContext()
        {

            BuildTestChrom(100, 100, "Test", "TestDescription");


            ChromVM = new ChromatogramPlotViewModel(_chromatogram, null, null, null);
            SpectrumVM = new SpectrumPlotViewModel(_chromatogram, null);

            TestCommand = new DelegateCommand<object>(OnTestCommand);
        }

        private void OnTestCommand(object obj)
        {
            Console.WriteLine("Test command called");
        }

        public void BuildTestChrom(int chromPoints, int spectrumPoints, string title, string description)
        {
            _spectrums.Clear();
            //_chromatogram.ChromPoints.Clear();

            var builder = new SineWaveformBuilder();
            var config = new WaveformConfigBase()
            {
                Amplitude = 1.0,
                Offset = 1.0,
                Phase = 0,
                Samples = spectrumPoints,
                Frequency = 10,
                SamplingRate = (chromPoints + 10) * 2.0,

            };

            var cps = new List<ChromatogramPoint>();
            for (int i = 0; i < chromPoints; i++)
            {
                config.Frequency = 10 + i;
                config.Amplitude = 1.0 + i;
                config.Offset = config.Amplitude;
                var ds = builder.CreateDataSource(config);

                //var spectrum = new MsSpectrum(ds);
                MsSpectrum spectrum = null;
                if (i == 0)
                {
                    spectrum = GetConstantSpectrum(spectrumPoints, 0);
                }
                else
                {
                    spectrum = GetConstantSpectrum(spectrumPoints, 1);
                }


                _spectrums.Add(spectrum);

                cps.Add(new ChromatogramPoint()
                {
                    Index = i,
                    SpectrumID = i,
                    Time = i,
                    TimeUnits = "au",
                    TIC = spectrum.TIC
                });
            }

            _spectrums.SpectrumSize = _spectrums[0].Points.Length;
            _spectrums.Positions = _spectrums[0].Points.Xvals;

            _chromatogram = new GenericChromatogram(_spectrums, new TestChromRebuilder(_spectrums), cps);
            _chromatogram.Title = title;
            _chromatogram.Description = description;

        }

        private MsSpectrum GetConstantSpectrum(int size, int value)
        {
            var x = new double[size];
            var y = new double[size];

            for (int i = 0; i < size; i++)
            {
                x[i] = (double)i / 10.0;
                y[i] = value;
            }

            y[0] = 0;

            return new MsSpectrum(new SimpleDataSource(x, y));
        }
    }

    [TestClass]
    public class ChromPlotTests
    {
        [TestMethod]
        public void ShowWindow()
        {
            return;
            try
            {
                var view = new ChromPlotView()
                {
                    DataContext = new TestDataContext()
                };

                view.ShowDialog();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        [TestCleanup]
        public void CleanUp()
        {
            Dispatcher.CurrentDispatcher.InvokeShutdown();

        }

    }
}
