using System;
using System.ComponentModel.Composition;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ChromExplorer.Assets;
using ChromExplorer.Infrastructure;
using ChromExplorer.Infrastructure.Interfaces;

namespace ChromExplorer.Modules.MzMLExporter.Services
{
    [Export(typeof(IChromDataExporter))]
    [ExactTypeMetadata(typeof(MzMLExportService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class MzMLExportService : IChromDataExporter
    {
        [ImportingConstructor]
        public MzMLExportService()
        {
            InitDescriptors();
        }

        #region Implementation of IResourceDescriptor

        public string ObjectKey { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Tooltip { get; set; }
        public ImageSource Icon { get; set; }

        #endregion

        #region Implementation of IChromDataExporter

        private readonly Uri _exportViewUri = new Uri("MzMLExportSettingsView", UriKind.Relative);
        public Uri ExportSettingsViewKey
        {
            get { return _exportViewUri; }
        }

        public void Export(IExportSettings settings)
        {
            
        }

        #endregion

        private void InitDescriptors()
        {
            ObjectKey = "MzMLExportService";
            Title = "MzML Chromatogram Exporter";
            Tooltip = "MzML Chromatogram Exporter";
            Icon = new BitmapImage(AssetExtension.GetResourceUri("LargeIcons/Devices/printer.png"));
        }

       
    }
}
