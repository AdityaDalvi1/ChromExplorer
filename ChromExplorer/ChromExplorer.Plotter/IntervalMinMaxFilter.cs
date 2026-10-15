using System.Collections.Generic;
using System.Windows;
using ChromExplorer.DataModel.Core;

namespace ChromExplorer.Plotter
{
    public class IntervalMinMaxFilter 
    {
        public enum Mode
        {
            Min,
            Max,
            MinMax
        }
      
        private Mode mode;
        private double scale;

        public IntervalMinMaxFilter()
        {
            mode = Mode.Max;
            scale = 1;
        }

        public IntervalMinMaxFilter(Mode mode)
        {
            this.mode = mode;
            this.scale = 4;
            this.Intervals = 1024;
        }

        public IntervalMinMaxFilter(Mode mode, double scale)
        {
            this.mode = mode;
            this.scale = scale;
            this.Intervals = 1024;
        }

        public int Intervals { get; set; }


        private Rect screenRect;
        public void SetScreenRect(Rect screenRect)
        {
            this.screenRect = screenRect;
        }

        public List<Point> Filter(SimpleDataSource datasource)
        {
            if (datasource == null) return new List<Point>();

            int count = datasource.Length;
           
            List<Point> resultPoints = null;
            var start = StartIndex(this.screenRect, datasource);
            var end = EndIndex(this.screenRect, datasource);

            if (end - start > Intervals * 2)
            {
                //Filtering here
                double step = (datasource.Xvals[end] - datasource.Xvals[start]) / Intervals * scale;
                resultPoints = new List<Point>();

                double current = datasource.Xvals[start];
                int counter = 0;
                int current_point = start;

                while (current < datasource.Xvals[end])
                {
                    double max = datasource.Yvals[current_point];
                    double min = datasource.Yvals[current_point];
                    double start_value = datasource.Yvals[current_point];
                    double stop_value = datasource.Yvals[current_point];

                    while (datasource.Xvals[current_point] < current + step)
                    {
                        current_point++;

                        if (current_point < count)
                        {
                            if ( datasource.Yvals[current_point] > max)
                            {
                                max =  datasource.Yvals[current_point];
                            }

                            if ( datasource.Yvals[current_point] < min)
                            {
                                min =  datasource.Yvals[current_point];
                            }

                            stop_value =  datasource.Yvals[current_point];
                        }
                        else
                        {
                            break;
                        }
                    }

                    switch (mode)
                    {
                        case Mode.Min:
                            resultPoints.Add(new Point(current, min));
                            break;

                        case Mode.Max:
                            resultPoints.Add(new Point(current, max));
                            break;

                        case Mode.MinMax:
                            if (start_value > stop_value)
                            {
                                resultPoints.Add(new Point(current + step / 4.0, max));
                                resultPoints.Add(new Point(current + step / 4.0 * 3.0, min));
                            }
                            else
                            {
                                resultPoints.Add(new Point(current + step / 4.0, min));
                                resultPoints.Add(new Point(current + step / 4.0 * 3.0, max));
                            }
                            break;

                    }

                    counter++;
                    current += step;

                    if (current_point >= count)
                        break;
                }

                return resultPoints;
            }

            return datasource.Points;
        }

        private int StartIndex(Rect visible, SimpleDataSource datasource)
        {

            int retval = 0;

            int count = datasource.Length;

            for (int i = 0; i < count; i++)
            {

                if (datasource.Xvals[i] >= visible.Left)
                {
                    retval = i;
                    break;
                }
            }

            if (retval > 0)
                retval--;

            return retval;
        }

        private int EndIndex(Rect visible, SimpleDataSource datasource)
        {
            int retval = 0;

            int count = datasource.Length;

            if (count == 0)
                return retval;

            retval = count - 1;

            for (int i = count - 1; i >= 0; i--)
            {

                if (datasource.Xvals[i] <= visible.Right)
                {
                    retval = i;
                    break;
                }
            }

            if (retval < count - 1)
                retval++;

            return retval;
        }

       

    }
}
