using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Controls;
using ChromExplorer.Modules.MzMLExporter.ViewModels;

namespace ChromExplorer.Modules.MzMLExporter.Views
{
    [Export("MzMLExportSettingsView", typeof(MzMLExportSettingsView))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class MzMLExportSettingsView : UserControl
    {
        public MzMLExportSettingsView()
        {
            InitializeComponent();
        }

        [Import]
        [SuppressMessage("Microsoft.Design", "CA1044:PropertiesShouldNotBeWriteOnly", Justification = "Needs to be a property to be composed by MEF")]
        MzMLExportSettingsViewViewModel ViewModel
        {
            set
            {
                this.DataContext = value;
            }
        }
    }
    /*

    [Export("ModasImportView", typeof(ModasImportView))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class ModasImportView : UserControl
    {
        public ModasImportView()
        {
            InitializeComponent();
        }

        [Import]
        [SuppressMessage("Microsoft.Design", "CA1044:PropertiesShouldNotBeWriteOnly", Justification = "Needs to be a property to be composed by MEF")]
        ModasImportViewViewModel ViewModel
        {
            set
            {
                this.DataContext = value;
            }
        }
    }
     * */
}
