using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Windows.Media;
using ChromExplorer.Infrastructure;
using ChromExplorer.Infrastructure.Interfaces;
using ChromExplorer.Modules.DataProvider.Properties;
using log4net;
using Microsoft.Practices.Prism.Regions;

namespace ChromExplorer.Modules.DataProvider.ViewModels
{
    [Export(typeof (DataProviderMainViewViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class DataProviderMainViewViewModel : IHeaderInfoProvider, IPartImportsSatisfiedNotification, INotifyPropertyChanged
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        [Import] public IRegionManager RegionManager;
        private const string _headerInfo = "File";
        private ObservableCollection<IChromDataProvider> _providers;
        private ObservableCollection<IChromDataExporter> _exporters;
        private IChromDataProvider _currentProvider;
        private IChromDataExporter _currentExporter;
        private readonly ChromDataProviderFactory _factory;

        [ImportingConstructor]
        public DataProviderMainViewViewModel(ChromDataProviderFactory factory)
        {
            _factory = factory;

            HeaderIcon = null;
            PropertyChanged += OnViewModelPropertyChanged;
        }

        [ImportMany]
        public ObservableCollection<IChromDataProvider> Providers
        {
            get { return _providers; }
            set
            {
                if (Equals(value, _providers)) return;
                _providers = value;
                OnPropertyChanged("Providers");
            }
        }

        [ImportMany]
        public ObservableCollection<IChromDataExporter> Exporters
        {
            get { return _exporters; }
            set
            {
                if (Equals(value, _exporters)) return;
                _exporters = value;
                OnPropertyChanged("Exporters");
            }
        }

        //public IChromDataProvider CurrentProvider
        //{
        //    get { return _currentProvider; }
        //    set
        //    {
        //        if (Equals(value, _currentProvider)) return;
        //        _currentProvider = value;
        //        //_factory.Current = _currentProvider;
        //        OnPropertyChanged("CurrentProvider");
        //    }
        //}

        public IChromDataProvider CurrentProvider
        {
            get { return _factory.Current; }
            set { _factory.Current = value; }
        }

        public IChromDataExporter CurrentExporter
        {
            get { return _currentExporter; }
            set
            {
                if (Equals(value, _currentExporter)) return;
                _currentExporter = value;
                OnPropertyChanged("CurrentExporter");
            }
        }

        #region Implementation of IPartImportsSatisfiedNotification

        void IPartImportsSatisfiedNotification.OnImportsSatisfied()
        {
            if (Providers == null || !Providers.Any()) return;



            _factory.Current =  Providers.FirstOrDefault(
                provider => provider.ObjectKey == ChromDataProviderKeys.ModasDataProvider)
                              ?? Providers.First();

            //CurrentProvider = Providers.FirstOrDefault(
            //    provider => provider.ObjectKey == ChromDataProviderKeys.ModasDataProvider)
            //                  ?? Providers.First();

            if (Exporters != null && Exporters.Any())
            {
                CurrentExporter = Exporters.FirstOrDefault(
                    provider => provider.ObjectKey == ChromDataExporterKeys.MascotExportService)
                                  ?? Exporters.First();
            }
        }

        #endregion

        public void UpdateProviderControl()
        {
            if (CurrentProvider == null)
                return;

            try
            {
                RegionManager.RequestNavigate("ImportSetupRegion", CurrentProvider.ImportSettingsViewKey);
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        public void UpdateExporterControl()
        {
            if (CurrentExporter == null)
                return;

            try
            {
                var contains = RegionManager.Regions.ContainsRegionWithName("ImportSetupRegion");
                RegionManager.RequestNavigate("ExportSetupRegion", CurrentExporter.ExportSettingsViewKey);
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "CurrentProvider")
            {
                UpdateProviderControl();
            }
            else if (e.PropertyName == "CurrentExporter")
            {
                UpdateExporterControl();
            }
        }

        public void DeactivateContorls()
        {
            if (RegionManager.Regions.ContainsRegionWithName("ImportSetupRegion"))
                RegionManager.Regions.Remove("ImportSetupRegion");

            if (RegionManager.Regions.ContainsRegionWithName("ExportSetupRegion"))
                RegionManager.Regions.Remove("ExportSetupRegion");
        }

        #region Implementation of IHeaderInfoProvider

        public string HeaderInfo
        {
            get { return _headerInfo; }
        }

        public ImageSource HeaderIcon { get; private set; }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}