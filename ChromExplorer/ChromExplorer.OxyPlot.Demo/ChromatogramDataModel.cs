using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using ChromExplorer.DataModel.Chromatogram;
using ChromExplorer.DataModel.Interfaces;
using ChromExplorer.DataModel.PostProcessing;
using ChromExplorer.DataModel.Spectrum;
using ChromExplorer.Infrastructure;
using log4net;
using OxyPlot;

namespace ChromExplorer.OxyPlot.Demo
{
    public class ChromatogramDataModel : IOxyChromatogramModel, INotifyPropertyChanged
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private BackgroundWorker _aggregateSpectrumWorker;
        private BackgroundWorker _rebuildChromatogramWorker;

        private readonly IChromRebuilder _rebuilder;
        private readonly ISpectrumCollection _spectrums;
        private readonly IPostProcessingService _postProcessingService = null;
        private readonly IStatusUpdateService _statusService = null;
        private readonly List<ChromatogramPoint> _sourceChromatogram = null;
        private ISpectrum _currentSpectrum;
        private int _selectedSpectumIndex = -1;
        private List<ChromatogramPoint> _instrumentChromatogram;

        public ChromatogramDataModel(ISpectrumCollection spectrums, IChromRebuilder rebuilder,
            IEnumerable<ChromatogramPoint> points, IPostProcessingService postProcessingService = null, IStatusUpdateService statusService = null)
        {
            SICDescriptors = new ObservableCollection<SICDescriptor>();
            _spectrums = spectrums;
            _rebuilder = rebuilder;
            _sourceChromatogram = new List<ChromatogramPoint>(points);

            //InstrumentChromatogram = new List<ChromatogramPoint>(_sourceChromatogram);
            var filter = new MsPowerFilter(1);
            FilterChromatogram(filter);
            
            _postProcessingService = postProcessingService;
            
            SelectedSpectumIndex = 0;

            CreateWorkers();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public List<ChromatogramPoint> InstrumentChromatogram
        {
            get => _instrumentChromatogram;
            set
            {
                if (Equals(value, _instrumentChromatogram)) return;
                _instrumentChromatogram = value;
                OnPropertyChanged();
            }
        }

        public ISpectrum CurrentSpectrum
        {
            get => _currentSpectrum;
            private set
            {
                if (Equals(value, _currentSpectrum)) return;
                _currentSpectrum = value;
                OnPropertyChanged();
            }
        }

        public int SelectedSpectumIndex 
        {
            get => _selectedSpectumIndex;
            set
            {
                //if (value == _selectedSpectumIndex) return;
                if (value < 0) return;
                if (value >= InstrumentChromatogram.Count) return;
                    
                _selectedSpectumIndex = value;
                UpdateToSpectrumIndex(_selectedSpectumIndex);
                OnPropertyChanged();
            }
        }

        public string Title { get; set; }
        public string Description { get; set;  }

        public string Filename { get; set; }

        public double PPD { get; set; }

        public ObservableCollection<SICDescriptor> SICDescriptors { get; private set; }

        public void FilterChromatogram(IChromFilter filter)
        {
            if (_sourceChromatogram == null)
                return;

            var list = new List<ChromatogramPoint>(_sourceChromatogram.Count);

            foreach (var point in _sourceChromatogram)
            {
                if(filter.Accept(point))
                    list.Add(point);
            }

            InstrumentChromatogram = list;
        }

        public void AddSelectedChromatogram(SpectrumXRangeAnnotation annotation)
        {
            log.Debug($"GetSelectedChromatogram: {annotation.MinimumX} - {annotation.MaximumX}");

            
            if(!_rebuildChromatogramWorker.IsBusy)
                _rebuildChromatogramWorker.RunWorkerAsync(annotation);
        }

        public void RemoveSelectedChromatogram(SpectrumXRangeAnnotation annotation)
        {
            SICDescriptor toDelete = null;

            foreach (var d in SICDescriptors)
            {
                if (d.Annotation == annotation)
                {
                    toDelete = d;
                }
            }

            SICDescriptors.Remove(toDelete);
            //throw new NotImplementedException();
        }

        public void ExportChromPlots(string filename)
        {
            var exporter = new GnuplotChromExporter(filename);
            exporter.ExportChromatogramPlot(this);
        }

        public List<ChromatogramPoint> GetChromatogram(IChromFilter filter)
        {
            if (_sourceChromatogram == null)
                return new List<ChromatogramPoint>();

            var list = new List<ChromatogramPoint>();

            foreach (var point in _sourceChromatogram)
            {
                if (filter.Accept(point))
                    list.Add(point);
            }

            return list;
        }

        

        public ISpectrum GetAggregatedSpectrum(List<ChromatogramPoint> points)
        {
            if (_sourceChromatogram == null)
                return null;

           
            var indexes = points.Select(point => point.Index).ToList();

            var spectrum = _spectrums != null ? _spectrums.GetAggregateSpectrum(indexes) : null;

            return spectrum;
        }

        public void SetSelectedSpectrumIndexes(double xmin, double xmax)
        {
            if (_aggregateSpectrumWorker.IsBusy)
            {
                log.Warn("_aggregateSpectrumWorker is busy");
                return;
            }
            log.DebugFormat("Selects spectra from {0} to {1}", xmin, xmax);
            int startIndex = GetSpectrumIndexByRT(xmin);
            int stopIndex = GetSpectrumIndexByRT(xmax);

            log.DebugFormat("Selects spectra from {0} to {1}", startIndex, stopIndex);

            var indexes = new List<int>();

            for (int i = startIndex; i <= stopIndex; i++)
            {
                indexes.Add(InstrumentChromatogram[i].Index);
            }

            log.DebugFormat("Selected indexes: {0}", String.Join(", ", indexes.ToArray()));

            _aggregateSpectrumWorker.RunWorkerAsync(indexes);

        }

        private void UpdateToSpectrumIndex(int cindex)
        {
            if (cindex < 0 || cindex >= InstrumentChromatogram.Count)
            {
                return;
            }

            var index = InstrumentChromatogram[cindex].Index;
            
            if (_spectrums != null)
            {
                if (index >= 0 & index < _spectrums.Count)
                {
                    var spectrum = _spectrums[index];
                    spectrum.UpdateStatistics();
                    
                    CurrentSpectrum
                        = _postProcessingService != null ? _postProcessingService.Processing(spectrum) : spectrum;

                    //CurrentSpectrum.UpdateStatistics();

                    return;
                }
            }

            CurrentSpectrum = null;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private int GetSpectrumIndexByRT(double rt)
        {
            var pt = new ChromatogramPoint() { Time = rt };
            int index = InstrumentChromatogram.BinarySearch(pt
                , new ComparisonComparer<ChromatogramPoint>((a, b) => a.Time.CompareTo(b.Time)));

            if (index < 0)
                index = ~index;

            if (index == InstrumentChromatogram.Count)
                index = InstrumentChromatogram.Count - 1;

            return index;
        }

        private void CreateWorkers()
        {
            _aggregateSpectrumWorker = new BackgroundWorker();
            _aggregateSpectrumWorker.DoWork += AggregateSpectrumWorkerDoWork;
            _aggregateSpectrumWorker.RunWorkerCompleted
                += AggregateSpectrumWorkerRunWorkerCompleted;

            _rebuildChromatogramWorker = new BackgroundWorker();
            _rebuildChromatogramWorker.DoWork += RebuildChromatogramWorkerDoWork;
            _rebuildChromatogramWorker.RunWorkerCompleted
                += RebuildChromatogramWorkerRunWorkerCompleted;
        }

        private void AggregateSpectrumWorkerRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            SetReadyStatus();
        }

        private void AggregateSpectrumWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            var indexes = e.Argument as IEnumerable<int>;
            SetWaitStatus();

            UpdateCurrentSpectrum(indexes);
        }

        private void UpdateCurrentSpectrum(IEnumerable<int> indexes)
        {
            var spectrum = _spectrums != null ? _spectrums.GetAggregateSpectrum(indexes) : null;

            if (spectrum == null)
            {
                CurrentSpectrum = null;
                return;
            }

            CurrentSpectrum
                = _postProcessingService != null ? _postProcessingService.Processing(spectrum) : spectrum;

            CurrentSpectrum.UpdateStatistics();
        }

        private void RebuildChromatogramWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            SetWaitStatus();
            var indexes = e.Argument as IList<int>;
            CreateSicChromatogram(e.Argument as SpectrumXRangeAnnotation);
        }

        public void CreateSicChromatogram(SpectrumXRangeAnnotation annotation)
        {

            if (CurrentSpectrum == null)
                return;

            var startIndex = CurrentSpectrum.Points.GetIndexByX(annotation.MinimumX);
            var stopIndex = CurrentSpectrum.Points.GetIndexByX(annotation.MaximumX);

            log.Debug($"GetSelectedChromatogram: {startIndex} - {stopIndex}");

            var indexes = new List<int>();  

            for (int i = startIndex; i <= stopIndex; i++)
            {
                indexes.Add(i);
            }
            var points = _rebuilder.Rebuild(indexes, InstrumentChromatogram);

            var d = new SICDescriptor(annotation);
            var line = new MinMaxLineSeries { Title = $"R{SICDescriptors.Count}" };
            line.StrokeThickness = UISettings.ChromLineThickness;
            line.Color = OxyColor.FromArgb(255, annotation.Fill.R, annotation.Fill.G, annotation.Fill.B);

            foreach (var p in points.Points)
            {
                line.Points.Add(new DataPoint(p.X, p.Y));
            }

            d.LineSeries = line;

            SICDescriptors.Add(d);

            log.Debug($"UpdateChrom: got SIC chromatogram, points.Count = {points.Length}");

        }

        /*
         * _mainSeries = new MinMaxLineSeries { Title = "TIC" };
            _mainSeries.StrokeThickness = 1;
            _mainSeries.Color = OxyColors.DarkBlue;



            foreach (var p in _chromatogram.CurrentSpectrum.Points.Points)
            {
                _mainSeries.Points.Add(new DataPoint(p.X, p.Y));
            }

            _mainSeries.InvalidateData();
         */

        private void RebuildChromatogramWorkerRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            SetReadyStatus();
        }

        private void SetWaitStatus()
        {
            if (_statusService != null)
            {
                _statusService.AppStatus = "Integrating spectrum...";
                _statusService.SetWaitCursor(true);
            }
        }

        private void SetReadyStatus()
        {
            if (_statusService != null)
            {
                _statusService.AppStatus = "Ready";
                _statusService.SetWaitCursor(false);
            }
        }
    }
}