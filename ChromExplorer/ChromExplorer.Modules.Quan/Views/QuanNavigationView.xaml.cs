using ChromExplorer.Infrastructure;
using ChromExplorer.Infrastructure.Behaviors;
using Microsoft.Practices.Prism.Regions;
using System;
using System.ComponentModel.Composition;
using System.Data.SQLite;
using System.Windows;
using System.Windows.Controls;

namespace ChromExplorer.Modules.Quan.Views
{
    [ViewExport(RegionName = RegionNames.ToolBarRegion)]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [ViewSortHint("30")]
    public partial class QuanNavigationView : UserControl, IPartImportsSatisfiedNotification
    {
        private static readonly Uri viewUri = new Uri("QuanView", UriKind.Relative);

        private readonly string _connectionString = @"Data Source=source.db3;Version=3;";

        [Import]
        public IRegionManager RegionManager;

        public QuanNavigationView()
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
                    cmd.Parameters.AddWithValue("@page_name", "QuanData");

                    long count = (long)cmd.ExecuteScalar();
                    hasRights = count > 0;
                }
            }

            if (!hasRights)
            {
                MessageBox.Show("You are not authorized to open Quan Data.",
                    "Access Denied",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            this.RegionManager.RequestNavigate(RegionNames.MainContentArea, viewUri);
        }
    }
}
