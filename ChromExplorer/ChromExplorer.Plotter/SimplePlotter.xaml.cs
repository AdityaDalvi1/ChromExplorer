using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ChromExplorer.DataModel.Core;
using ChromExplorer.Plotter.Guestures;
using ChromExplorer.Plotter.Markers;

namespace ChromExplorer.Plotter
{
    /// <summary>
    /// Interaction logic for SimplePlotter.xaml
    /// </summary>
    public partial class SimplePlotter : UserControl
    {
        RectangleSelectionGesture rectZoom;
        public SimplePlotter()
        {
            InitializeComponent();
            //plot_canvas.Points = GetDummyPoints(100, 100);
           

            rectZoom = new RectangleSelectionGesture(plot_canvas, this, ModifierKeys.Control);
            rectZoom.ZoomSelection = true;
            rectZoom.ZoomSizeThreshold = 2;

            plot_canvas.XAxis = xaxis_control;
            plot_canvas.YAxis = yaxis_control;
            plot_canvas.MarkerCanvas = marker_canvas;
            plot_canvas.Container = this;

            IsSelectEnabled = true;
            SelectionLimit = 1000;

            if (autoScaleButton.IsChecked != null) 
                plot_canvas.AutoScaleY = (bool)autoScaleButton.IsChecked;
            else
                plot_canvas.AutoScaleY = false;


            this.Visible = new DataRect();
        }

        private bool _isSelectEnabled;
        public bool IsSelectEnabled
        {
            get { return _isSelectEnabled; }
            set
            {
                _isSelectEnabled = value;
                rectZoom.IsSelectEnabled = _isSelectEnabled;
            }
        }

        public int SelectionLimit { get; set; }

        public SimpleDataSource DataSource
        {
            get { return (SimpleDataSource)GetValue(DataSourceProperty); }
            set { SetValue(DataSourceProperty, value); }
        }

        public static readonly DependencyProperty DataSourceProperty =
            DependencyProperty.Register(
              "DataSource",
              typeof(SimpleDataSource),
              typeof(SimplePlotter),
              new FrameworkPropertyMetadata
              {
                  AffectsRender = true,
                  DefaultValue = null,
                  PropertyChangedCallback = OnDataSourceChangedCallback
              }
            );

        private static void OnDataSourceChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SimplePlotter graph = (SimplePlotter)d;
            graph.plot_canvas.DataSource = e.NewValue as SimpleDataSource;
        }


        public SimpleDataSource DataSource2
        {
            get { return (SimpleDataSource)GetValue(DataSource2Property); }
            set { SetValue(DataSource2Property, value); }
        }

        public static readonly DependencyProperty DataSource2Property =
            DependencyProperty.Register(
              "DataSource2",
              typeof(SimpleDataSource),
              typeof(SimplePlotter),
              new FrameworkPropertyMetadata
              {
                  AffectsRender = true,
                  DefaultValue = null,
                  PropertyChangedCallback = OnDataSource2ChangedCallback
              }
            );

        private static void OnDataSource2ChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SimplePlotter graph = (SimplePlotter)d;
            graph.plot_canvas.DataSource2 = e.NewValue as SimpleDataSource;
        }

        public MarkerCollection Markers
        {
            get { return (MarkerCollection)GetValue(MarkersProperty); }
            set { SetValue(MarkersProperty, value); }
        }

        public static readonly DependencyProperty MarkersProperty =
            DependencyProperty.Register(
              "Markers",
              typeof(MarkerCollection),
              typeof(SimplePlotter),
              new FrameworkPropertyMetadata
              {
                  AffectsRender = true,
                  DefaultValue = null,
                  PropertyChangedCallback = OnMarkersChangedCallback
              }
            );

        private static void OnMarkersChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SimplePlotter graph = (SimplePlotter)d;
            graph.plot_canvas.Markers = e.NewValue as MarkerCollection;
        }

        public static readonly DependencyProperty VisibleProperty =
            DependencyProperty.Register(
                "Visible",
                typeof(DataRect),
                typeof(SimplePlotter),
                new FrameworkPropertyMetadata
                {
                    DefaultValue = null,
                    //PropertyChangedCallback = OnVisibleChangedCallback
                }
                );

        private static void OnVisibleChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SimplePlotter graph = (SimplePlotter) d;
            var dataRect = e.NewValue as DataRect;

            if(dataRect != null)
                graph.plot_canvas.SetRange(dataRect.XMin, dataRect.XMax);
        }


        public DataRect Visible
        {
            get
            {
                return (DataRect)GetValue(VisibleProperty);
            }
            set
            {
                SetValue(VisibleProperty, value);
                
                OnZoomChanged();

            }
        }

        public int SelectedIndex
        {
            get { return (int)GetValue(SelectedIndexProperty); }
            set { SetValue(SelectedIndexProperty, value); }
        }
     
        public static readonly DependencyProperty SelectedIndexProperty =
          DependencyProperty.Register(
            "SelectedIndex",
            typeof(int),
            typeof(SimplePlotter),
            new FrameworkPropertyMetadata
            {
                AffectsRender = true,
                DefaultValue = -1,
                PropertyChangedCallback = OnSelectedIndexPropertyCallback
            }
        );

        private static void OnSelectedIndexPropertyCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var graph = (SimplePlotter)d;
 
            if (e.NewValue != null)
                graph.plot_canvas.SelectedIndex = (int)e.NewValue;
            else
                graph.plot_canvas.SelectedIndex = -1;
        }

        public IList<int> SelectedIndexes
        {
            get { return (IList<int>)GetValue(SelectedIndexesProperty); }
            set { SetValue(SelectedIndexesProperty, value); }
        }

        public static readonly DependencyProperty SelectedIndexesProperty =
          DependencyProperty.Register(
            "SelectedIndexes",
            typeof(IList<int>),
            typeof(SimplePlotter),
            new FrameworkPropertyMetadata
            {
                AffectsRender = true,
                DefaultValue = null,
                PropertyChangedCallback = OnSelectedIndexesPropertyCallback
            }
        );

        private static void OnSelectedIndexesPropertyCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var graph = (SimplePlotter)d;
     
            graph.plot_canvas.SelectedIndexes = e.NewValue as IList<int>;
        }

        public object XAxisTooltip
        {
            get { return GetValue(SelectedIndexesProperty); }
            set { SetValue(SelectedIndexesProperty, value); }
        } 

        public static readonly DependencyProperty XAxisTooltipProperty =
            DependencyProperty.Register(
            "XAxisTooltip",
            typeof(object),
            typeof(SimplePlotter),
            new FrameworkPropertyMetadata
            {
                AffectsRender = true,
                DefaultValue = string.Empty,
                PropertyChangedCallback = OnXAxisTooltipPropertyCallback
            }
        );

        private static void OnXAxisTooltipPropertyCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var graph = (SimplePlotter)d;
            graph.xaxis_control.ToolTip = e.NewValue;
        }

        #region MultiSelectEnabled

        public bool IsMultiSelectEnabled
        {
            get { return (bool)GetValue(IsMultiSelectEnabledProperty); }
            set { SetValue(IsMultiSelectEnabledProperty, value); }
        }

        public static readonly DependencyProperty IsMultiSelectEnabledProperty =
            DependencyProperty.Register(
              "IsMultiSelectEnabled",
              typeof(bool),
              typeof(SimplePlotter),
              new FrameworkPropertyMetadata
              {
                  AffectsRender = true,
                  DefaultValue = false,
                  PropertyChangedCallback = OnIsMultiSelectEnabledPropertyChangedCallback
              }
            );

        private static void OnIsMultiSelectEnabledPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var graph = (SimplePlotter)d;
            graph.rectZoom.IsMultiSelectEnabled = (bool) e.NewValue;
        }

        #endregion

        #region Integration Properties

        public double IntegrationStartTime
        {
            get { return (double)GetValue(IntegrationStartTimeProperty); }
            set { SetValue(IntegrationStartTimeProperty, value); }
        }

        public static readonly DependencyProperty IntegrationStartTimeProperty =
            DependencyProperty.Register("IntegrationStartTime", typeof(double), typeof(SimplePlotter),
                new FrameworkPropertyMetadata(double.NaN, (d, e) => ((SimplePlotter)d).plot_canvas.IntegrationStartTime = (double)e.NewValue));

        public double IntegrationEndTime
        {
            get { return (double)GetValue(IntegrationEndTimeProperty); }
            set { SetValue(IntegrationEndTimeProperty, value); }
        }

        public static readonly DependencyProperty IntegrationEndTimeProperty =
            DependencyProperty.Register("IntegrationEndTime", typeof(double), typeof(SimplePlotter),
                new FrameworkPropertyMetadata(double.NaN, (d, e) => ((SimplePlotter)d).plot_canvas.IntegrationEndTime = (double)e.NewValue));

        public bool IsIntegrationActive
        {
            get { return (bool)GetValue(IsIntegrationActiveProperty); }
            set { SetValue(IsIntegrationActiveProperty, value); }
        }

        public static readonly DependencyProperty IsIntegrationActiveProperty =
            DependencyProperty.Register("IsIntegrationActive", typeof(bool), typeof(SimplePlotter),
                new FrameworkPropertyMetadata(false, (d, e) => ((SimplePlotter)d).plot_canvas.IsIntegrationActive = (bool)e.NewValue));

        #endregion

        #region Noise Region Properties

        public double NoiseRegionStartTime
        {
            get { return (double)GetValue(NoiseRegionStartTimeProperty); }
            set { SetValue(NoiseRegionStartTimeProperty, value); }
        }

        public static readonly DependencyProperty NoiseRegionStartTimeProperty =
            DependencyProperty.Register("NoiseRegionStartTime", typeof(double), typeof(SimplePlotter),
                new FrameworkPropertyMetadata(double.NaN, (d, e) => ((SimplePlotter)d).plot_canvas.NoiseRegionStartTime = (double)e.NewValue));

        public double NoiseRegionEndTime
        {
            get { return (double)GetValue(NoiseRegionEndTimeProperty); }
            set { SetValue(NoiseRegionEndTimeProperty, value); }
        }

        public static readonly DependencyProperty NoiseRegionEndTimeProperty =
            DependencyProperty.Register("NoiseRegionEndTime", typeof(double), typeof(SimplePlotter),
                new FrameworkPropertyMetadata(double.NaN, (d, e) => ((SimplePlotter)d).plot_canvas.NoiseRegionEndTime = (double)e.NewValue));

        public bool IsNoiseRegionActive
        {
            get { return (bool)GetValue(IsNoiseRegionActiveProperty); }
            set { SetValue(IsNoiseRegionActiveProperty, value); }
        }

        public static readonly DependencyProperty IsNoiseRegionActiveProperty =
            DependencyProperty.Register("IsNoiseRegionActive", typeof(bool), typeof(SimplePlotter),
                new FrameworkPropertyMetadata(false, (d, e) => ((SimplePlotter)d).plot_canvas.IsNoiseRegionActive = (bool)e.NewValue));

        #endregion

        public ContextMenu PlotterMenu
        {
            get
            {
                return null;
            }
        }

        public void SetRange(double xmin, double xmax)
        {
            plot_canvas.SetRange(xmin, xmax);
        }

        public void Replot()
        {
            plot_canvas.Replot();
        }

        // Resets the X-axis window to the CURRENT DataSource's own full bounds and clears the
        // zoomed flag — same as the "Rescale" context-menu item below. Exposed publicly so a
        // host view can force this when it knows a genuinely different file was just loaded
        // (see ChromatogramPlotView's DataContextChanged), since XYPlotter.DataSource's own
        // setter only does this automatically on the very FIRST DataSource assignment —
        // afterwards keepVisualRect intentionally preserves whatever X range was already showing,
        // which is desirable when the same file's data is merely being rebuilt/reprocessed in
        // place, but wrong when the file itself has actually changed.
        public void Restore()
        {
            plot_canvas.Restore();
        }

        private void RescaleMenuItem_Click(object sender, RoutedEventArgs e)
        {
            plot_canvas.Restore();
        }

        private void CopyItem_Click(object sender, RoutedEventArgs e)
        {
            var oldBrush = top.Background;
            top.Background = Brushes.White;
            var height = PlotterSettings.Default.Height;
            var width = PlotterSettings.Default.Width;

            var oldTicks = plot_canvas.verticalTickSpacing;



            plot_canvas.verticalTickSpacing = height / 10;

            ChartUtilities.CopyFrameworkElementToClipboard(top, width, height);
            top.Background = oldBrush;
            plot_canvas.verticalTickSpacing = oldTicks;
        }

        private void autoScaleMenuItem_Checked(object sender, RoutedEventArgs e)
        {
            if (autoScaleButton.IsChecked != null) {
                plot_canvas.AutoScaleY = (bool)autoScaleButton.IsChecked;
            }

        }

      

        public event EventHandler<EventArgs> ZoomChanged;

        protected void OnZoomChanged()
        {
            if (ZoomChanged != null)
            {
                ZoomChanged(this, EventArgs.Empty);
            }
        }

    }
}
