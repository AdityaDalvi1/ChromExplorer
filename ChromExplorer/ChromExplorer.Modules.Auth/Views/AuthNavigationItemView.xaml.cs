using ChromExplorer.Infrastructure;
using ChromExplorer.Infrastructure.Behaviors;
using Microsoft.Practices.Prism.Regions;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ChromExplorer.Modules.Auth.Views
{
    /// <summary>
    /// Interaction logic for AuthNavigationItemView.xaml
    /// </summary>
    
    [ViewExport(RegionName = RegionNames.ToolBarRegion)]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [ViewSortHint("999")]
    public partial class AuthNavigationItemView : UserControl, IPartImportsSatisfiedNotification
    {
        private static Uri _viewerUri = new Uri("AuthView", UriKind.Relative);
    
        public AuthNavigationItemView()
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
            this.NavigateToAuthView.IsChecked = (e.Uri == _viewerUri);

        }

        private void UpdateNavigationButtonState(Uri uri)
        {
            this.NavigateToAuthView.IsChecked = (uri == _viewerUri);

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
                this.NavigateToAuthView.IsChecked = false;
            }
        }

        private void NavigateClick(object sender, RoutedEventArgs e)
        {
            this.RegionManager.RequestNavigate(RegionNames.OverContentRegion, _viewerUri);
        }
    }
}
