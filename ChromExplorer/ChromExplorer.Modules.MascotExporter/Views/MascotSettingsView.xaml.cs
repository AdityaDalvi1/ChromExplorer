using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Controls;
using ChromExplorer.Modules.MascotExporter.ViewModels;

namespace ChromExplorer.Modules.MascotExporter.Views
{
    [Export("MascotExportSettingsView", typeof(MascotSettingsView))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class MascotSettingsView : UserControl
    {
        public MascotSettingsView()
        {
            InitializeComponent();
        }

        [Import]
        [SuppressMessage("Microsoft.Design", "CA1044:PropertiesShouldNotBeWriteOnly", Justification = "Needs to be a property to be composed by MEF")]
        MascotSettingsViewViewModel ViewModel
        {
            set
            {
                this.DataContext = value;
            }
        }
    }
}
