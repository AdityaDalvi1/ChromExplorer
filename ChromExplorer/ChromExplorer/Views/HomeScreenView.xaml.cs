using ChromExplorer.Infrastructure;
using ChromExplorer.Infrastructure.Behaviors;
using ChromExplorer.Modules.Syringe.ViewModels;
using ChromExplorer.ViewModels;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Controls;

namespace ChromExplorer.Views
{
    [ViewExport(RegionName = RegionNames.MainContentArea)]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class HomeScreenView : UserControl
    {
        public HomeScreenView()
        {
            InitializeComponent();
        }

        [Import]
        [SuppressMessage("Microsoft.Design", "CA1044:PropertiesShouldNotBeWriteOnly", Justification = "Needs to be a property to be composed by MEF")]
        HomeScreenViewViewModel ViewModel
        {
            get { return (HomeScreenViewViewModel)this.DataContext; }
            set
            {
                this.DataContext = value;
            }
        }
    }
}
