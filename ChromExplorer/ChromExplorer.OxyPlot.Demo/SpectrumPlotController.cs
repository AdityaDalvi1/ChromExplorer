using System.Reflection;
using log4net;
using OxyPlot;
using OxyPlot.Axes;

namespace ChromExplorer.OxyPlot.Demo
{
    public class SpectrumPlotController : PlotController
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static IViewCommand<OxyMouseDownEventArgs> ZoomBand { get; private set; }
        public static IViewCommand<OxyMouseDownEventArgs> SelectBand { get; private set; }
        public static IViewCommand<OxyMouseDownEventArgs> RemoveBand { get; private set; }
        public static IViewCommand<OxyMouseDownEventArgs> UnzoomCommand { get; private set; }
        public static IViewCommand<OxyMouseDownEventArgs> MoveSpectrumMarker { get; private set; }



        public SpectrumPlotController() : base()
        {
            ZoomBand = new DelegatePlotCommand<OxyMouseDownEventArgs>((view, controller, args) =>
                controller.AddMouseManipulator(view, new ZoomBandManipulator(view), args));
            SelectBand = new DelegatePlotCommand<OxyMouseDownEventArgs>((view, controller, args) =>
                controller.AddMouseManipulator(view, new SpectrumXRangeSelector(view), args));
            UnzoomCommand =
                new DelegatePlotCommand<OxyMouseDownEventArgs>((view, controller, args) => HandleUnzoom(view, args));
            MoveSpectrumMarker =
                new DelegatePlotCommand<OxyMouseDownEventArgs>((view, controller, args) =>
                    HandleMoveSpectrumNumber(view, args));
            RemoveBand = new DelegatePlotCommand<OxyMouseDownEventArgs>((view, controller, args) =>
            {
                HandleRemoveBand(view, args);
            });

        }




        public void Setup()
        {
            UnbindAll();
            Bind(new OxyMouseWheelGesture(),
                new DelegatePlotCommand<OxyMouseWheelEventArgs>((view, _, args) => HandleZoomByWheel(view, args)));

            Bind(new OxyMouseWheelGesture(OxyModifierKeys.Control)
                , new DelegatePlotCommand<OxyMouseWheelEventArgs>((view, _, args) => HandleVerticalZoomByWheel(view, args)));

            Bind(new OxyMouseDownGesture(OxyMouseButton.Middle), PlotCommands.PanAt);

            Bind(new OxyMouseDownGesture(OxyMouseButton.Left), SpectrumPlotController.ZoomBand);
            Bind(new OxyMouseDownGesture(OxyMouseButton.Left, OxyModifierKeys.Shift),
                SpectrumPlotController.SelectBand);
            Bind(new OxyMouseDownGesture(OxyMouseButton.Right, OxyModifierKeys.Shift),
                SpectrumPlotController.RemoveBand);
            //Bind(new OxyMouseDownGesture(OxyMouseButton.Left, OxyModifierKeys.None, 2),
            //    ChromatogramPlotController.MoveSpectrumMarker);
            Bind(new OxyMouseDownGesture(OxyMouseButton.Right), SpectrumPlotController.UnzoomCommand);
            

            //new OxyMouseDownGesture(OxyMouseButton.Left,)

            //Bind(new OxyKeyGesture(OxyKey.Left), PlotCommands.Reset);
            Bind(new OxyKeyGesture(OxyKey.Home),
                new DelegatePlotCommand<OxyKeyEventArgs>((view, _, args) => HandleReset(view, args)));
            Bind(new OxyKeyGesture(OxyKey.Left),
                new DelegatePlotCommand<OxyKeyEventArgs>((view, _, args) => HandleArrowLeft(view, args)));
            Bind(new OxyKeyGesture(OxyKey.Right),
                new DelegatePlotCommand<OxyKeyEventArgs>((view, _, args) => HandleArrowRight(view, args)));

            //Bind(new OxyKeyGesture(OxyKey.Left),
            //    PlotCommands.PanLeft);
            //BindKeyDown(OxyKey.Home, );
            //this.BindKeyDown(OxyKey.Left, );
            //this.BindKeyDown(OxyKey.Right, PlotCommands.PanRight);
        }

        private void HandleReset(IPlotView view, OxyKeyEventArgs args)
        {
            log.DebugFormat("HandleReset: args =  {0}", args);

            var model = view.ActualModel as ChromatogramPlotModel;

            if (model != null)
            {
                model.AutoFitPlot();
                model.InvalidatePlot(true);
            }


        }



        private void HandleZoomByWheel(IPlotView view, OxyMouseWheelEventArgs args, double factor = 1)
        {
            var m = new ZoomStepManipulator(view)
            {
                AxisPreference = AxisPreference.X,
                Step = args.Delta * 0.001 * factor,
                FineControl = args.IsControlDown,
            };
            m.Started(args);




        }

        private void HandleVerticalZoomByWheel(IPlotView view, OxyMouseWheelEventArgs args, double factor = 1)
        {
            var m = new ZoomStepManipulator(view)
            {
                AxisPreference = AxisPreference.Y,
                Step = args.Delta * 0.001 * factor,
                FineControl = args.IsControlDown,
            };
            m.Started(args);
        }

        private static void HandleUnzoom(IPlotView view, OxyInputEventArgs args)
        {
            args.Handled = true;
            view.ActualModel.ResetAllAxes();
            view.InvalidatePlot(false);
        }

        private void HandleMoveSpectrumNumber(IPlotView view, OxyMouseDownEventArgs args)
        {
            var model = view.ActualModel as ISpectrumMarkerAware;
            if (model == null)
                return;

            args.Handled = true;

            Axis xaxis;
            Axis yaxis;

            view.ActualModel.GetAxesFromPoint(args.Position, out xaxis, out yaxis);

            if (xaxis != null)
            {
                var dataPos = xaxis.InverseTransform(args.Position.X);
                model.MoveTo(dataPos);
                args.Handled = true;
            }
        }

        private void HandleRemoveBand(IPlotView view, OxyMouseDownEventArgs args)
        {
            var model = view.ActualModel as ISpectrumXRangeAware;
            if (model == null)
                return;

            args.Handled = true;

            Axis xaxis;
            Axis yaxis;

            view.ActualModel.GetAxesFromPoint(args.Position, out xaxis, out yaxis);

            if (xaxis != null)
            {
                var dataPos = xaxis.InverseTransform(args.Position.X);
                model.RemoveRangeAt(dataPos);
                args.Handled = true;
            }
        }

        private void HandleArrowLeft(IPlotView view, OxyKeyEventArgs args)
        {
            var model = view.ActualModel as ISpectrumMarkerAware;
            if (model == null)
                return;
            args.Handled = true;
            model.StepLeft();
        }

        private void HandleArrowRight(IPlotView view, OxyKeyEventArgs args)
        {
            var model = view.ActualModel as ISpectrumMarkerAware;
            if (model == null)
                return;

            args.Handled = true;
            model.StepRight();
        }
    }
}