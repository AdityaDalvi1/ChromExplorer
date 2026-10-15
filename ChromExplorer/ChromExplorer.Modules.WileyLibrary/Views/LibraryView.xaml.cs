using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ChromExplorer.Infrastructure;
using ChromExplorer.Modules.WileyLibrary.ViewModels;
using Microsoft.Practices.Prism.Regions;

namespace ChromExplorer.Modules.WileyLibrary.Views
{
    [Export("LibraryView", typeof(LibraryView))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class LibraryView : UserControl, IPartImportsSatisfiedNotification
    {
        public LibraryView()
        {
            InitializeComponent();
        }

        [Import]
        public IRegionManager RegionManager;

        [Import]
        [SuppressMessage("Microsoft.Design", "CA1044:PropertiesShouldNotBeWriteOnly", Justification = "Needs to be a property to be composed by MEF")]
        LibraryViewViewModel ViewModel
        {
            get { return (LibraryViewViewModel)this.DataContext; }
            set
            {
                this.DataContext = value;
            }
        }

        private static Uri _defaultUri = new Uri("ChromatorgramView", UriKind.Relative);
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.RegionManager.RequestNavigate(RegionNames.MainContentArea, _defaultUri);
        }

        void IPartImportsSatisfiedNotification.OnImportsSatisfied()
        {

        }
    }
}
