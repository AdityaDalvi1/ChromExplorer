using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Controls;
using ChromExplorer.Modules.TestChromProvider.ViewModels;

namespace ChromExplorer.Modules.TestChromProvider.Views
{
    [Export("TestImportView", typeof(TestImportView))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class TestImportView : UserControl
    {
        public TestImportView()
        {
            InitializeComponent();
        }

        [Import]
        [SuppressMessage("Microsoft.Design", "CA1044:PropertiesShouldNotBeWriteOnly", Justification = "Needs to be a property to be composed by MEF")]
        TestImportViewViewModel ViewModel
        {
            set
            {
                this.DataContext = value;
            }
        }
    }
}
