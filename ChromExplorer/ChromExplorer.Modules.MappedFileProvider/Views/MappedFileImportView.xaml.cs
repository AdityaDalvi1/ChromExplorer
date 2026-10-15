using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Controls;
using ChromExplorer.Modules.MappedFileProvider.ViewModels;

namespace ChromExplorer.Modules.MappedFileProvider.Views
{
    [Export("MappedFileImportView", typeof(MappedFileImportView))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class MappedFileImportView : UserControl
    {
        public MappedFileImportView()
        {
            InitializeComponent();
        }

        [Import]
        [SuppressMessage("Microsoft.Design", "CA1044:PropertiesShouldNotBeWriteOnly", Justification = "Needs to be a property to be composed by MEF")]
        MappedFileImportViewViewModel ViewModel
        {
            set
            {
                this.DataContext = value;
            }
        }
    }
}
