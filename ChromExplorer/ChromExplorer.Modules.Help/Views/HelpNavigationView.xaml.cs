using System;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using ChromExplorer.Infrastructure;
using ChromExplorer.Infrastructure.Behaviors;
using Microsoft.Practices.Prism.Regions;

namespace ChromExplorer.Modules.Help.Views
{
    [ViewExport(RegionName = RegionNames.ToolBarRegion)]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [ViewSortHint("99")]
    public partial class HelpNavigationView : UserControl, IPartImportsSatisfiedNotification
    {
        private static readonly Uri viewUri = new Uri("XpsHelpView", UriKind.Relative);

        [Import]
        public IRegionManager RegionManager;

        public HelpNavigationView()
        {
            InitializeComponent();
        }

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
            this.UpdateNavigationButtonState(e.Uri);
        }

        private void UpdateNavigationButtonState(Uri uri)
        {
            this.NavigateToHelpView.IsChecked = (uri == viewUri);
        }

        private void NavigateClick(object sender, RoutedEventArgs e)
        {
            this.RegionManager.RequestNavigate(RegionNames.MainContentArea, viewUri);
        }
    }
}
