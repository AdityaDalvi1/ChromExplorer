using System;
using System.IO;
using System.Security.Cryptography;
using ChromExplorer.DataModel.Interfaces;
using ChromExplorer.DataModel.PostProcessing;
using ChromExplorer.DataModel.Spectrum;
using ChromExplorer.Infrastructure;
using ChromExplorer.Infrastructure.Events;
using ChromExplorer.Infrastructure.Interfaces;
using ChromExplorer.Services;
using Microsoft.Practices.Prism.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace ChromExplorer.Modules.ModasChromProvider.Tests
{
    [TestClass]
    public class ModasChromProviderTests
    {
        private const string Filename = "caffeine 400ngul POS 201412009.mtd";

        [TestMethod]
        public void CreateProviderTest()
        {
            var eventAggregatorMock = new Mock<IEventAggregator>();
            var statusServiceMock = new Mock<IStatusUpdateService>();
            var postProcessingServiceMock = new Mock<IPostProcessingService>();
            var settingsProviderMock = new Mock<IUserSettingsProvider>();

            var importSettingsMock = new Mock<IModasImportUserSettings>();
            importSettingsMock.Setup(s => s.PPD).Returns(25);
            importSettingsMock.Setup(s => s.NoiseFilterLevel).Returns(3);
            importSettingsMock.Setup(s => s.RebuildTic).Returns(true);
            importSettingsMock.Setup(s => s.TimeUnits).Returns(TimeUnits.Sec);
            importSettingsMock.Setup(s => s.RemoveBaseLine).Returns(true);

            settingsProviderMock.Setup(sp => sp.ModasImportSettings).Returns(importSettingsMock.Object);

            eventAggregatorMock.Setup(s => s.GetEvent<ChromatogramChangedEvent>()).Returns(new ChromatogramChangedEvent());

            /*
             *           recalcTic = _settingsProvider.ModasImportSettings.RebuildTic;
                removeBaseline = _settingsProvider.ModasImportSettings.RemoveBaseLine;
                nfl = _settingsProvider.ModasImportSettings.NoiseFilterLevel;
                ppd = _settingsProvider.ModasImportSettings.PPD;
                units = _settingsProvider.ModasImportSettings.TimeUnits;
             * */

            var provider = new Services.ModasChromProvider(eventAggregatorMock.Object, statusServiceMock.Object, null, settingsProviderMock.Object);

            var fi = new FileInfo(Filename);

            Assert.IsTrue(fi.Exists);

            bool opres = provider.Load(fi.FullName, false);

            Assert.IsTrue(opres);

            if (provider.Chromatogram == null)
            {
                Console.WriteLine("WTF");
            }

            Assert.IsNotNull(provider.Chromatogram);

            Assert.AreEqual(42, provider.Chromatogram.ChromPoints.Count);

            Assert.IsNotNull(provider.Chromatogram.CurrentSpectrum);

            provider.Chromatogram.SelectedIndex = 36;

            Assert.IsNotNull(provider.Chromatogram.CurrentSpectrum);

            SaveSpectrum("CreateProviderTest.dat", provider.Chromatogram.CurrentSpectrum);

           

        }
        
        public void SaveSpectrum(string filename, MsSpectrum s)
        {
            using(TextWriter stream = new StreamWriter(filename))
            {
                var points = s.Points.Points;

                foreach (var point in points)
                {
                    stream.WriteLine($"{point.X} {point.Y}");
                }
            }
        }
    }
}
