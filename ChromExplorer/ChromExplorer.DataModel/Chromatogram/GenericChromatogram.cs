using ChromExplorer.DataModel.Core;
using ChromExplorer.DataModel.Interfaces;
using ChromExplorer.DataModel.PostProcessing;
using ChromExplorer.DataModel.Properties;
using ChromExplorer.DataModel.Spectrum;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SQLite;
using System.Linq;
using System.Windows;



namespace ChromExplorer.DataModel.Chromatogram
{
    public class GenericChromatogram : INotifyPropertyChanged
    {
        private BackgroundWorker _aggregateSpectrumWorker;
        private MsSpectrum _currentSpectrum;
        private string _description;
        private int _firstSelectedIndex;
        private SimpleDataSource _points;
        private double _ppd;
        private BackgroundWorker _rebuildChromatogramWorker;
        private int _selectedIndex;
        private IList<int> _selectedIndexes;
        private int _selectedIndexesCount;
        private double _selectedSIC;
        private IList<int> _selectedSpectrumIndexes;
        private string _title;
        private string _xAxisTooltipString;
        private readonly ObservableCollection<ChromatogramPoint> _chromPoints;
        private readonly IPostProcessingService _postProcessingService;
        private readonly IChromRebuilder _rebuilder;
        private readonly ISpectrumCollection _spectrums;
        private readonly IStatusUpdateService _statusService;

        private readonly string _connectionString = @"Data Source=source.db3;Version=3;";
        private bool _isTICInserted = false;
        private bool _isMassInserted = false;

        //private bool _isEICInserted = false;
        //private bool _readyForEIC = false;
        //private bool _skipFirstEIC = false;


        public GenericChromatogram()
        {
            _chromPoints = new ObservableCollection<ChromatogramPoint>();
            BuildPlotPoints();
            CreateWorkers();
            SetupTooltip(ChromPoints);
        }

        public GenericChromatogram(IStatusUpdateService statusService)
        {
            _statusService = statusService;
            _chromPoints = new ObservableCollection<ChromatogramPoint>();
            BuildPlotPoints();
            CreateWorkers();
            SetupTooltip(ChromPoints);
        }

        public GenericChromatogram(ISpectrumCollection spectrums, IChromRebuilder rebuilder)
        {
            _spectrums = spectrums;
            _rebuilder = rebuilder;
            _chromPoints = new ObservableCollection<ChromatogramPoint>();
            CurrentSpectrum = Spectrums.FirstOrDefault();
            CurrentSpectrum?.UpdateStatistics();
            BuildPlotPoints();
            CreateWorkers();
            SetupTooltip(ChromPoints);
        }

        public GenericChromatogram(ISpectrumCollection spectrums, IChromRebuilder rebuilder,
            IEnumerable<ChromatogramPoint> points)
        {
            _spectrums = spectrums;
            _rebuilder = rebuilder;
            _chromPoints = new ObservableCollection<ChromatogramPoint>(points);
            CurrentSpectrum = Spectrums.FirstOrDefault();
            CurrentSpectrum?.UpdateStatistics();
            BuildPlotPoints();
            CreateWorkers();
            SetupTooltip(ChromPoints);
        }

        public GenericChromatogram(ISpectrumCollection spectrums, IChromRebuilder rebuilder,
            IEnumerable<ChromatogramPoint> points, IStatusUpdateService statusService)
        {
            _statusService = statusService;
            _spectrums = spectrums;
            _rebuilder = rebuilder;
            _chromPoints = new ObservableCollection<ChromatogramPoint>(points);
            CurrentSpectrum = Spectrums.FirstOrDefault();
            CurrentSpectrum?.UpdateStatistics();
            BuildPlotPoints();
            CreateWorkers();
            SetupTooltip(ChromPoints);
        }

        public GenericChromatogram(ISpectrumCollection spectrums, IChromRebuilder rebuilder,
            IEnumerable<ChromatogramPoint> points, IStatusUpdateService statusService, IPostProcessingService ppService)
        {
            _statusService = statusService;
            _spectrums = spectrums;
            _rebuilder = rebuilder;
            _postProcessingService = ppService;
            _chromPoints = new ObservableCollection<ChromatogramPoint>(points);
            CurrentSpectrum = Spectrums.FirstOrDefault();
            CurrentSpectrum?.UpdateStatistics();

            // Deactivate any filters left on from a previously loaded file so the first TIC
            // built below for this new chromatogram is the raw, unfiltered curve.
            _postProcessingService?.Reset();

            BuildPlotPoints();
            CreateWorkers();
            SetupTooltip(ChromPoints);
        }

        public string Title
        {
            get { return _title; }
            set
            {
                if (value == _title) return;
                _title = value;
                OnPropertyChanged("Title");
            }
        }

        public string Description
        {
            get { return _description; }
            set
            {
                if (value == _description) return;
                _description = value;
                OnPropertyChanged("Description");
            }
        }
        //public SimpleDataSource Points
        //{
        //    get { return _points; }
        //    set
        //    {
        //        _points = value;

        //        // FIRST LOAD = TIC
        //        if (!_isTICInserted)
        //        {
        //            InsertAuditLog("TIC Integrated", "TIC Integrated");

        //            _isTICInserted = true;
        //        }
        //        // AFTER MASS = EIC
        //        else if (_isMassInserted && !_isEICInserted)
        //        {
        //            InsertAuditLog("EIC Extracted", "EIC Extracted");

        //            _isEICInserted = true;
        //        }

        //        OnPropertyChanged("Points");
        //    }
        //}
        public SimpleDataSource Points
        {
            get { return _points; }
            set
            {
                _points = value;

                OnPropertyChanged("Points");
            }
        }
        private void InsertAuditLog(string action, string description)
        {
            try
            {
                int userId = 0;
                if (Application.Current?.Properties?["UserId"] != null)
                    userId = Convert.ToInt32(Application.Current.Properties["UserId"]);

                using (var con = new SQLiteConnection(_connectionString))
                {
                    con.Open();

                    var sql = @"INSERT INTO AuditLogM
                    (action, description, is_active, inserted_by, inserted_date)
                    VALUES
                    (@action, @desc, 1, @uid, datetime('now','localtime'))";

                    using (var cmd = new SQLiteCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@action", action);
                        cmd.Parameters.AddWithValue("@desc", description);
                        cmd.Parameters.AddWithValue("@uid", userId);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                // MessageBox.Show(ex.ToString());
            }
        }
        //public SimpleDataSource Points
        //{
        //    get { return _points; }
        //    set
        //    {
        //        if (Equals(value, _points)) return;
        //        _points = value;
        //        OnPropertyChanged("Points");
        //    }
        //}

        public ObservableCollection<ChromatogramPoint> ChromPoints
        {
            get { return _chromPoints; }
        }

        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                if (value == _selectedIndex) return;
                _selectedIndex = value;
                SelectedIndexesCount = 1;
                FirstSelectedIndex = _selectedIndex;
                UpdateCurrentSpectrum(_selectedIndex);
                UpdateSIC();

                OnPropertyChanged("SelectedIndex");
            }
        }

        public double SelectedSIC
        {
            get { return _selectedSIC; }
            set
            {
                if (value.Equals(_selectedSIC)) return;
                _selectedSIC = value;
                OnPropertyChanged("SelectedSIC");
            }
        }

        public int SelectedIndexesCount
        {
            get { return _selectedIndexesCount; }
            set
            {
                if (value == _selectedIndexesCount) return;
                _selectedIndexesCount = value;
                OnPropertyChanged("SelectedIndexesCount");
            }
        }

        public int FirstSelectedIndex
        {
            get { return _firstSelectedIndex; }
            set
            {
                if (value == _firstSelectedIndex) return;
                _firstSelectedIndex = value;
                OnPropertyChanged("FirstSelectedIndex");
            }
        }

        //public IList<int> SelectedIndexes
        //{
        //    get { return _selectedIndexes; }
        //    set
        //    {
        //        if (Equals(value, _selectedIndexes)) return;
        //        _selectedIndexes = value;

        //        if (!_aggregateSpectrumWorker.IsBusy)
        //        {
        //            _selectedIndexes = value;

        //            if (_selectedIndexes != null)
        //            {
        //                SelectedIndexesCount = _selectedIndexes.Count;

        //                if (SelectedIndexesCount > 0)
        //                    FirstSelectedIndex = _selectedIndexes[0];
        //            }

        //            UpdateSIC();

        //            SetWaitStatus();
        //            _aggregateSpectrumWorker.RunWorkerAsync(_selectedIndexes);
        //        }

        //        OnPropertyChanged("SelectedIndexes");
        //    }
        //}

        public IList<int> SelectedIndexes
        {
            get { return _selectedIndexes; }
            set
            {
                _selectedIndexes = value;

                // FIRST TIC INTERACTION
                if (!_isTICInserted)
                {
                    InsertAuditLog("TIC Integrated", "TIC Integrated");

                    _isTICInserted = true;
                }
                // AFTER MASS -> USER INTERACTED WITH EIC
                else if (_isMassInserted)
                {
                    InsertAuditLog("EIC Extracted", "EIC Extracted");

                    _isMassInserted = false;
                }

                if (!_aggregateSpectrumWorker.IsBusy)
                {
                    _selectedIndexes = value;

                    if (_selectedIndexes != null)
                    {
                        SelectedIndexesCount = _selectedIndexes.Count;

                        if (SelectedIndexesCount > 0)
                            FirstSelectedIndex = _selectedIndexes[0];
                    }

                    UpdateSIC();

                    SetWaitStatus();

                    _aggregateSpectrumWorker.RunWorkerAsync(_selectedIndexes);
                }

                OnPropertyChanged("SelectedIndexes");
            }
        }
        //public IList<int> SelectedSpectrumIndexes
        //{
        //    get { return _selectedSpectrumIndexes; }
        //    set
        //    {
        //        if (Equals(value, _selectedSpectrumIndexes)) return;
        //        _selectedSpectrumIndexes = value;

        //        if (!_rebuildChromatogramWorker.IsBusy)
        //        {
        //            _selectedSpectrumIndexes = value;
        //            SetWaitStatus();
        //            _rebuildChromatogramWorker.RunWorkerAsync(_selectedSpectrumIndexes);
        //        }

        //        OnPropertyChanged("SelectedSpectrumIndexes");
        //    }
        //}


        public IList<int> SelectedSpectrumIndexes
        {
            get { return _selectedSpectrumIndexes; }
            set
            {
                //if (Equals(value, _selectedSpectrumIndexes)) return;

                _selectedSpectrumIndexes = value;

                //MessageBox.Show("Mass Selection Triggered");

                // MASS SELECTION LOG
                if (_isTICInserted)
                {
                    InsertAuditLog("Mass Selection on MS", "Mass Selected on MS");

                    _isMassInserted = true;


                }

                if (!_rebuildChromatogramWorker.IsBusy)
                {
                    _selectedSpectrumIndexes = value;

                    SetWaitStatus();

                    _rebuildChromatogramWorker.RunWorkerAsync(_selectedSpectrumIndexes);
                }

                OnPropertyChanged("SelectedSpectrumIndexes");
            }
        }
        public MsSpectrum CurrentSpectrum
        {
            get { return _currentSpectrum; }
            private set
            {
                if (Equals(value, _currentSpectrum)) return;
                _currentSpectrum = value;
                OnPropertyChanged("CurrentSpectrum");
                // _currentSpectrum.OnPropertyChanged("Index");
            }
        }

        private void CurrentSpectrumOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            OnPropertyChanged("CurrentSpectrum");
        }

        public string XAxisTooltipString
        {
            get { return _xAxisTooltipString; }
            set
            {
                if (value == _xAxisTooltipString) return;
                _xAxisTooltipString = value;
                OnPropertyChanged("XAxisTooltipString");
            }
        }

        public ISpectrumCollection Spectrums
        {
            get { return _spectrums; }
        }

        // --- Compound (Parent Ion) selector --------------------------------------------------
        // Additive only: when SelectedCompound is null (default), everything behaves exactly as
        // before. Setting it filters ChromPoints/Points down to just that Parent Ion's scans, and
        // _activeCompoundIndexMap translates the now-filtered SelectedIndex back to the correct
        // row in the full, unfiltered Spectrums collection so the mass spectrum stays correct.
        private List<ChromatogramPoint> _allChromPointsBackup;
        private List<int> _activeCompoundIndexMap;
        private double? _selectedCompound;
        private ObservableCollection<double> _availableCompounds;

        public ObservableCollection<double> AvailableCompounds
        {
            get
            {
                if (_availableCompounds == null)
                {
                    _availableCompounds = new ObservableCollection<double>();

                    // Read straight from ChromatogramPoint.SpectrumMeta.ParentIon (set once at load
                    // time from the DB record) rather than Spectrums[i].ParentIon — MsSpectrum only
                    // populates its ParentIon inside UpdateStatistics(), which GetSpectrum(i) never
                    // calls, so Spectrums[i].ParentIon always reads back as 0 here.
                    // Rounded to 2 decimal places only to absorb float noise: 609, 609.1, 609.2 etc.
                    // are distinct fragmentation variants and stay as separate entries.
                    var distinct = _chromPoints
                        .Select(p => Math.Round(p.SpectrumMeta.ParentIon, 2))
                        .Where(p => p > 0)
                        .Distinct()
                        .OrderBy(p => p)
                        .ToList();

                    foreach (var p in distinct)
                        _availableCompounds.Add(p);
                }

                return _availableCompounds;
            }
        }

        private ObservableCollection<CompoundInfo> _availableCompoundInfos;

        // Same distinct compounds as AvailableCompounds, but each carrying its acquisition
        // method's scan mass range (MFrom/MTo) too — used to label a compound as e.g.
        // "422 (255 to 470)" instead of just "422".
        public ObservableCollection<CompoundInfo> AvailableCompoundInfos
        {
            get
            {
                if (_availableCompoundInfos == null)
                {
                    _availableCompoundInfos = new ObservableCollection<CompoundInfo>();

                    var groups = _chromPoints
                        .Where(p => p.SpectrumMeta.ParentIon > 0)
                        .GroupBy(p => Math.Round(p.SpectrumMeta.ParentIon, 2))
                        .OrderBy(g => g.Key);

                    foreach (var g in groups)
                    {
                        var first = g.First();
                        var info = new CompoundInfo
                        {
                            ParentIon = g.Key,
                            MFrom = first.MFrom,
                            MTo = first.MTo
                        };

                        foreach (var p in g)
                        {
                            if (!info.MethodIds.Contains(p.MethodID))
                                info.MethodIds.Add(p.MethodID);
                        }

                        _availableCompoundInfos.Add(info);
                    }
                }

                return _availableCompoundInfos;
            }
        }

        public double? SelectedCompound
        {
            get { return _selectedCompound; }
            set
            {
                if (_selectedCompound == value) return;
                _selectedCompound = value;
                ApplyCompoundFilter();
                OnPropertyChanged("SelectedCompound");
            }
        }

        private void ApplyCompoundFilter()
        {
            if (_allChromPointsBackup == null)
            {
                _allChromPointsBackup = _chromPoints.ToList();
            }

            List<ChromatogramPoint> newPoints;

            if (_selectedCompound == null)
            {
                _activeCompoundIndexMap = null;
                newPoints = _allChromPointsBackup;
            }
            else
            {
                var indexes = new List<int>();

                for (var i = 0; i < _allChromPointsBackup.Count; i++)
                {
                    if (Math.Round(_allChromPointsBackup[i].SpectrumMeta.ParentIon, 2) == _selectedCompound.Value)
                    {
                        indexes.Add(i);
                    }
                }

                _activeCompoundIndexMap = indexes;
                newPoints = indexes.Select(i => _allChromPointsBackup[i]).ToList();
            }

            _chromPoints.Clear();
            foreach (var p in newPoints)
                _chromPoints.Add(p);

            // Clear the stale selection BEFORE rebuilding Points, not after: BuildPlotPoints()
            // calls UpdateSIC() internally at its own end, and if _selectedIndexes still holds
            // the previous channel's (differently-sized) selection at that moment, UpdateSIC
            // indexes the newly-rebuilt Points.Yvals out of bounds. Direct field assignment here
            // (not the SelectedIndexes setter) avoids redundant audit-log/aggregate-worker calls
            // for what's just a transient clearing step.
            _selectedIndexes = null;
            _selectedIndexesCount = 0;

            BuildPlotPoints();

            _selectedIndex = -1;
            SelectedIndex = _chromPoints.Count > 0 ? 0 : -1;

            // Auto-integrate: select the whole newly-filtered range so the chromatogram/mass
            // spectrum immediately reflect the full aggregate for whichever channel is now
            // showing (same as right after a fresh load), instead of just the single first scan.
            // Going through the real SelectedIndexes setter (not a direct field assignment) is
            // what actually triggers TIC integration and the aggregate spectrum computation.
            //
            // Sized to Points.Length (the POST-PROCESSED plot data UpdateSIC actually indexes
            // into, now rebuilt and stable), not _chromPoints.Count — post-processing (baseline
            // removal, smoothing, etc. inside BuildPlotPoints) can trim the point count.
            var selectableCount = Points?.Length ?? 0;

            if (selectableCount > 0)
            {
                SelectedIndexes = Enumerable.Range(0, selectableCount).ToList();
            }
            else
            {
                OnPropertyChanged("SelectedIndexes");
                OnPropertyChanged("SelectedIndexesCount");
            }
        }

        private int MapToSpectrumIndex(int filteredIndex)
        {
            if (_activeCompoundIndexMap == null)
                return filteredIndex;

            if (filteredIndex < 0 || filteredIndex >= _activeCompoundIndexMap.Count)
                return -1;

            return _activeCompoundIndexMap[filteredIndex];
        }

        public double PPD
        {
            get { return _ppd; }
            set
            {
                if (value.Equals(_ppd)) return;
                _ppd = value;
                OnPropertyChanged("PPD");
            }
        }

        public string Filename { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void SetupTooltip(IEnumerable<ChromatogramPoint> points)
        {
            var units = "ms";
            var point = points?.FirstOrDefault();

            if (point != null)
                units = point.TimeUnits;

            XAxisTooltipString = string.Format("Retention time ({0})", units);
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

        public void RebuildChromatogram()
        {

            if (!_rebuildChromatogramWorker.IsBusy)
            {
                SetWaitStatus();
                _rebuildChromatogramWorker.RunWorkerAsync(_selectedSpectrumIndexes);
            }
        }
        // Runs on the BackgroundWorker's pool thread — must NOT touch Points/SelectedSIC (or
        // anything else that raises PropertyChanged) directly here. Doing so previously raised
        // "Points" changed from a non-UI thread, which the WPF-bound chromatogram plot silently
        // failed to pick up (the resulting exception was swallowed by RunWorkerCompleted never
        // checking e.Error) — this is why "Apply to EIC/TIC" looked like it did nothing. The
        // actual rebuilt data is computed here and only applied to the bound properties in
        // RebuildChromatogramWorkerRunWorkerCompleted, which BackgroundWorker always marshals
        // back onto the UI thread.
        private void RebuildChromatogramWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            var indexes = e.Argument as IList<int>;
            e.Result = ComputeChromatogramPoints(indexes);
        }

        private SimpleDataSource ComputeChromatogramPoints(IList<int> indexes)
        {
            var points = _rebuilder.Rebuild(indexes, ChromPoints);

            if (_postProcessingService != null)
            {
                points = _postProcessingService.Processing(points);
            }

            return points;
        }

        private void RebuildChromatogramWorkerRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error == null && e.Result is SimpleDataSource rebuiltPoints)
            {
                Points = rebuiltPoints;
                UpdateSIC();
            }

            SetReadyStatus();
        }

        private void AggregateSpectrumWorkerRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            SetReadyStatus();
        }

        private void AggregateSpectrumWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            var indexes = e.Argument as IEnumerable<int>;
            UpdateCurrentSpectrum(indexes);
        }

        private void UpdateSIC()
        {
            if (SelectedIndexesCount == 1)
            {
                if (_selectedIndex >= 0 && _selectedIndex < Points.Length)
                {
                    SelectedSIC = Points.Yvals[_selectedIndex];
                }
                else
                {
                    SelectedSIC = 0.0;
                }
            }
            else
            {
                double sic = 0;

                if (_selectedIndexes != null)
                {
                    foreach (var index in _selectedIndexes)
                    {
                        sic += Points.Yvals[index];
                    }
                }

                SelectedSIC = sic;
            }
        }

        public void RebuilCurrentSpectrum()
        {
            // A multi-scan selection (e.g. the whole-TIC aggregate shown right after a file loads)
            // leaves _selectedIndex at 0 — it must win here, otherwise a post-processing re-apply
            // replaces the aggregate spectrum with scan 0 alone, which is blank.
            bool multiSelected = _selectedIndexes != null && _selectedIndexesCount > 1;

            if (_selectedIndex >= 0 && !multiSelected)
            {
                UpdateCurrentSpectrum(_selectedIndex);
            }
            else if (_selectedIndexes != null)
            {
                if (!_aggregateSpectrumWorker.IsBusy)
                {
                    SetWaitStatus();
                    _aggregateSpectrumWorker.RunWorkerAsync(_selectedIndexes);
                }
            }
        }

        private void UpdateCurrentSpectrum(int index)
        {
            var spectrumIndex = MapToSpectrumIndex(index);

            if (Spectrums != null)
            {
                if (spectrumIndex >= 0 & spectrumIndex < Spectrums.Count)
                {
                    var spectrum = Spectrums[spectrumIndex];

                    CurrentSpectrum
                        = _postProcessingService != null ? _postProcessingService.Processing(spectrum) : spectrum;

                    CurrentSpectrum?.UpdateStatistics();

                    return;
                }
            }

            CurrentSpectrum = null;
        }

        //[Async]
        private void UpdateCurrentSpectrum(IEnumerable<int> indexes)
        {
            // indexes are in the currently-displayed (possibly compound-filtered) index space;
            // translate each one back to its real row in the full Spectrums collection, same as
            // the single-index overload does, so a multi-select range never pulls in scans from
            // a different compound than the one currently filtered.
            var mappedIndexes = indexes?.Select(MapToSpectrumIndex).Where(i => i >= 0).ToList();

            var spectrum = Spectrums != null ? Spectrums.GetAggregateSpectrum(mappedIndexes) : null;

            if (spectrum == null)
            {
                CurrentSpectrum = null;
                return;
            }

            CurrentSpectrum
                = _postProcessingService != null ? _postProcessingService.Processing(spectrum) : spectrum;

            CurrentSpectrum.UpdateStatistics();
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

        public void SetAppStatus(bool waitCursor, string status)
        {
            if (_statusService != null)
            {
                _statusService.AppStatus = status;
                _statusService.SetWaitCursor(waitCursor);
            }
        }

        private void BuildPlotPoints()
        {
            var x = new double[ChromPoints.Count];
            var y = new double[ChromPoints.Count];
            for (var i = 0; i < ChromPoints.Count; i++)
            {
                x[i] = ChromPoints[i].Time;
                y[i] = ChromPoints[i].TIC;
            }

            Points = new SimpleDataSource(x, y);
            if (_postProcessingService != null)
            {
                Points = _postProcessingService.Processing(Points);
            }
            UpdateSIC();
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}