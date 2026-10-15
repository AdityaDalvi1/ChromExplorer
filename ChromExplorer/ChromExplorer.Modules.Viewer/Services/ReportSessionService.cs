using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace ChromExplorer.Modules.Viewer.Services
{
    /// <summary>One file's snapshot queued into the multi-file report, plus the bookkeeping
    /// needed to show it in the "files added to report" preview list.</summary>
    public class ReportEntry
    {
        public ChromatogramReportData Data { get; set; }
        public DateTime AddedAt { get; set; }
    }

    /// <summary>
    /// Shared queue of per-file report snapshots. A file's processed state lives only on
    /// ChromatogramViewViewModel and is torn down the moment the next file is loaded
    /// (UpdateChrom() replaces Chromatogram/ChromVM/SpectrumVM in place), so "Add to Report"
    /// must capture a fully-detached ChromatogramReportData (see BuildReportData()) at click
    /// time and hold onto it here — following the same shared-MEF-part bridge pattern as
    /// ForensicReportContext — so it survives across however many files get loaded afterwards.
    ///
    /// The queue is also persisted to disk on every change, because being a MEF-shared
    /// singleton only keeps it alive for the current process — closing the app, logging out of
    /// it, or a crash would otherwise silently drop everything the user queued. Entries stay on
    /// disk until the user removes them (RemoveAt/Clear) or a report is generated from them;
    /// nothing else should ever call Clear().
    /// </summary>
    [Export]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class ReportSessionService
    {
        private static readonly string StorageDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ChromExplorer");

        private static readonly string StorageFile = Path.Combine(StorageDir, "pending_report_queue.json");

        private readonly List<ReportEntry> _entries;

        public ReportSessionService()
        {
            _entries = Load();
        }

        public IReadOnlyList<ReportEntry> Entries => _entries;

        public int Count => _entries.Count;

        public void Add(ChromatogramReportData data)
        {
            _entries.Add(new ReportEntry { Data = data, AddedAt = DateTime.Now });
            Save();
        }

        public void RemoveAt(int index)
        {
            if (index >= 0 && index < _entries.Count)
            {
                _entries.RemoveAt(index);
                Save();
            }
        }

        /// <summary>Removes a specific entry by reference — used by ReportPreviewWindow's
        /// per-file ✕ button, where the caller has the entry itself rather than its index.</summary>
        public void Remove(ReportEntry entry)
        {
            if (_entries.Remove(entry))
                Save();
        }

        public void Clear()
        {
            _entries.Clear();
            Save();
        }

        // ── Persistence ─────────────────────────────────────────────────────────
        // Best-effort: a failure here should never crash the app or block reporting — worst
        // case the queue just doesn't survive a restart this one time.

        private static List<ReportEntry> Load()
        {
            try
            {
                if (!File.Exists(StorageFile))
                    return new List<ReportEntry>();

                using (var stream = File.OpenRead(StorageFile))
                {
                    var serializer = new DataContractJsonSerializer(typeof(List<ReportEntry>));
                    return (List<ReportEntry>)serializer.ReadObject(stream) ?? new List<ReportEntry>();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ReportSessionService: failed to load persisted report queue — starting empty. " + ex.Message);
                return new List<ReportEntry>();
            }
        }

        private void Save()
        {
            try
            {
                Directory.CreateDirectory(StorageDir);

                // Write to a temp file first so a crash/power-loss mid-write can't leave behind
                // a truncated, unreadable queue file.
                string tempFile = StorageFile + ".tmp";

                using (var stream = new FileStream(tempFile, FileMode.Create, FileAccess.Write))
                {
                    var serializer = new DataContractJsonSerializer(typeof(List<ReportEntry>));
                    serializer.WriteObject(stream, _entries);
                }

                File.Copy(tempFile, StorageFile, overwrite: true);
                File.Delete(tempFile);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ReportSessionService: failed to persist report queue. " + ex.Message);
            }
        }
    }
}
