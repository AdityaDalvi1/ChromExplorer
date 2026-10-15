using System.Collections.Generic;
using System.Windows;

namespace ChromExplorer.DataModel.Core
{
    public class DataRect
    {
        public double XMin { get; set; }
        public double XMax { get; set; }

        public double YMin { get; set; }
        public double YMax { get; set; }

        public double Width { get { return this.XMax - this.XMin;  } }
        public double Height { get { return this.YMax - this.YMin; } }

        public DataRect()
        {
            this.XMin = double.PositiveInfinity;
            this.XMax = double.NegativeInfinity;

            this.YMin = double.PositiveInfinity;
            this.YMax = double.NegativeInfinity;
        }

        public Rect ToRect()
        {
            if (Width <= 0.0 || Height <= 0.0)
                return Rect.Empty; 

            return new Rect(XMin, YMin, Width, Height);
        }

        public override string ToString()
        {
            return string.Format("({0:0.0}, {1:0.0}) - ({2:0.0}, {3:0.0})", XMin, YMin, XMax, YMax);
        }

        public DataRect(SimpleDataSource datasource)
        {
            if (datasource.Length > 0)
            {
                this.XMin = datasource.Xvals[0];
                this.XMax = datasource.Xvals[0];

                this.YMin = datasource.Yvals[0];
                this.YMax = datasource.Yvals[0];
            }

            for (int i = 0; i < datasource.Length; i++)
            {
                if (datasource.Xvals[i] > this.XMax)
                {
                    this.XMax = datasource.Xvals[i];
                }
                else if (datasource.Xvals[i] < this.XMin)
                {
                    this.XMin = datasource.Xvals[i];
                }

                if (datasource.Yvals[i] > this.YMax)
                {
                    this.YMax = datasource.Yvals[i];
                }
                else if (datasource.Yvals[i] < this.YMin)
                {
                    this.YMin = datasource.Yvals[i];
                }
            }

            if (this.XMin == this.XMax)
            {
                if (this.XMin == 0.0)
                {
                    this.XMax = 1.0;
                    
                }
                else
                {
                    this.XMax = this.XMin * 2.0;
                }
            }

            if (this.YMin == this.YMax)
            {
                if (this.YMin == 0.0)
                {
                    this.YMax = 1.0;
                }
                else
                {
                    this.YMax = this.YMin * 2.0;
                   
                }
            }
        }

        public DataRect(IEnumerable<Point> points) : this()
        {
            if (points != null)
            {
                foreach (Point p in points)
                {
                    if (p.X > this.XMax)
                    {
                        this.XMax = p.X;
                    }
                    
                    if (p.X < this.XMin)
                    {
                        this.XMin = p.X;
                    }

                    if (p.Y > this.YMax)
                    {
                        this.YMax = p.Y;
                    }
                    
                    if (p.Y < this.YMin)
                    {
                        this.YMin = p.Y;
                    }
                }

            }
        }
    }
}
