using System.Windows;

namespace ChromExplorer.Plotter.Markers
{
    public class SimpleMarker : IMarker
    {
        private Point position;
        private string text;
        private MarkerPlacement placement;
        private double offset;

        public SimpleMarker(Point pos, string text)
        {
            this.position = pos;
            this.text = text;

            placement = MarkerPlacement.Top;
            Orientation = MarkerOrientation.Vertical;
            this.offset = 0.0;
        }

        public Point Position
        {
            get { return position; }
        }

        public string Text
        {
            get { return text; }
        }

        public MarkerPlacement Placement
        {
            get { return placement; }
            set { placement = value; }
        }

        public MarkerOrientation Orientation { get; set; }

        public double ScreenOffset
        {
            get { return offset; }
            set { offset = value; }
        }
    }
}
