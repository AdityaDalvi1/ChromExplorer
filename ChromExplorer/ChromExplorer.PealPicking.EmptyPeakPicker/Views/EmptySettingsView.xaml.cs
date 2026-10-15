using System.ComponentModel.Composition;
using System.Windows.Controls;

namespace ChromExplorer.PealPicking.EmptyPeakPicker.Views
{
    [Export("EmptySettingsView", typeof(EmptySettingsView))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class EmptySettingsView : UserControl
    {
        public EmptySettingsView()
        {
            InitializeComponent();
        }
    }
}
