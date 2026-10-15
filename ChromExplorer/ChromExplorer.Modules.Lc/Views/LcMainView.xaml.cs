using ChromExplorer.Modules.Lc.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ChromExplorer.Modules.Lc.Views
{
    /// <summary>
    /// Interaction logic for LcMainView.xaml
    /// </summary>
    [Export("LcMainView", typeof(LcMainView))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public partial class LcMainView : UserControl
    {
        public LcMainView()
        {
            InitializeComponent();
        }

        [Import]
        [SuppressMessage("Microsoft.Design", "CA1044:PropertiesShouldNotBeWriteOnly", Justification = "Needs to be a property to be composed by MEF")]
        LcMainViewViewModel ViewModel
        {
            set
            {
                this.DataContext = value;
            }
        }
    }
}
