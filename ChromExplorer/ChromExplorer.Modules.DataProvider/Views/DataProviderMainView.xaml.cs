using System;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using ChromExplorer.Infrastructure;
using ChromExplorer.Modules.DataProvider.ViewModels;
using log4net;
using Microsoft.Practices.Prism.Regions;

namespace ChromExplorer.Modules.DataProvider.Views
{
    [Export("DataProviderMainView", typeof(DataProviderMainView))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class DataProviderMainView : UserControl, IPartImportsSatisfiedNotification, INavigationAware 
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public DataProviderMainView()
        {
            log.Debug("DataProviderMainView() called");
            InitializeComponent();
        }

        [Import]
        public IRegionManager RegionManager;

        [Import]
        [SuppressMessage("Microsoft.Design", "CA1044:PropertiesShouldNotBeWriteOnly", Justification = "Needs to be a property to be composed by MEF")]
        DataProviderMainViewViewModel ViewModel
        {
            get { return (DataProviderMainViewViewModel)this.DataContext; }
            set
            {
                this.DataContext = value;
            }
        }


        #region Implementation of INavigationAware

        void INavigationAware.OnNavigatedTo(NavigationContext navigationContext)
        {
            if (this.ViewModel != null)
            {
                ViewModel.UpdateProviderControl();
                ViewModel.UpdateExporterControl();
            }
        }

        bool INavigationAware.IsNavigationTarget(NavigationContext navigationContext)
        {
            return false;
        }

        void INavigationAware.OnNavigatedFrom(NavigationContext navigationContext)
        {
            if (this.ViewModel != null)
            {
                ViewModel.DeactivateContorls();
            }
        }

        #endregion
    
        private static Uri _defaultUri = new Uri("ChromatorgramView", UriKind.Relative);
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //var popupRegion = this.RegionManager.Regions[RegionNames.OverContentRegion];

            //if (popupRegion != null && popupRegion.ActiveViews.Count() > 0)
            //{
                //var view = popupRegion.ActiveViews.FirstOrDefault();
            //    popupRegion.Deactivate(this);
            //}

            if (RegionManager.Regions.ContainsRegionWithName("ImportSetupRegion"))
                RegionManager.Regions.Remove("ImportSetupRegion");
            this.RegionManager.RequestNavigate(RegionNames.MainContentArea, _defaultUri);
        }

        void IPartImportsSatisfiedNotification.OnImportsSatisfied()
        {
           
        }
    }
}
