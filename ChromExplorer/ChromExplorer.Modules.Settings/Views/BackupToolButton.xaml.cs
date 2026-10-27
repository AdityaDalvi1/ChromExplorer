using ChromExplorer.Infrastructure;
using ChromExplorer.Infrastructure.Behaviors;
using Microsoft.Practices.Prism.Regions;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;

namespace ChromExplorer.Modules.Settings.Views
{
    // Shell toolbar icon that runs BackupService.CreateBackup() directly on click - no navigation,
    // no separate page. ViewSortHint of 1000 places it after every other ToolBarRegion icon
    // (the current rightmost one is AuthNavigationItemView's 999).
    [ViewExport(RegionName = RegionNames.ToolBarRegion)]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [ViewSortHint("1000")]
    public partial class BackupToolButton : UserControl
    {
        public BackupToolButton()
        {
            InitializeComponent();
        }

        private void BtnClick(object sender, RoutedEventArgs e)
        {
            if (!Application.Current.Properties.Contains("RoleId"))
            {
                MessageBox.Show("Session expired. Please login again.");
                return;
            }

            BackupService.CreateBackup();
        }
    }
}
