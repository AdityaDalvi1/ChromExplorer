using System;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ChromExplorer.Modules.Viewer.ViewModels;
using log4net;
using Microsoft.Practices.Prism.Regions;

namespace ChromExplorer.Modules.Viewer.Views
{
    [Export("ChromatorgramView", typeof(ChromatorgramView))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class ChromatorgramView : UserControl, INavigationAware
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public ChromatorgramView()
        {
            log.Debug("ChromatorgramView() called");
            InitializeComponent();
           
        }

        [Import]
        [SuppressMessage("Microsoft.Design", "CA1044:PropertiesShouldNotBeWriteOnly", Justification = "Needs to be a property to be composed by MEF")]
        ChromatogramViewViewModel ViewModel
        {
            set
            {
                this.DataContext = value;
                if (value != null)
                {
                    value.GetChromatogramSnapshot = CaptureChromatogram;
                    value.GetSpectrumSnapshot = CaptureSpectrum;
                }
            }
        }

        private string CaptureChromatogram()
        {
            return CaptureElement(chromPlot);
        }

        private string CaptureSpectrum()
        {
            return CaptureElement(spectrumPlot);
        }

        private string CaptureElement(FrameworkElement element)
        {
            try
            {
                if (element == null) return "";

                // Use the element's actual size
                double width = element.ActualWidth;
                double height = element.ActualHeight;

                if (width <= 0 || height <= 0)
                {
                    // Fallback to render size or measure if necessary
                    width = element.RenderSize.Width;
                    height = element.RenderSize.Height;
                }

                if (width <= 0 || height <= 0) return "";

                // Update layout to ensure everything is rendered
                element.UpdateLayout();

                RenderTargetBitmap bmp = new RenderTargetBitmap((int)width, (int)height, 96, 96, PixelFormats.Pbgra32);
                
                // Using VisualBrush is more robust for capturing complex controls
                DrawingVisual visual = new DrawingVisual();
                using (DrawingContext dc = visual.RenderOpen())
                {
                    VisualBrush brush = new VisualBrush(element);
                    dc.DrawRectangle(brush, null, new Rect(new Point(0, 0), new Size(width, height)));
                }

                bmp.Render(visual);

                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bmp));

                using (MemoryStream ms = new MemoryStream())
                {
                    encoder.Save(ms);
                    byte[] bytes = ms.ToArray();
                    return "data:image/png;base64," + Convert.ToBase64String(bytes);
                }
            }
            catch (Exception ex)
            {
                log.Error("Failed to capture element snapshot", ex);
                return "";
            }
        }

        #region Implementation of INavigationAware

        void INavigationAware.OnNavigatedTo(NavigationContext navigationContext)
        {
           
        }

        bool INavigationAware.IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        void INavigationAware.OnNavigatedFrom(NavigationContext navigationContext)
        {
            chromPlot.Focus();
        }

        #endregion
    }
}
