using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using ChromExplorer.DataModel.Chromatogram;
using ChromExplorer.DataModel.Core;
using ChromExplorer.DataModel.DAL;
using ChromExplorer.DataModel.Interfaces;
using ChromExplorer.DataModel.Resampling;
using ChromExplorer.DataModel.Spectrum;
using ChromExplorer.Infrastructure;
using log4net;
using Mindscape.LightSpeed;
using Mindscape.LightSpeed.Querying;

namespace ChromExplorer.ModasReader
{
    public class ModasDataReader : IDisposable, ISpectrumCollection, IChromRebuilder
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly MethodFileCache _methodCache;
        private readonly CalibrationCache _calibrationCache;
        private readonly LightSpeedContext<ModasDataModelUnitOfWork> _context;
        private readonly VersionUpdater _updater;
        private readonly List<ChromatogramPoint> _chromPoints;
        private readonly List<Spectrum> _spectrumRecords;
        private readonly IUniformResampler _resampler;
        private ModasIntensityStore _store;

        private double _mFrom = double.PositiveInfinity;
        private double _mTo = double.NegativeInfinity;
        private double _ppd;

        private bool _normalizeByIT;
        private double _referenceITSec = 0.1;

        private TimeUnits _timeUnits;
        public QueryExpression SpectrumFilter { get; set; }

        private int _resampledSpectrumSize;


        public IList<ChromatogramPoint> ChromPoints
        {
            get { return _chromPoints; }
        }

        public double MassFrom { get { return _mFrom; } }
        public double MassTo { get { return _mTo; } }
        public double PPD
        {
            get { return _ppd; }
        }

        public ModasDataReader(string filename, TimeUnits units = TimeUnits.Ms, QueryExpression spectrumFilter = null)
        {
            _resampler = new LinearInterpolationResampler();
            //_resampler = new EmptyResampler();
            _spectrumRecords = new List<Spectrum>();
            _chromPoints = new List<ChromatogramPoint>();
            _methodCache = new MethodFileCache();
            _calibrationCache = new CalibrationCache();
            _context = new LightSpeedContext<ModasDataModelUnitOfWork>();
            _updater = new VersionUpdater(Context);

            SpectrumFilter = spectrumFilter;
            _timeUnits = units;

            FileName = filename;
            IsInitialized = false;
            IsContextInitialized = false;

            Initialize();
        }

        public void Initialize()
        {
            TimeUnits units;
            QueryExpression spectrumFilter;
            try
            {
                InitContext();


                if (_updater.NeedUpdate)
                {
                    _updater.UpdateFromPreviousVersions();
                    _updater.CreateMissedIndexes();
                }

                if (!IsContextInitialized)
                    return;

                //EstimateMassRange();
                //LoadChromPoints(_timeUnits, _spectrumFilter);
                //IsInitialized = true;
            }
            catch (Exception ex)
            {
                log.Error("InitContext(): Failed to init context", ex);
            }
        }

        public string FileName { get; private set; }
        public bool IsInitialized { get; private set; }
        public bool IsContextInitialized { get; private set; }

        public bool IsModasFile
        {
            get { return _updater.IsModasFile; }
        }

        public bool UpdateRequired
        {
            get { return _updater.NeedUpdate; }
        }

        public void UpdateFromPreviousVersions()
        {
            _updater.UpdateFromPreviousVersions();
        }

        public IList<Method> GetMethods()
        {
            using (var uof = Context.CreateUnitOfWork())
            {
                return uof.Find<Method>();
            }
        }

        public IList<MethodFileLite> GetLiteMethods()
        {
            var methods = GetMethods();

            foreach (var method in methods)
            {
                _methodCache.Add(method);
            }

            return _methodCache.Methods;
        }

        public IList<Calibrations> GetCalibrations()
        {
            using (var uof = Context.CreateUnitOfWork())
            {
                return uof.Find<Calibrations>();
            }
        }

        public void LoadCalibrations()
        {
            var calibrations = GetCalibrations();

            foreach (var c in calibrations)
            {
                _calibrationCache.Add(c);
            }
        }

        public void ClearMethodCache()
        {
            _methodCache.Clear();
        }

        public void ClearCalibrationCache()
        {
            _calibrationCache.Clear();
        }

        private void InitContext()
        {
            //try
            //{
            var fi = new FileInfo(this.FileName);

            if (fi.Exists)
            {
                Context.ConnectionString = string.Format("Data Source=\"{0}\"", this.FileName);
                Context.DataProvider = DataProvider.SQLite3;
                Context.IdentityMethod = IdentityMethod.KeyTable;

                IsContextInitialized = true;
            }
            //}
            //catch(Exception ex)
            //{
            //    log.Error("InitContext(): Failed to init context", ex);
            //}
        }

        #region IDisposable Members
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ModasDataReader()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_store != null)
                    _store.Dispose();
            }

            // free unmanaged resources
        }
        #endregion

        #region Implementation of IChromRebuilder

        public SimpleDataSource Rebuild(IList<int> indexes, IList<ChromatogramPoint> points)
        {

            if (points == null)
                return null;

            int size = points.Count;
            var x = new double[size];
            var y = new double[size];

            int i = 0;

            foreach (var point in points)
            {
                x[i] = point.Time;
                y[i] = 0.0;

                double divider = point.SpectrumMeta.InjectionTime > 0 ? point.SpectrumMeta.InjectionTime : 1.0;

                if (indexes != null)
                {
                    foreach (var index in indexes)
                    {
                        y[i] += _store.Spectrums[point.Index].Data[index] / divider;
                    }
                }
                else
                {
                    y[i] = point.TIC;
                }

                i++;
            }

            return new SimpleDataSource(x, y);
        }

        #endregion

        #region Implementation of ICollection<MsSpectrum>

        public void Add(MsSpectrum item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(MsSpectrum item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(MsSpectrum[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(MsSpectrum item)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get { return _chromPoints.Count; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public IEnumerator<MsSpectrum> GetEnumerator()
        {
            var count = _chromPoints.Count;
            for (int i = 0; i < count; i++)
            {
                yield return GetSpectrum(i);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region Implementation of IList<MsSpectrum>

        public int IndexOf(MsSpectrum item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, MsSpectrum item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public MsSpectrum this[int index]
        {
            get { return GetSpectrum(index); }
            set { throw new NotImplementedException(); }
        }

        #endregion

        #region Implementation of ISpectrumCollection

        public int SpectrumSize
        {
            get { return _resampler.XVals != null ? _resampler.XVals.Length : 0; }
        }

        public IEnumerable<double> Positions
        {
            get { return _resampler.XVals; }
        }

        public LightSpeedContext<ModasDataModelUnitOfWork> Context
        {
            get { return _context; }
        }

        public MsSpectrum GetAggregateSpectrum(IEnumerable<int> indexes)
        {
            if (indexes == null)
                return null;

            var aggregated = new double[this.SpectrumSize];

            var firstIndex = indexes.FirstOrDefault();

            if (firstIndex < 0 || firstIndex >= _chromPoints.Count)
            {
                return new MsSpectrum(new SimpleDataSource(this.Positions.ToArray(), aggregated));
            }

            var fakeCp = new ChromatogramPoint(_chromPoints[firstIndex]);



            int count = 0;
            double injectionTime = 0.0;
            foreach (var index in indexes)
            {
                if (index >= 0 && index < _store.Spectrums.Count)
                {

                    var spectrum = _store.Spectrums[index];
                    fakeCp.SpectrumIndexes.Add(index);

                    var cp = _chromPoints[index];
                    injectionTime += cp.SpectrumMeta.InjectionTime;


                    for (int i = 0; i < this.SpectrumSize; i++)
                    {
                        aggregated[i] += spectrum.Data[i];
                    }

                    var msSpectrum = this[index];
                    count++;
                }
            }

            fakeCp.SpectrumMeta.InjectionTime = injectionTime;

            log.DebugFormat("GetAggregateSpectrum: Averaged spectrum constructed from {0}", count);

            return new MsSpectrum(new SimpleDataSource(this.Positions.ToArray(), aggregated), fakeCp) { BaselineRemoved = this.BaselineRemoved, PPD = _ppd };
        }

        #endregion

        public void SetGlobalStartTime()
        {
            log.Debug("SetGlobalStartTime() called");

            using (var uof = Context.CreateUnitOfWork())
            {
                var records = uof.Find<Spectrum>(Entity.Attribute("Id") < 100);

                if (records.Count > 0)
                    _startTime = records[0].Timestamp;
            }
        }

        public void LoadChromPoints(TimeUnits units, QueryExpression spectrumFilter = null)
        {
            _spectrumRecords.Clear();
            _chromPoints.Clear();

            // Needed so BuildPoint can look up each record's Method (for MFrom/MTo) below.
            GetLiteMethods();

            SetGlobalStartTime();

            using (var uof = Context.CreateUnitOfWork())
            {
                IList<Spectrum> records = null;

                if (spectrumFilter == null)
                    records = uof.Find<Spectrum>();
                else
                    records = uof.Find<Spectrum>(spectrumFilter);

                if (records == null)
                    return;

                var index = 0;
                foreach (var spectrum in records)
                {
                    //if (index == 0)
                    //{
                    //    _startTime = spectrum.Timestamp;
                    //}

                    _spectrumRecords.Add(spectrum);
                    _chromPoints.Add(BuildPoint(index++, spectrum, units.ToString(), TimeMultipliers.GetMultyplier(units)));
                }
            }
        }

        private DateTime _startTime = DateTime.Now;
        private ChromatogramPoint BuildPoint(int index, Spectrum record, string timeUnits = "ms", double fromMs = 1.0)
        {
            double offsetInMs = (record.Timestamp - _startTime).TotalMilliseconds;
            var retval = new ChromatogramPoint()
            {
                Index = index,
                SpectrumID = record.Id,
                TIC = record.Tic,
                Time = offsetInMs * fromMs,
                TimeUnits = timeUnits,
                SpectrumSize = record.DataSize,
                DigitizerSampleRate = record.DigitizerSampleRate,
                MethodID = record.MethodId ?? -1,
                CalibrationID = record.CalibrationsId ?? -1,
                Timestamp = record.Timestamp,

            };

            if (record.MethodId.HasValue)
            {
                var pointMethod = _methodCache.GetMethod(record.MethodId.Value);
                if (pointMethod != null)
                {
                    retval.MFrom = pointMethod.MFrom;
                    retval.MTo = pointMethod.MTo;
                }
            }

            var meta = new FileMappedSpectrumData.SpectrumMetaData()
            {
                EllapsedMilliseconds = (long)offsetInMs,
                InjectionTime = record.InjectionTime,
                MsPower = record.MsPower,
                ParentIon = record.ParentIonMass
            };

            retval.SpectrumMeta = meta;
            return retval;
        }

        [Obsolete]
        public void EstimateMassRange()
        {
            var methods = GetLiteMethods();

            foreach (var method in methods)
            {
                if (method.MFrom < _mFrom)
                    _mFrom = method.MFrom;

                if (method.MTo > _mTo)
                    _mTo = method.MTo;
            }

        }

        [Obsolete]
        public int GetChromPointsCount()
        {
            using (var uof = Context.CreateUnitOfWork())
            {
                return (int)uof.Count<Spectrum>();
            }
        }

        public const double DefaultPPD = 20.0;
        public const double DefaultNoiseFilterLevel = 3.0;

        [Obsolete]
        public double GetPPD()
        {
            if (_chromPoints == null)
                return DefaultPPD;

            double ppd = 0;

            foreach (var record in _spectrumRecords)
            {
                if (record.MethodId != null)
                {
                    var method = _methodCache.GetMethod(record.MethodId.Value);
                    if (method != null)
                    {
                        var massRange = Math.Abs(method.MTo - method.MFrom);
                        var lppd = Math.Floor((double)record.DataSize / massRange);

                        if (lppd > ppd)
                            ppd = lppd;

                    }
                }
            }

            if (ppd > 0)
                return ppd;

            return DefaultPPD;
        }



        public void UpdateStatistic()
        {
            _ppd = DefaultPPD;
            _mFrom = double.PositiveInfinity;
            _mTo = double.NegativeInfinity;

            if (_chromPoints == null)
                return;

            var methods = GetLiteMethods(); // Just loading methods in cache

            double ppd = 0;

            foreach (var record in _spectrumRecords)
            {
                if (record.MethodId == null) continue;

                var method = _methodCache.GetMethod(record.MethodId.Value);
                if (method == null) continue;

                var massRange = Math.Abs(method.MTo - method.MFrom);
                var lppd = Math.Floor((double)record.DataSize / massRange);

                if (lppd > ppd)
                    ppd = lppd;

                if (method.MFrom < _mFrom)
                    _mFrom = method.MFrom;

                if (method.MTo > _mTo)
                    _mTo = method.MTo;
            }

            if (ppd > 0)
                _ppd = ppd;
        }

        public IList<ChromatogramPoint> ReadChromatogram(TimeUnits units)
        {
            LoadChromPoints(units, SpectrumFilter);
            return ChromPoints;
        }

        public bool HasLoadErrors { get; set; }

        public bool BaselineRemoved { get; set; }

        private IList<double> _storedTic = null;
        public void LoadData(double min, double max, double ppd, bool rebuildTic, IStatusUpdateService statusReport = null, bool removeBaseLine = false, double nfl = 3.0, bool normalizeByIT = false,
    double referenceIT = 100.0)
        {
            this._normalizeByIT = normalizeByIT;
            this._referenceITSec = referenceIT / 1000.0;
            HasLoadErrors = false;
            if (_chromPoints == null || _chromPoints.Count <= 0)
                return;

            if (_store != null)
                _store.Dispose();

            _storedTic = null;
            _ppd = ppd;
            BaselineRemoved = removeBaseLine;

            LoadCalibrations();

            _store = new ModasIntensityStore(_chromPoints.Count);

            _resampler.Step = 1.0 / ppd;
            _resampler.XMin = min; //Math.Floor(min);
            _resampler.XMax = max; //Math.Ceiling(max);
            _resampler.InitXVals();

            if (_methodCache.Methods.Count == 0)
                GetLiteMethods();


            double step = 0;

            if (statusReport != null)
            {
                step = (statusReport.ProgressMax - statusReport.ProgressMin) / _chromPoints.Count;
            }

            if (rebuildTic)
                _storedTic = new List<double>();

            try
            {

                using (var uof = Context.CreateUnitOfWork())
                {

                    for (int i = 0; i < _chromPoints.Count; i++)
                    {
                        if (statusReport != null)
                        {
                            statusReport.ReportProgress((double)i * step);
                        }

                        var point = ChromPoints[i];

                        var sd = GetSpectrumData(point.SpectrumID);
                        //    uof.Find<SpectrumData>(Entity.Attribute("SpectrumId") == point.SpectrumID).FirstOrDefault();

                        if (sd != null)
                        {
                            var method = _methodCache.GetMethod(point.MethodID);

                            var calibration = _calibrationCache.GetCalibration(point.CalibrationID);

                            if (calibration == null)
                            {
                                //var cals = uof.
                                var ms = uof.Find<SpetrumCalibrations>(Entity.Attribute("SpectrumId") == point.SpectrumID);

                                var firstOrDefault = ms.FirstOrDefault(m => m.Calibrations.CodeName == "MASS");
                                if (firstOrDefault != null)
                                {
                                    calibration = _calibrationCache.GetCalibration(firstOrDefault.Calibrations);
                                }
                            }

                            if (!method.IsInitialized)
                            {
                                method.BuildMassList(point.SpectrumSize, point.DigitizerSampleRate, calibration);
                            }

                            var ss =
                                new ModasSpectrumStore(GetResapmledIntensities(sd, point.SpectrumSize,
                                                                               method.MassList.ToArray(), method.SamplesToSkip));

                            double normFactor = this._normalizeByIT ? this._referenceITSec / point.SpectrumMeta.InjectionTime : 1.0;
                            double tic = 0;
                            tic = removeBaseLine ? ss.GetTicAndRefine(normFactor, nfl) : ss.GetTic(normFactor);

                            if (rebuildTic)
                            {
                                _storedTic.Add(tic);
                            }
                            else
                            {
                                _storedTic.Add(point.TIC);
                            }

                            _store.Spectrums.Add(ss);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                HasLoadErrors = true;
                if (statusReport != null)
                    statusReport.AppStatus
                        = string.Format("Warning: only {0} of {1} spectrums have been loaded."
                        , _store.Spectrums.Count
                        , _chromPoints.Count);

                if (_chromPoints.Count > _store.Spectrums.Count)
                    _chromPoints.RemoveRange(_store.Spectrums.Count, _chromPoints.Count - _store.Spectrums.Count);

                log.Error(ex);
            }
        }

        public SpectrumData GetSpectrumData(int id)
        {
            using (var uof = _context.CreateUnitOfWork())
            {
                var result = uof.Find<SpectrumData>(Entity.Attribute("SpectrumId") == id);
                var retval = result.FirstOrDefault<SpectrumData>();
                return retval;
            }
        }


        public void RebuildTic()
        {
            if (_storedTic == null)
                return;

            if (_storedTic.Count != ChromPoints.Count)
                return;

            for (int i = 0; i < _storedTic.Count; i++)
                ChromPoints[i].TIC = _storedTic[i];
        }

        private MsSpectrum GetSpectrum(int i)
        {
            if (_store == null || _store.Spectrums.Count <= i || _resampler.XVals == null)
                return null;

            var ss = _store.Spectrums[i];

            return new MsSpectrum(new SimpleDataSource(_resampler.XVals, ss.Data), _chromPoints[i]) { BaselineRemoved = BaselineRemoved, PPD = _ppd, TIC = _storedTic[i] };

        }

        private double[] GetResapmledIntensities(SpectrumData data, int size, double[] masses, int toSkip = 0)
        {
            using (var stream = new MemoryStream(data.Data))
            {
                using (var reader = new BinaryReader(stream))
                {
                    var retval = new double[masses.Length];

                    for (var i = 0; i < toSkip; i++)
                    {
                        reader.ReadDouble();
                    }

                    for (var i = 0; i < size - toSkip; i++)
                    {
                        if (i < masses.Length)
                            retval[i] = reader.ReadDouble();
                    }

                    //var min = retval.Min();

                    //for (var i = 0; i < masses.Length; i++)
                    //{
                    //    retval[i] -= min;
                    //}

                    return _resampler.GetYVals(new SimpleDataSource(masses, retval));
                }
            }


        }

        private int[] GetResapmledIntensitiesAsInt(SpectrumData data, int size, double[] masses, int toSkip = 0)
        {
            using (var stream = new MemoryStream(data.Data))
            {
                using (var reader = new BinaryReader(stream))
                {
                    var tmp = new double[masses.Length];

                    for (var i = 0; i < toSkip; i++)
                    {
                        reader.ReadInt32();
                    }

                    for (var i = 0; i < size - toSkip; i++)
                    {
                        if (i < masses.Length)
                            tmp[i] = reader.ReadInt32();
                    }


                    return _resampler.GetYValsAsInts(new SimpleDataSource(masses, tmp));
                }
            }


        }

        //public ChromFilterSettings GetFilterSettings()
        //{
        //    if (!IsModasFile)
        //        return new ChromFilterSettings();

        //    var retval = new ChromFilterSettings();

        //    retval.Methods = new ObservableCollection<MethodFileLite>(GetLiteMethods());
        //    retval.SelectedMethod = retval.Methods.FirstOrDefault();


        //    retval.InjectionTimes = new ObservableCollection<double>(GetInjectionTimes());
        //    retval.SelectedInjectionTime = retval.InjectionTimes.FirstOrDefault();

        //    return retval;
        //}

        public ChromFilterSettings GetFilterSettings()
        {
            if (!IsModasFile)
                return new ChromFilterSettings();

            var retval = new ChromFilterSettings();

            retval.Methods = new ObservableCollection<MethodFileLite>(GetLiteMethods());
            retval.SelectedMethod = retval.Methods.FirstOrDefault();


            retval.InjectionTimes = new ObservableCollection<double>(GetInjectionTimes());
            retval.SelectedInjectionTime = retval.InjectionTimes.FirstOrDefault();

            retval.MassRanges = new ObservableCollection<MassRangeOption>(GetMassRanges());
            retval.SelectedMassRange = retval.MassRanges.FirstOrDefault();

            return retval;
        }

        // Distinct scan mass ranges (Method.MFrom-MTo) actually present in this file — e.g. a
        // file with parent masses 609/609.1 sharing range 270-620 and 455/455.1 sharing
        // 270-470 returns 2 MassRangeOption entries, not 4. Built from the already-loaded
        // _spectrumRecords/_methodCache rather than a fresh query, since LoadChromPoints has
        // already populated both by the time this is called.
        public IList<MassRangeOption> GetMassRanges()
        {
            GetLiteMethods();

            var options = new List<MassRangeOption>();

            foreach (var record in _spectrumRecords)
            {
                if (!record.MethodId.HasValue)
                    continue;

                var method = _methodCache.GetMethod(record.MethodId.Value);
                if (method == null)
                    continue;

                var option = options.FirstOrDefault(o => o.MFrom.Equals(method.MFrom) && o.MTo.Equals(method.MTo));

                if (option == null)
                {
                    option = new MassRangeOption { MFrom = method.MFrom, MTo = method.MTo };
                    options.Add(option);
                }

                if (!option.MethodIds.Contains(method.ID))
                    option.MethodIds.Add(method.ID);

                if (record.ParentIonMass > 0 && !option.ParentMasses.Contains(record.ParentIonMass))
                    option.ParentMasses.Add(record.ParentIonMass);
            }

            return options.OrderBy(o => o.MFrom).ThenBy(o => o.MTo).ToList();
        }

        // Every distinct compound (Parent Ion, rounded to the nearest WHOLE number) in the WHOLE
        // file, queried directly from the database — independent of whatever filter was used for
        // the current load. This is what lets the Data Channel dropdown offer a compound that
        // wasn't actually loaded (e.g. the file was opened with an Import "Mass Range" filter
        // that only pulled in some of them). Rounding to a whole number deliberately MERGES
        // fragmentation variants like 402/402.1/402.2 into one "402" channel — confirmed as the
        // wanted behavior, overriding the previous 2-decimal-place grouping that kept them split.
        public IList<CompoundInfo> GetAllCompoundInfos()
        {
            GetLiteMethods();

            var options = new List<CompoundInfo>();

            using (var uof = Context.CreateUnitOfWork())
            {
                IList<Spectrum> allRecords = uof.Find<Spectrum>();

                foreach (var record in allRecords)
                {
                    if (record.ParentIonMass <= 0 || !record.MethodId.HasValue)
                        continue;

                    var method = _methodCache.GetMethod(record.MethodId.Value);
                    if (method == null)
                        continue;

                    var key = Math.Round(record.ParentIonMass, 0);

                    var info = options.FirstOrDefault(o => o.ParentIon.Equals(key));

                    if (info == null)
                    {
                        info = new CompoundInfo { ParentIon = key, MFrom = method.MFrom, MTo = method.MTo };
                        options.Add(info);
                    }
                    else
                    {
                        // Different exact parent ions merged into this same whole-number channel
                        // can come from different methods with slightly different scan ranges —
                        // widen MFrom/MTo to cover all of them, not just whichever was seen first.
                        if (method.MFrom < info.MFrom) info.MFrom = method.MFrom;
                        if (method.MTo > info.MTo) info.MTo = method.MTo;
                    }

                    if (!info.MethodIds.Contains(method.ID))
                        info.MethodIds.Add(method.ID);
                }
            }

            return options.OrderBy(o => o.ParentIon).ToList();
        }

        // Builds a query matching every scan belonging to any of this compound's MethodIds — the
        // same "OR across MethodIds" approach ChromFilterSettings.BuildMassRangeExpression uses,
        // so a reload for a single compound still includes all its fragmentation variants
        // (e.g. 609 and 609.1 both scanning the same range).
        public QueryExpression BuildCompoundFilter(CompoundInfo compound)
        {
            if (compound == null || compound.MethodIds.Count == 0)
                return null;

            QueryExpression expr = null;

            foreach (var methodId in compound.MethodIds)
            {
                var clause = Entity.Attribute("MethodId") == methodId;
                expr = expr == null ? clause : expr || clause;
            }

            return expr;
        }

        public IList<double> GetInjectionTimes()
        {
            var query = new Query(typeof(Spectrum));
            query.Projection.Add("InjectionTime");
            query.Distinct = true;

            using (var uof = Context.CreateUnitOfWork())
            {
                return uof.Project<double>(query);
            }
        }

        public IList<double> GetParentIonMasses()
        {
            var query = new Query(typeof(Spectrum));
            query.Projection.Add("ParentIonMass");
            query.Distinct = true;

            using (var uof = Context.CreateUnitOfWork())
            {
                return uof.Project<double>(query);
            }
        }

    }
}
