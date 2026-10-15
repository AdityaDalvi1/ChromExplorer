using System;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using ChromExplorer.Infrastructure;
using ChromExplorer.Modules.Settings.ViewModels;
using Microsoft.Practices.Prism.Regions;

namespace ChromExplorer.Modules.Settings.Views
{
    [Export("SettingsView", typeof(SettingsView))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class SettingsView : UserControl, IPartImportsSatisfiedNotification
    {
        public SettingsView()
        {
            InitializeComponent();
        }

       
        [Import]
        public IRegionManager RegionManager;

        [Import]
        [SuppressMessage("Microsoft.Design", "CA1044:PropertiesShouldNotBeWriteOnly", Justification = "Needs to be a property to be composed by MEF")]
        SettingsViewViewModel ViewModel
        {
            get { return (SettingsViewViewModel)this.DataContext; }
            set
            {
                this.DataContext = value;
            }
        }

        private IRegionNavigationJournal _mainJournal = null;
        private static Uri _defaultUri = new Uri("HomeScreenView", UriKind.Relative);
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Uri toNav = _defaultUri;

            if (_mainJournal != null && _mainJournal.CurrentEntry != null)
                toNav = _mainJournal.CurrentEntry.Uri;

            this.RegionManager.RequestNavigate(RegionNames.MainContentArea, toNav);
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
