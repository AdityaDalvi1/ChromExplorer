using System.Windows;

namespace ChromExplorer.Plotter.Markers
{
    public enum MarkerPlacement
    {
        Top,
        Bottom,
        Left,
        Right,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    public enum MarkerOrientation
    {
        Vertical,
        Horizontal
    }

    public interface IMarker
    {
        Point Position { get; }
        string Text { get; }
        MarkerPlacement Placement { get; }
        MarkerOrientation Orientation { get; }
        double ScreenOffset { get; }
    }
}
