using ChromExplorer.Modules.Quan.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace ChromExplorer.Modules.Quan.Services
{
    /// <summary>
    /// Holds the queue of peak-area values added via "Add to RSD" (one per chromatogram/file),
    /// grouped by the Concentration the user entered, and computes %RSD (Relative Standard
    /// Deviation) per concentration:
    ///
    ///     %RSD = (sample standard deviation of the Area values / mean of the Area values) * 100
    ///
    /// — the standard analytical-chemistry precision measure across replicate injections at the
    /// same concentration. %RSD is a property of the GROUP, not of any single file: it needs at
    /// least 2 files at the same concentration before it means anything. Sample ID plays no part
    /// in the grouping.
    ///
    /// In-memory only for the current session (same approach as QuanService's own _peaks list —
    /// no SQLite table, no persistence to disk), shared across the app via MEF so every
    /// "Add to RSD" click and the RSD tab both see the same live list.
    /// </summary>
    [Export(typeof(RsdService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class RsdService
    {
        private readonly List<RsdPeakInfo> _entries = new List<RsdPeakInfo>();
        private int _nextId = 1;

        [ImportingConstructor]
        public RsdService()
        {
        }

        /// <summary>Raised after Add/Remove/Clear so the RSD tab can refresh its grids.</summary>
        public event EventHandler EntriesChanged;

        public IEnumerable<RsdPeakInfo> Entries => _entries;

        public void Add(RsdPeakInfo info)
        {
            if (info == null) return;

            info.ID = _nextId++;
            info.InsertedDate = DateTime.Now.ToString("g");
            _entries.Add(info);

            OnEntriesChanged();
        }

        public void Remove(RsdPeakInfo info)
        {
            if (info != null && _entries.Remove(info))
                OnEntriesChanged();
        }

        /// <summary>Writes every queued entry to <paramref name="fileName"/> (XML). The
        /// concentration groups / %RSD are derived from the entries, so restoring the entries
        /// restores everything shown on the RSD tab. Throws on failure so the caller can report it.</summary>
        public void SaveData(string fileName)
        {
            using (var writer = new StreamWriter(fileName))
            {
                new XmlSerializer(typeof(List<RsdPeakInfo>)).Serialize(writer, _entries);
            }
        }

        /// <summary>Replaces the current queue with the entries stored in
        /// <paramref name="fileName"/>. Returns the number of entries loaded. Throws on an
        /// unreadable file, leaving the current queue untouched.</summary>
        public int LoadData(string fileName)
        {
            List<RsdPeakInfo> loaded;
            using (var stream = File.OpenRead(fileName))
            {
                loaded = new XmlSerializer(typeof(List<RsdPeakInfo>)).Deserialize(stream) as List<RsdPeakInfo>;
            }

            if (loaded == null)
                throw new InvalidDataException("The file does not contain an RSD list.");

            _entries.Clear();
            _entries.AddRange(loaded.Where(e => e != null));
            _nextId = _entries.Count == 0 ? 1 : _entries.Max(e => e.ID) + 1;
            OnEntriesChanged();
            return _entries.Count;
        }

        public void Clear()
        {
            _entries.Clear();
            _nextId = 1;
            OnEntriesChanged();
        }

        /// <summary>Groups the queued entries by Sample ID and computes %RSD for each group.
        /// Uses SAMPLE standard deviation (n-1, Bessel's correction) — the standard convention
        /// for %RSD in analytical-chemistry QC (USP/ICH-style precision checks), since a queued
        /// set of replicate injections is a sample of the method's variability, not the entire
        /// population of possible runs.</summary>
        public List<RsdGroupSummary> ComputeGroupSummaries()
        {
            var result = new List<RsdGroupSummary>();

            foreach (var group in _entries.GroupBy(e =>
                e.Concentration.HasValue ? (double?)Math.Round(e.Concentration.Value, 6) : null))
            {
                var areas = group.Select(g => g.Area).ToList();
                double mean = areas.Average();

                var summary = new RsdGroupSummary
                {
                    Concentration = group.Key,
                    Count = areas.Count,
                    MeanArea = mean,
                };

                if (areas.Count < 2)
                {
                    summary.Note = "Add at least 2 files at this concentration";
                }
                else if (mean == 0)
                {
                    summary.Note = "Mean area is zero — RSD undefined";
                }
                else
                {
                    double sumSquares = areas.Sum(a => (a - mean) * (a - mean));
                    double sd = Math.Sqrt(sumSquares / (areas.Count - 1));
                    summary.StdDev = sd;
                    summary.RsdPercent = (sd / mean) * 100.0;
                }

                result.Add(summary);
            }

            return result.OrderBy(s => s.Concentration.HasValue ? 0 : 1).ThenBy(s => s.Concentration).ToList();
        }

        private void OnEntriesChanged()
        {
            EntriesChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
