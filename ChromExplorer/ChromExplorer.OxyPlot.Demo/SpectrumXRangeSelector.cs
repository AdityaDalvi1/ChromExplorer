using System;
using System.Reflection;
using log4net;
using OxyPlot;

namespace ChromExplorer.OxyPlot.Demo
{
    public class SpectrumXRangeSelector : MouseManipulator
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private SpectrumXRangeAnnotation range;
        private double startx = double.NaN;

        public SpectrumXRangeSelector(IPlotView plotView)
            : base(plotView)
        {
        }


        public override void Completed(OxyMouseEventArgs e)
        {
            base.Completed(e);
            startx = double.NaN;
            (this.PlotView.ActualModel as ISpectrumXRangeAware)?.UpdateRange(range);
            this.PlotView.InvalidatePlot();


            e.Handled = true;
        }


        public override void Delta(OxyMouseEventArgs e)
        {
            base.Delta(e);


            if (!double.IsNaN(startx))
            {
                var x = range.InverseTransform(e.Position).X;
                range.MinimumX = Math.Min(x, startx);
                range.MaximumX = Math.Max(x, startx);
                this.PlotView.InvalidatePlot();
                e.Handled = true;
            }

        }

        public override void Started(OxyMouseEventArgs e)
        {
            base.Started(e);
            startx = this.InverseTransform(this.StartPosition.X, this.StartPosition.Y).X;
            range = new SpectrumXRangeAnnotation()
            {
                Fill = OxyColor.FromAColor(120, OxyColors.SkyBlue)
                ,
                MinimumX = startx,
                MaximumX = startx

            };
            
            (this.PlotView.ActualModel as ISpectrumXRangeAware)?.AddRange(range);
            e.Handled = true;

        }


    }
}