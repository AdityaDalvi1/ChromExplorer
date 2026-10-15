using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ChromExplorer.Modules.Viewer.ViewModels;

namespace ChromExplorer.Modules.Viewer.Views
{
    /// <summary>
    /// Interaction logic for SpectrumPlotView.xaml
    /// </summary>
    public partial class SpectrumPlotView : UserControl
    {
        public SpectrumPlotView()
        {
            InitializeComponent();

            this.DataContextChanged += new DependencyPropertyChangedEventHandler(SpectrumPlotView_DataContextChanged);
        }

        void SpectrumPlotView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if(this.DataContext == null)
                return;

            var model = this.DataContext as SpectrumPlotViewModel;

            if (model != null)
                model.Plotter = this.spectrumPlotter;

            // A new SpectrumPlotViewModel instance here means a genuinely new spectrum was just
            // loaded — a new file, OR switching Data Channel (ChromatogramViewViewModel.
            // Chromatogram's setter always creates a fresh SpectrumPlotViewModel, same as it
            // does for ChromatogramPlotViewModel). Same XYPlotter.DataSource.keepVisualRect
            // staleness this session's chromatogram fix addressed — without this, the spectrum
            // plot keeps whatever zoom window (e.g. from "Select Mass") was left over from the
            // PREVIOUS channel/spectrum, and if the new one's real mass range doesn't fall
            // inside that stale window, it renders blank. Deferred to Background priority so it
            // runs after the DataSource binding itself has updated to the new spectrum's points.
            Dispatcher.BeginInvoke(new Action(() => spectrumPlotter?.Restore()), DispatcherPriority.Background);
        }
    }
}
