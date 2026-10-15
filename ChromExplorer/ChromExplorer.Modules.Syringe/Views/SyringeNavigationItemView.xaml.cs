using ChromExplorer.Infrastructure;
using ChromExplorer.Infrastructure.Behaviors;
using Microsoft.Practices.Prism.Regions;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data.SQLite;
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

namespace ChromExplorer.Modules.Syringe.Views
{
    /// <summary>
    /// Interaction logic for SyringeNavigationItemView.xaml
    /// </summary>
    
    //[ViewExport(RegionName = RegionNames.LeftToolBarRegion)]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [ViewSortHint("30")]
    public partial class SyringeNavigationItemView : UserControl, IPartImportsSatisfiedNotification
    {
        private static Uri _viewerUri = new Uri("SyringeControlView", UriKind.Relative);
        public SyringeNavigationItemView()
        {
            InitializeComponent();
        }
        private readonly string _connectionString = @"Data Source=source.db3;Version=3;";
        //private readonly string _connectionString = @"// [MERGED-REMOVED] Data Source=D:\chromatograpy 23 feb 2026\chromexplorer-master 21-02 (1)\chromexplorer-master (2) (1)\chromexplorer-master\ChromExplorer\source.db3;Version=3;";

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
            this.NavigateToSyringeSettingsView.IsChecked = (e.Uri == _viewerUri);

        }

        private void UpdateNavigationButtonState(Uri uri)
        {
            this.NavigateToSyringeSettingsView.IsChecked = (uri == _viewerUri);

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
                this.NavigateToSyringeSettingsView.IsChecked = false;
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
            bool hasRights = false;

            // 2️⃣ check permission directly from DB
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
                    cmd.Parameters.AddWithValue("@page_name", "SyringeNavigationItemView");

                    long count = (long)cmd.ExecuteScalar();
                    hasRights = count > 0;
                }
            }

            // 3️⃣ if no rights
            if (!hasRights)
            {
                MessageBox.Show("You are not authorized to view syringe navigation.",
                    "Access Denied",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }
            else
            {
                this.RegionManager.RequestNavigate(RegionNames.OverContentRegion, _viewerUri);
            }
        }
    }
}
