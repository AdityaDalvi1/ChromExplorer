using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Controls;
using ChromExplorer.Modules.Quan.ViewModels;

namespace ChromExplorer.Modules.Quan.Views
{
    /// <summary>
    /// Interaction logic for RsdView.xaml
    /// </summary>
    [Export("RsdView", typeof(RsdView))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public partial class RsdView : UserControl
    {
        public RsdView()
        {
            InitializeComponent();
        }

        [Import]
        [SuppressMessage("Microsoft.Design", "CA1044:PropertiesShouldNotBeWriteOnly", Justification = "Needs to be a property to be composed by MEF")]
        RsdViewViewModel ViewModel
        {
            set
            {
                this.DataContext = value;
            }
        }
    }
}
