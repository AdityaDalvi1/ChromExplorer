using System.ComponentModel.Composition;
using System.Windows.Controls;

namespace ChromExplorer.PeakPicking.ModasPeakPicker.Views
{
    [Export("ModasPPConfigView", typeof(ModasPPConfigView))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class ModasPPConfigView : UserControl
    {
        public ModasPPConfigView()
        {
            InitializeComponent();
        }
    }
}
