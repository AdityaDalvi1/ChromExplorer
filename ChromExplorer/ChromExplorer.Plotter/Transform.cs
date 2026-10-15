using System.Windows;
using ChromExplorer.DataModel.Core;

namespace ChromExplorer.Plotter
{
    public class CoordinateTransform
    {
        public CoordinateTransform(DataRect visibleRect, Rect screenRect)
        {
            axToScreen = screenRect.Width / visibleRect.Width;
            ayToScreen = screenRect.Height / visibleRect.Height;
            bxToScreen = visibleRect.XMin * axToScreen - screenRect.Left;
            byToScreen = screenRect.Height + screenRect.Top + visibleRect.YMin * ayToScreen;

            axToData = visibleRect.Width / screenRect.Width;
            ayToData = visibleRect.Height / screenRect.Height;
            bxToData = screenRect.Left * axToData - visibleRect.XMin;
            byToData = visibleRect.Height + visibleRect.YMin + screenRect.Top * ayToData;
        }
        
        // x(screen) = axToScreen * x + bxToScreen
        // y(screen) = ayToScreen * y + byToScreen
        double axToScreen;
        double bxToScreen;
        double ayToScreen;
        double byToScreen;

        double axToData;
        double bxToData;
        double ayToData;
        double byToData;

        public Point DataToScreen(Point dataPoint)
        {
            Point screenPoint = new Point(dataPoint.X * axToScreen - bxToScreen,
                byToScreen - dataPoint.Y * ayToScreen);

            return screenPoint;
        }

        public Point ScreenToData(Point screenPoint)
        {
            Point dataPoint = new Point(screenPoint.X * axToData - bxToData,
               byToData - screenPoint.Y * ayToData);

            return dataPoint;
        }
    }
}
