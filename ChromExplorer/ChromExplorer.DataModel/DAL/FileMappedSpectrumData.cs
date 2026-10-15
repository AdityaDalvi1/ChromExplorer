using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using ChromExplorer.DataModel.Chromatogram;
using ChromExplorer.DataModel.Core;
using ChromExplorer.DataModel.Interfaces;
using ChromExplorer.DataModel.PostProcessing;
using ChromExplorer.DataModel.Spectrum;
using log4net;

namespace ChromExplorer.DataModel.DAL
{
    //[Log(EntryLevel = LogLevel.Debug, ExitLevel = LogLevel.Debug, ExceptionLevel = LogLevel.Error)]
    public class FileMappedSpectrumData
        : IDisposable, ISpectrumCollection, IChromRebuilder
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private List<ChromatogramPoint> _chromatogram;
        private bool _inMemory;
        private IntensityStore _store;
        private readonly int _dataSize;
        private readonly AGCSpectrumEstimator _estimator = new AGCSpectrumEstimator();
        private readonly FileInfo _file;
        private readonly long _headerOffset;
        private readonly double[] _positions;
        private readonly BinaryReader _reader;

        public FileMappedSpectrumData(string filename)
        {
            _dataSize = Marshal.SizeOf(typeof (int));


            _file = new FileInfo(filename);
            if (!_file.Exists)
                throw new FileNotFoundException(filename);

            _reader = new BinaryReader(_file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite));

            Info = new Header(_reader);
            _headerOffset = _reader.BaseStream.Position;

            if(!Info.IsValid)
                throw new FileFormatException("Bad file header");

            _positions = new double[Info.Points];

            for (var i = 0; i < Info.Points; i++)
            {
                _positions[i] = Info.MFrom + i/Info.PPD;
            }

            _inMemory = false;
        }

        public FileInfo File
        {
            get { return _file; }
        }

        public Header Info { get; private set; }

        #region Implementation of IChromRebuilder

        public SimpleDataSource Rebuild(IList<int> indexes, IList<ChromatogramPoint> points)
        {
            if (points == null)
                return null;

            var size = points.Count();
            var x = new double[size];
            var y = new double[size];

            var i = 0;

            foreach (var point in points)
            {
                x[i] = point.Time;
                y[i] = 0.0;


                var devider = point.SpectrumMeta.InjectionTime > 0 ? point.SpectrumMeta.InjectionTime : 1.0;

                if (indexes != null)
                {
                    foreach (var index in indexes)
                    {
                        if (_inMemory)
                        {
                            y[i] += _store.Spectrums[i].Data[index]/devider;
                        }
                        else
                        {
                            y[i] += GetValue(i, index)/devider;
                        }
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

        // TODO [Log(EntryLevel = LogLevel.Debug, ExitLevel = LogLevel.Debug, ExceptionLevel = LogLevel.Error)]
        public void LoadData(IStatusUpdateService statusService = null)
        {
            var step = 0.0;

            if (statusService != null)
                step = (statusService.ProgressMax - statusService.ProgressMin)/Info.Spectrums;

            _store = new IntensityStore(Info.Spectrums);
            for (var i = 0; i < Info.Spectrums; i++)
            {
                if (statusService != null)
                {
                    statusService.ReportProgress(i*step);
                }

                SeekPosition(i, 0);
                var spectum = new SpectrumStore(_reader, Info.Points);
                _store.Spectrums.Add(spectum);
            }

            _inMemory = true;
        }

        public void RebuildTic()
        {
            if (_chromatogram == null || !_inMemory)
                return;
            var i = 0;
            foreach (var point in _chromatogram)
            {
                var divider = point.SpectrumMeta.InjectionTime > 0 ? point.SpectrumMeta.InjectionTime : 1.0;
                var tic = 0.0;
                for (var j = 0; j < Info.Points; j++)
                {
                    tic += _estimator.IntegrateUsefulSignalVersion2(_store.Spectrums[i].Data, 1)/divider;
                    //tic += _store.Spectrums[i].Data[j] / divider;
                }

                point.TIC = tic;

                i++;
            }
        }

        public int GetValue(int spectrum, int point)
        {
            if (_inMemory)
            {
                return _store.GetValue(spectrum, point);
            }
            SeekPosition(spectrum, point);
            return _reader.ReadInt32();
        }

        public MsSpectrum GetSpectrum(int spectrum)
        {
            if (_chromatogram == null)
                ReadChromatogram();

            SeekSpectrumMetaDataPosition(spectrum);

            SeekPosition(spectrum, 0);

            var data = new double[Info.Points];
            for (var i = 0; i < Info.Points; i++)
            {
                data[i] = _reader.ReadInt32();
            }

            return
                _chromatogram != null
                    ? new MsSpectrum(new SimpleDataSource(_positions, data), _chromatogram[spectrum])
                    : new MsSpectrum(new SimpleDataSource(_positions, data), spectrum);
        }

        public IList<ChromatogramPoint> ReadChromatogram(string timeUnits = "ms", double fromMS = 1.0)
        {
            _chromatogram = new List<ChromatogramPoint>();

            for (var i = 0; i < Info.Spectrums; i++)
            {
                SeekSpectrumMetaDataPosition(i);
                var meta = new SpectrumMetaData(_reader);

                var point = new ChromatogramPoint
                {
                    Index = i,
                    SpectrumID = i,
                    TIC = meta.TIC,
                    Time = meta.EllapsedMilliseconds*fromMS,
                    TimeUnits = timeUnits,
                    SpectrumMeta = meta
                };

                _chromatogram.Add(point);
            }

            return _chromatogram;
        }

        private void SeekPosition(int spectrum, int point)
        {
            var pos = _headerOffset + (spectrum + 1)*SpectrumMetaData.SizeOf + (spectrum*Info.Points + point)*_dataSize;

            if (pos != _reader.BaseStream.Position)
            {
                _reader.BaseStream.Seek(pos, SeekOrigin.Begin);
            }
        }

        private void SeekSpectrumMetaDataPosition(int spectrum)
        {
            var pos = _headerOffset + (spectrum)*SpectrumMetaData.SizeOf + (spectrum*Info.Points)*_dataSize;

            if (pos != _reader.BaseStream.Position)
            {
                _reader.BaseStream.Seek(pos, SeekOrigin.Begin);
            }
        }

        public class Header
        {
            public const int Magic = 4256632;

            public Header()
            {
                MagicNumber = Magic;
            }

            public Header(BinaryReader reader)
            {
                Read(reader);
            }

            public int MagicNumber { get; set; }
            public int Spectrums { get; set; }
            public int Points { get; set; }
            public double PPD { get; set; }
            public double MFrom { get; set; }

            public bool IsValid
            {
                get { return Magic == MagicNumber; }
            }

            public void Read(BinaryReader reader)
            {
                MagicNumber = reader.ReadInt32();
                Spectrums = reader.ReadInt32();
                Points = reader.ReadInt32();
                PPD = reader.ReadDouble();
                MFrom = reader.ReadDouble();
            }

            public void Write(BinaryWriter writer)
            {
                writer.Write(MagicNumber);
                writer.Write(Spectrums);
                writer.Write(Points);
                writer.Write(PPD);
                writer.Write(MFrom);
            }
        }

        public class SpectrumMetaData
        {
            public static int SizeOf = 36;

            public SpectrumMetaData()
            {
                MsPower = 0;
                EllapsedMilliseconds = 0;
                ParentIon = 0;
                InjectionTime = 0;
                TIC = 0.0;
            }

            public SpectrumMetaData(SpectrumMetaData data)
            {
                MsPower = data.MsPower;
                EllapsedMilliseconds = data.EllapsedMilliseconds;
                ParentIon = data.ParentIon;
                InjectionTime = data.InjectionTime;
                TIC = data.TIC;
            }

            public SpectrumMetaData(BinaryReader reader)
            {
                Read(reader);
            }

            public int MsPower { get; set; }
            public long EllapsedMilliseconds { get; set; }
            public double ParentIon { get; set; }
            public double InjectionTime { get; set; }
            public double TIC { get; set; }

            public void Write(BinaryWriter writer)
            {
                writer.Write(EllapsedMilliseconds);
                writer.Write(InjectionTime);
                writer.Write(MsPower);
                writer.Write(ParentIon);
                writer.Write(TIC);
            }

            public void Read(BinaryReader reader)
            {
                EllapsedMilliseconds = reader.ReadInt64();
                InjectionTime = reader.ReadDouble();
                MsPower = reader.ReadInt32();
                ParentIon = reader.ReadDouble();
                TIC = reader.ReadDouble();
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~FileMappedSpectrumData()
        {
            Dispose(false);
        }


        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_store != null)
                    _store.Dispose();
                if (_reader != null)
                    _reader.Close();
            }

            //free unmanaged resources
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
            get { return Info.Spectrums; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public IEnumerator<MsSpectrum> GetEnumerator()
        {
            for (var i = 0; i < Info.Spectrums; i++)
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
            get { return Info.Points; }
        }

        public IEnumerable<double> Positions
        {
            get { return _positions; }
        }

        public MsSpectrum GetAggregateSpectrum(IEnumerable<int> indexes)
        {
            if (indexes == null)
                return null;

            var aggregated = new double[SpectrumSize];

            if (_inMemory)
            {
                foreach (var index in indexes)
                {
                    if (index >= 0 && index < Info.Spectrums)
                    {
                        var spectrum = _store.Spectrums[index];
                        for (var i = 0; i < SpectrumSize; i++)
                        {
                            aggregated[i] += spectrum.Data[i];
                        }
                    }
                }

                return new MsSpectrum(new SimpleDataSource(Positions.ToArray(), aggregated));
            }


            foreach (var index in indexes)
            {
                if (index >= 0 && index < Count)
                {
                    var spectrum = this[index].Points;
                    for (var i = 0; i < SpectrumSize; i++)
                    {
                        aggregated[i] += spectrum.Yvals[i];
                    }
                }
            }

            return new MsSpectrum(new SimpleDataSource(Positions.ToArray(), aggregated));
        }

        #endregion
    }
}