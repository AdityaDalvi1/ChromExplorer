using System;
using System.ComponentModel.Composition;
using System.Data.SQLite;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using ChromExplorer.Infrastructure;
using ChromExplorer.Modules.Ms.ViewModels;
using Microsoft.Practices.Prism.Regions;

namespace ChromExplorer.Modules.Ms.Views
{
    /// <summary>
    /// Interaction logic for MsControlView.xaml
    /// </summary>
    [Export("MsControlView", typeof(MsControlView))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public partial class MsControlView : UserControl, IPartImportsSatisfiedNotification
    {
        public MsControlView()
        {
            InitializeComponent();
        }
        private readonly string _connectionString = @"Data Source=source.db3;Version=3;";
        [Import]
        public IRegionManager RegionManager;

        [Import]
        [SuppressMessage("Microsoft.Design", "CA1044:PropertiesShouldNotBeWriteOnly", Justification = "Needs to be a property to be composed by MEF")]
        MsControlViewViewModel ViewModel
        {
            get { return (MsControlViewViewModel)this.DataContext; }
            set
            {
                this.DataContext = value;
            }
        }

        private IRegionNavigationJournal _mainJournal = null;
        private static Uri _defaultUri = new Uri("HomeScreenView", UriKind.Relative);
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Uri toNav = _defaultUri;

                if (_mainJournal != null && _mainJournal.CurrentEntry != null)
                    toNav = _mainJournal.CurrentEntry.Uri;

                this.RegionManager.RequestNavigate(RegionNames.MainContentArea, toNav);

                Log("Syringe Connect", "Syringe Connected");
            }
            catch (Exception ex)
            {
                Log("Syringe Start", ex.Message);
            }
        }

        public void Log(string action, string description)
        {
            try
            {
                int userId = 0;

                if (Application.Current.Properties["UserId"] != null)
                    userId = Convert.ToInt32(Application.Current.Properties["UserId"]);

                using (var con = new SQLiteConnection(_connectionString))
                {
                    con.Open();

                    string sql = @"INSERT INTO AuditLogM
                          (action, description, is_active, inserted_by, inserted_date)
                          VALUES
                          (@action, @desc, 1, @uid, datetime('now','localtime'))";

                    using (var cmd = new SQLiteCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@action", action);
                        cmd.Parameters.AddWithValue("@desc", description);
                        cmd.Parameters.AddWithValue("@uid", userId);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Log Error: " + ex.Message);
            }
        }

        void IPartImportsSatisfiedNotification.OnImportsSatisfied()
        {
            var mainRegion = this.RegionManager.Regions[RegionNames.MainContentArea];

            if (mainRegion != null && mainRegion.NavigationService != null)
            {
                _mainJournal = mainRegion.NavigationService.Journal;
            }
        }
    }
}
