using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ChromExplorer.Infrastructure;

namespace ChromExplorer.Modules.Viewer.Views
{
    /// <summary>
    /// Interaction logic for ChromatogramPlotView.xaml
    /// </summary>
    public partial class ChromatogramPlotView : UserControl
    {
        public ChromatogramPlotView()
        {
            InitializeComponent();

            plotExpander.Expanded += ExpandOrCollapse;
            plotExpander.Collapsed += ExpandOrCollapse;

            this.Loaded += new RoutedEventHandler(ChromatogramPlotViewLoaded);

            // Opening a NEW file swaps this view's DataContext to a brand-new
            // ChromatogramPlotViewModel instance (see ChromatogramViewViewModel.Chromatogram's
            // setter). XYPlotter's own zoom-preservation logic (keepVisualRect) only resets its
            // X-axis window on the control's very first DataSource assignment — every load after
            // that keeps whatever range was already showing, which is correct when the SAME
            // file's data is merely being rebuilt (e.g. a display-setting change), but wrong when
            // a genuinely different file was just opened: the new chromatogram's real peaks can
            // fall entirely outside the stale window, rendering as a flat/empty line. Deferred to
            // Background priority so it runs after the DataSource binding itself has already
            // updated for the new file.
            this.DataContextChanged += (s, e) =>
            {
                Dispatcher.BeginInvoke(new System.Action(() => chromatogramPlotter?.Restore()),
                    DispatcherPriority.Background);
            };
        }

        void ChromatogramPlotViewLoaded(object sender, RoutedEventArgs e)
        {
            Focus();
        }

        private void ExpandOrCollapse(object sender, RoutedEventArgs e)
        {
            ExpandOrCollapse(sender as Expander);
        }
        
        private void ExpandOrCollapse(Expander expander)
        {
      
           var rowIndex = Grid.GetRow(expander);
           var row = topGrid.RowDefinitions[rowIndex];
           if (expander.IsExpanded)
           {
               UpdateParent(true);
               row.Height = new GridLength(1, GridUnitType.Star);
               row.MinHeight = 50;
           }
           else
           {
               UpdateParent(false);
               row.Height = GridLength.Auto;
               row.MinHeight = 0;
           }
        }

        private void UpdateParent(bool isExpanded)
        {
            var parent = UIHelper.FindVisualParent<ChromatogramPlotView>(topGrid);

            var grid = UIHelper.FindVisualParent<Grid>(parent) as Grid;

            if(grid == null)
                return;

            var rowIndex = Grid.GetRow(parent);
            var row = grid.RowDefinitions[rowIndex];
            if(isExpanded)
            {
                row.Height = new GridLength(1, GridUnitType.Star);
                row.MinHeight = 50;
            }
            else
            {
                row.Height = GridLength.Auto;
                row.MinHeight = 0;
            }

            //grid.InvalidateVisual();
        }

    }
}
