using System;
using OxyPlot;

namespace ChromExplorer.OxyPlot.Demo
{
    public enum ControllerType
    {
        Default,
        SpectrumPlot,
    }

    public class PlotControllerFactory
    {
        public static PlotController Create(ControllerType type = ControllerType.Default)
        {
            switch (type)
            {
                case ControllerType.Default:
                    return CreateDefaultController();
                    break;

                case ControllerType.SpectrumPlot:
                    return CreateSpectrumPlotController();
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        private static PlotController CreateDefaultController()
        {
            ChromatogramPlotController controller = new ChromatogramPlotController();
            controller.Setup();
           
            return controller;
        }
            
        private static PlotController CreateSpectrumPlotController()
        {
            SpectrumPlotController controller = new SpectrumPlotController();
            controller.Setup();

            return controller;
        }
    }
}