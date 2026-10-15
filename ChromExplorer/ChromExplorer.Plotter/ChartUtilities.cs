using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ChromExplorer.Plotter
{
    public class ChartUtilities
    {
        public static void CopyFrameworkElementToClipboard(FrameworkElement copyTarget, double width, double height)
        {
            if (copyTarget == null)
                return;

            Clipboard.SetDataObject(CopyFrameworkElementToBitmap(copyTarget, width, height));
        }

        public static Bitmap CopyFrameworkElementToBitmap(FrameworkElement copyTarget, double width, double height)
        {
            if (copyTarget == null)
                return new Bitmap(Math.Max(1, (int)width), Math.Max(1, (int)height));

            Bitmap bitmap;
            // Convert from a WinFX Bitmap Source to a Win32 Bitamp
            using (Stream outStream = CopyFrameworkElementToStream(copyTarget, width, height, new BmpBitmapEncoder(), new MemoryStream(), 150.0))
            {
                bitmap = new Bitmap(outStream);
            }

            return bitmap;
        }

        public static void CopyFrameworkElemenToPNGFile(FrameworkElement copyTarget, double width, double height, string filename)
        {
            BitmapEncoder pngEncoder = new PngBitmapEncoder();
            using (FileStream fileSteam = new FileStream(filename, FileMode.Create))
            {
                CopyFrameworkElementToStream(copyTarget, width, height, pngEncoder, fileSteam, 150.0);
            }
        }

        public static Stream CopyFrameworkElementToStream(FrameworkElement copyTarget,
            double width, double height, BitmapEncoder enc, Stream outStream, double? dpi)
        {
            if (copyTarget == null)
                return outStream;

            // Store the Frameworks current layout transform, as this will be restored later
            Transform storedTransform = copyTarget.LayoutTransform;

            // Set the layout transform to unity to get the nominal width and height
           
            copyTarget.LayoutTransform = new ScaleTransform(1, 1);
            copyTarget.UpdateLayout();

            double baseHeight = copyTarget.ActualHeight;
            double baseWidth = copyTarget.ActualWidth;

            // Now scale the layout to fit the bitmap
            copyTarget.LayoutTransform =
                new ScaleTransform(baseWidth / width, baseHeight / height);
            copyTarget.UpdateLayout();

            // Render to a Bitmap Source, the DPI is changed for the 
            // render target as a way of scaling the FrameworkElement
            RenderTargetBitmap rtb = new RenderTargetBitmap(
                (int)width,
                (int)height,
                96d * width / baseWidth,
                96d * height / baseHeight,
                PixelFormats.Default);

            rtb.Render(copyTarget);

            // Convert from a WinFX Bitmap Source to a Win32 Bitamp

            if (dpi == null)
            {
                enc.Frames.Add(BitmapFrame.Create(rtb));
            }
            else
            {
                // Change the DPI

                ImageBrush brush = new ImageBrush(BitmapFrame.Create(rtb));

                RenderTargetBitmap rtbDpi = new RenderTargetBitmap(
                    (int)width, // PixelWidth
                    (int)height, // PixelHeight
                    (double)dpi, // DpiX
                    (double)dpi, // DpiY
                    PixelFormats.Default);

                DrawingVisual drawVisual = new DrawingVisual();
                using (DrawingContext dc = drawVisual.RenderOpen())
                {
                    dc.DrawRectangle(brush, null,
                        new Rect(0, 0, rtbDpi.Width, rtbDpi.Height));
                }

                rtbDpi.Render(drawVisual);

                enc.Frames.Add(BitmapFrame.Create(rtbDpi));
            }
            enc.Save(outStream);
            // Restore the Framework Element to it's previous state
            copyTarget.LayoutTransform = storedTransform;
            copyTarget.UpdateLayout();

            return outStream;
        }
    }
}
