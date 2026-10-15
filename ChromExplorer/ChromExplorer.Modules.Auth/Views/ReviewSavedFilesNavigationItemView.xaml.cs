using ChromExplorer.Infrastructure;
using ChromExplorer.Infrastructure.Behaviors;
using Microsoft.Practices.Prism.Regions;
using System;
using System.ComponentModel.Composition;
using System.Data.SQLite;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ChromExplorer.Modules.Auth.Views
{
    // Main toolbar entry point for the Reviewer feature — sits alongside Settings/Data
    // Processing in the same ToolBarRegion, instead of being reachable only through the User
    // toolbar's context menu. Same role-rights check those other main-nav buttons do inline
    // before navigating (role_wise_rights_details/menuM), matching the page's own menu_name.
    [ViewExport(RegionName = RegionNames.ToolBarRegion)]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [ViewSortHint("60")]
    public partial class ReviewSavedFilesNavigationItemView : UserControl, IPartImportsSatisfiedNotification
    {
        private static readonly Uri ViewerUri = new Uri(typeof(ReviewSavedFiles).FullName, UriKind.Relative);

        private readonly string _connectionString = @"Data Source=source.db3;Version=3;BusyTimeout=15000;";

        [Import]
        public IRegionManager RegionManager;

        public ReviewSavedFilesNavigationItemView()
        {
            InitializeComponent();
        }

        void IPartImportsSatisfiedNotification.OnImportsSatisfied()
        {
            IRegion mainContentRegion = RegionManager.Regions[RegionNames.MainContentArea];
            if (mainContentRegion != null && mainContentRegion.NavigationService != null)
            {
                mainContentRegion.NavigationService.Navigated += MainContentRegionNavigated;
            }

            IRegion overContentRegion = RegionManager.Regions[RegionNames.OverContentRegion];
            if (overContentRegion != null && overContentRegion.NavigationService != null)
            {
                overContentRegion.NavigationService.Navigated += OverContentRegionNavigated;
            }
        }

        public void MainContentRegionNavigated(object sender, RegionNavigationEventArgs e)
        {
            UpdateNavigationButtonState(e.Uri);
        }

        public void OverContentRegionNavigated(object sender, RegionNavigationEventArgs e)
        {
            NavigateToReviewSavedFiles.IsChecked = (e.Uri == ViewerUri);
        }

        private void UpdateNavigationButtonState(Uri uri)
        {
            NavigateToReviewSavedFiles.IsChecked = (uri == ViewerUri);

            if (uri != ViewerUri)
                DeactivateView();
        }

        private void DeactivateView()
        {
            var popupRegion = RegionManager.Regions[RegionNames.OverContentRegion];

            if (popupRegion != null && popupRegion.ActiveViews.Any())
            {
                var view = popupRegion.ActiveViews.FirstOrDefault();
                popupRegion.Deactivate(view);
                NavigateToReviewSavedFiles.IsChecked = false;
            }
        }

        private void NavigateClick(object sender, RoutedEventArgs e)
        {
            if (!Application.Current.Properties.Contains("RoleId"))
            {
                MessageBox.Show("Session expired. Please login again.");
                return;
            }

            int roleId = Convert.ToInt32(Application.Current.Properties["RoleId"]);
            bool hasRights;

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                const string sql = @"
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
                    cmd.Parameters.AddWithValue("@page_name", typeof(ReviewSavedFiles).FullName);

                    hasRights = Convert.ToInt64(cmd.ExecuteScalar()) > 0;
                }
            }

            if (!hasRights)
            {
                MessageBox.Show("You are not authorized to open Review Saved Files.",
                    "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            RegionManager.RequestNavigate(RegionNames.OverContentRegion, ViewerUri);
        }
    }
}
