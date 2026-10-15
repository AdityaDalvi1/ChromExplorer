using System;
using System.ComponentModel.Composition;
using System.Data.SQLite;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ChromExplorer.Infrastructure;
using ChromExplorer.Infrastructure.Behaviors;
using Microsoft.Practices.Prism.Regions;

namespace ChromExplorer.Modules.DataProvider.Views
{
    [ViewExport(RegionName = RegionNames.ToolBarRegion)]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [ViewSortHint("10")]
    public partial class DataProviderMainViewNavigationItem : UserControl, IPartImportsSatisfiedNotification
    {
        private static Uri _viewerUri = new Uri("DataProviderMainView", UriKind.Relative);

        private readonly string _connectionString = @"Data Source=source.db3;Version=3;";

        [Import]
        public IRegionManager RegionManager;

        public DataProviderMainViewNavigationItem()
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
            this.NavigateToDataPrividerView.IsChecked = (e.Uri == _viewerUri);
        }

        private void UpdateNavigationButtonState(Uri uri)
        {
            this.NavigateToDataPrividerView.IsChecked = (uri == _viewerUri);
            //this.NavigateToDataPrividerView.IsPressed = (uri == _viewerUri);

            if (uri != _viewerUri)
            {
                DactivateView();
            }
        }

        private void DactivateView()
        {
            var popupRegion = this.RegionManager.Regions[RegionNames.OverContentRegion];

            if(popupRegion != null && popupRegion.ActiveViews.Count() > 0)
            {
                var view = popupRegion.ActiveViews.FirstOrDefault();
                popupRegion.Deactivate(view);

                if (RegionManager.Regions.ContainsRegionWithName("ImportSetupRegion"))
                    RegionManager.Regions.Remove("ImportSetupRegion");

                if (RegionManager.Regions.ContainsRegionWithName("ExportSetupRegion"))
                    RegionManager.Regions.Remove("ExportSetupRegion");
            }

            if (RegionManager.Regions.ContainsRegionWithName("ImportSetupRegion"))
                RegionManager.Regions.Remove("ImportSetupRegion");

            if (RegionManager.Regions.ContainsRegionWithName("ExportSetupRegion"))
                RegionManager.Regions.Remove("ExportSetupRegion");

        }

        private void NavigateClick(object sender, RoutedEventArgs e)
        {
            if (!Application.Current.Properties.Contains("RoleId"))
            {
                MessageBox.Show("Session expired. Please login again.");
                return;
            }

            int roleId = Convert.ToInt32(Application.Current.Properties["RoleId"]);
            bool hasRights = false;

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                string sql = @"
SELECT COUNT(*)
FROM role_wise_rights_details r
INNER JOIN menuM m ON r.menu_id = m.menu_id
WHERE r.role_id = @role_id
AND m.menu_name = @page_name
AND r.is_active = 1
AND m.is_active = 1";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@role_id", roleId);
                    cmd.Parameters.AddWithValue("@page_name", "DataProviderFileLoad");

                    long count = (long)cmd.ExecuteScalar();
                    hasRights = count > 0;
                }
            }

            if (!hasRights)
            {
                MessageBox.Show("You are not authorized to File Load.",
                    "Access Denied",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }
                this.RegionManager.RequestNavigate(RegionNames.OverContentRegion, _viewerUri);
        }
    }
}
