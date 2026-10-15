using System.Collections.Generic;
using System.Windows;
using ChromExplorer.DataModel.Core;

namespace ChromExplorer.Plotter
{
    public abstract class ZoomConstrain
    {
        public abstract DataRect Apply(DataRect proposedDataRect);
        public abstract DataRect Apply(DataRect proposedDataRect, List<Point> points);
        public abstract DataRect Apply(DataRect proposedDataRect, List<Point> points, List<Point> points2);
    }

    public class LocalMaximumConstrain : ZoomConstrain
    {
        public LocalMaximumConstrain()
        {
        }



        public override DataRect Apply(DataRect proposedDataRect)
        {
            return proposedDataRect;
        }

        public override DataRect Apply(DataRect proposedDataRect, List<Point> points)
        {
            DataRect newRect = new DataRect();

            newRect.XMin = proposedDataRect.XMin;
            newRect.XMax = proposedDataRect.XMax;
            newRect.YMin = proposedDataRect.YMin;
            newRect.YMax = proposedDataRect.YMin;

            foreach (var p in points)
            {
                if (p.X >= newRect.XMin && p.X <= newRect.XMax)
                {
                    if (p.Y > newRect.YMax)
                    {
                        newRect.YMax = p.Y;
                    }
                }
            }

            return newRect;
        }

        public override DataRect Apply(DataRect proposedDataRect, List<Point> points, List<Point> points2)
        {
            DataRect newRect = new DataRect();

            newRect.XMin = proposedDataRect.XMin;
            newRect.XMax = proposedDataRect.XMax;
            newRect.YMin = proposedDataRect.YMin;
            newRect.YMax = proposedDataRect.YMin;

            foreach (var p in points)
            {
                if (p.X >= newRect.XMin && p.X <= newRect.XMax)
                {
                    if (p.Y > newRect.YMax)
                    {
                        newRect.YMax = p.Y;
                    }
                }
            }

            foreach (var p in points2)
            {
                if (p.X >= newRect.XMin && p.X <= newRect.XMax)
                {
                    if (p.Y > newRect.YMax)
                    {
                        newRect.YMax = p.Y;
                    }
                }
            }

            return newRect;
        }
    }

    public class FullDataHeightConstrain : ZoomConstrain
    {
        private double scaleFactor;

        public double ScaleFactor
        {
            get { return scaleFactor; }
            set { scaleFactor = value; }
        }

        public FullDataHeightConstrain()
        {
            scaleFactor = 1.0;
        }

        public FullDataHeightConstrain(double scaleFactor)
        {
            this.scaleFactor = scaleFactor;
        }

        public override DataRect Apply(DataRect proposedDataRect)
        {
            if(proposedDataRect.ToRect().IsEmpty)
                return new DataRect();

            DataRect newRect = new DataRect();

            newRect.XMin = proposedDataRect.XMin;
            newRect.XMax = proposedDataRect.XMax;
            newRect.YMin = proposedDataRect.YMin;
            newRect.YMax = newRect.YMin + proposedDataRect.Height * scaleFactor;

            return newRect;
        }

        public override DataRect Apply(DataRect proposedDataRect, List<Point> points)
        {
            return Apply(proposedDataRect);
        }
        
        public override DataRect Apply(DataRect proposedDataRect, List<Point> points, List<Point> points2)
        {
            return Apply(proposedDataRect);
        }
    }
}
