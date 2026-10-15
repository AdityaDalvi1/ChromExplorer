using ChromExplorer.Infrastructure.Events;
using ChromExplorer.Infrastructure.Interfaces;
using ChromExplorer.Modules.Ms.Services;
using log4net;
using Microsoft.Practices.Prism.Events;
using OxyPlot;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ChromExplorer.Modules.Ms.ViewModels
{
    [Export(typeof(MsMainViewViewModel))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class MsMainViewViewModel : IPartImportsSatisfiedNotification
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        private readonly MsControlService _msService;

        public PlotModel SpectrumPlot { get; }
        public PlotController SpectrumPlotController { get; }

        [ImportingConstructor]
        public MsMainViewViewModel(MsControlService service)
        {
            _msService = service;
            _msService.MeasurementsReceived += _msService_MeasurementsReceived;
         
            SpectrumPlot = new PlotModel();
            SpectrumPlotController = new PlotController();


            
        }

        private void _msService_MeasurementsReceived(object sender, Modas.Grpc.Client.Shared.MeasurementsReceivedEventArgs e)
        {
            SpectrumPlot.Series.Clear();

            var line = new OxyPlot.Series.LineSeries();
            line.Color = OxyColors.Blue;
            line.StrokeThickness = 1;

            foreach (var measurement in e.Dataset.Measurements)
            {
                line.Points.Add(new DataPoint(measurement.MassChargeRatio, measurement.Abundance));
            }

            SpectrumPlot.Series.Add(line);
            SpectrumPlot.InvalidatePlot(true);
        }

        public string SpectrumInfo { get; set; }

        public void OnImportsSatisfied()
        {
            
        }
    }
}
