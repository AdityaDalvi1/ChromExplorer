using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using ChromExplorer.DataModel.Core;
using ChromExplorer.DataModel.PeakPicking;
using ChromExplorer.Infrastructure.Events;
using ChromExplorer.Infrastructure.Interfaces;
using ChromExplorer.Plotter.Markers;
using Microsoft.Practices.Prism.Events;

namespace ChromExplorer.Services.PickPeakerService.Services
{
    [Export(typeof(PeakPickerService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class PeakPickerService : IPartImportsSatisfiedNotification
    {
        public PeakPickerService()
        {
            Analyzer = PeakAnalyzers.Centroid;
            ThresholdType = ThresholdTypes.Percentage;
            Threshold = 10;
            LabelFormat = "{0:0.0}";
            LabelPlacement = MarkerPlacement.Top;
            LabelOrientation = MarkerOrientation.Horizontal;
        }

        [Import]
        private IEventAggregator _eventAggregator;

        [ImportMany]
        public ObservableCollection<IPeakPicker> Algorithms { get; set; }

        private IPeakPicker _selectedAlgorithm;
        public IPeakPicker SelectedAlgorithm
        {
            get { return _selectedAlgorithm; }
            set
            {
                _selectedAlgorithm = value;
                
                if(_eventAggregator != null)
                {
                    _eventAggregator.GetEvent<PeakPickerChangedEvent>().Publish(null);
                }
            }
        }

        public PeakAnalyzers Analyzer { get; set; }
        public ThresholdTypes ThresholdType { get; set; }
        public double Threshold { get; set; }

        public string LabelFormat { get; set; }
        public MarkerPlacement LabelPlacement { get; set; }
        public MarkerOrientation LabelOrientation { get; set; }

        public List<PeakInfo> GetPeaks(SimpleDataSource points, double min, double max)
        {
            if(_selectedAlgorithm == null || points == null)
                return new List<PeakInfo>();

            switch (Analyzer)
            {
                case PeakAnalyzers.Simple:
                    return _selectedAlgorithm.FindPeaks<SimplePeakAnalyzer>(points.Xvals, points.Yvals
                        , min, max
                        , Threshold, ThresholdType == ThresholdTypes.Percentage);

                case PeakAnalyzers.Centroid:
                    return _selectedAlgorithm.FindPeaks<CentroidPeakAnalyzer>(points.Xvals, points.Yvals
                        , min, max
                        , Threshold, ThresholdType == ThresholdTypes.Percentage);
            }
            
            return new List<PeakInfo>();
        }

        public void RaisePerformPeakickingEvent()
        {
            if (_eventAggregator != null)
            {
                _eventAggregator.GetEvent<PerformPeakPickingEvent>().Publish(null);
            }
        }

        void IPartImportsSatisfiedNotification.OnImportsSatisfied()
        {
            if(Algorithms != null)
            {
                this.SelectedAlgorithm = Algorithms.FirstOrDefault();
            }
        }
    }
}
