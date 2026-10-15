using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ChromExplorer.Infrastructure;
using ChromExplorer.Infrastructure.Behaviors;
using Microsoft.Practices.Prism.Regions;

namespace ChromExplorer.Modules.Ms.Views
{
    /// <summary>
    /// Interaction logic for MsNavigationItemView.xaml
    /// </summary>
    
    //[ViewExport(RegionName = RegionNames.RightToolBarRegion)]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [ViewSortHint("30")]
    public partial class MsNavigationItemView : UserControl, IPartImportsSatisfiedNotification
    {
        private static Uri _viewerUri = new Uri("MsControlView", UriKind.Relative);
        private static Uri _mainViewerUri = new Uri("MsMainView", UriKind.Relative);
        public MsNavigationItemView()
        {
            InitializeComponent();
        }

        [Import]
        public IRegionManager RegionManager;

        void IPartImportsSatisfiedNotification.OnImportsSatisfied()
        {
            IRegion mainContentRegion = this.RegionManager.Regions[RegionNames.MainContentArea];
            if (mainContentRegion != null && mainContentRegion.NavigationService != null)
            {
                mainContentRegion.NavigationService.Navigated += this.MainContentRegionNavigated;
            }

            IRegion overContentRegion = this.RegionManager.Regions[RegionNames.OverContentRegion];
            if (overContentRegion != null && overContentRegion.NavigationService != null)
            {
                overContentRegion.NavigationService.Navigated += this.OverContentRegionNavigated;
            }
        }

        public void MainContentRegionNavigated(object sender, RegionNavigationEventArgs e)
        {
            this.UpdateNavigationButtonState(e.Uri);
        }

        public void OverContentRegionNavigated(object sender, RegionNavigationEventArgs e)
        {
            this.NavigateToMsSettingsView.IsChecked = (e.Uri == _viewerUri);
        }

        private void UpdateNavigationButtonState(Uri uri)
        {
            this.NavigateToMsSettingsView.IsChecked = (uri == _viewerUri);

            if (uri != _viewerUri)
            {
                DactivateView();
            }
        }

        private void DactivateView()
        {
            var popupRegion = this.RegionManager.Regions[RegionNames.OverContentRegion];

            if (popupRegion != null && popupRegion.ActiveViews.Count() > 0)
            {
                var view = popupRegion.ActiveViews.FirstOrDefault();
                popupRegion.Deactivate(view);
            }
        }

        private void NavigateClick(object sender, RoutedEventArgs e)
        {
            this.RegionManager.RequestNavigate(RegionNames.MainContentArea, _mainViewerUri);
            this.RegionManager.RequestNavigate(RegionNames.OverContentRegion, _viewerUri);
        }
    }
}
