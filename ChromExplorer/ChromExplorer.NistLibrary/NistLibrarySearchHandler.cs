using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using ChromExplorer.DataModel.Formats;
using log4net;

namespace ChromExplorer.NistLibrary
{
    public class NistLibrarySearchHandler
    {
        // ReSharper disable once InconsistentNaming
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private bool _installed = false;

        public bool IsInstalled => _installed;

        public bool WasAutoDetected { get; private set; }

        public NistLibrarySearchHandler()
        {
            CheckInstalled();
            CreateDirs();
        }

        private void CheckInstalled()
        {
            var configuredPath = NistLibrarySettings.Default.NistExecutable;
            var fi = new FileInfo(configuredPath);
            _installed = fi.Exists;

            if (_installed)
            {
                log.Info($"NIST executable found at configured path '{configuredPath}'.");
            }
            else
            {
                log.Info($"NIST executable not found at configured path '{configuredPath}'. Searching connected drives for a NIST installation...");
                _installed = TryAutoDetect();
            }

            if (_installed)
                AlignLocatorPathsToInstall();
        }

        // FirstLocatorFile / SecondLocatorFile / WorkingDir are separate per-machine settings
        // that are NOT derived from NistExecutable, so they can silently go stale: NIST itself
        // only ever reads its own AUTOIMP.MSD inside its OWN install folder. If the executable
        // lives somewhere other than where these three settings point (e.g. a client PC with
        // NIST installed on a different drive/folder than the dev machine's default
        // c:\NIST17\...), nistms.exe still launches fine (StartSearch only needs the exe path)
        // but the exported spectrum's locator files land in a folder the running NIST process
        // never looks at, so the file silently never loads. Re-root all three under the actual
        // install directory so they always match wherever NIST really is on this machine.
        private void AlignLocatorPathsToInstall()
        {
            try
            {
                var exePath = NistLibrarySettings.Default.NistExecutable;
                var msSearchDir = Path.GetDirectoryName(exePath); // .../NISTxx/MSSEARCH
                var installRoot = Directory.GetParent(msSearchDir)?.FullName; // .../NISTxx
                if (string.IsNullOrEmpty(installRoot))
                    return;

                var expectedFirstLocator = Path.Combine(installRoot, "MSSEARCH", "AUTOIMP.MSD");
                var expectedSecondLocator = Path.Combine(installRoot, "MTSEARCH", "FILESPEC.FIL");
                var expectedWorkingDir = Path.Combine(installRoot, "MTSEARCH", "DATA");

                bool changed = false;

                if (!string.Equals(NistLibrarySettings.Default.FirstLocatorFile, expectedFirstLocator, StringComparison.OrdinalIgnoreCase))
                {
                    NistLibrarySettings.Default.FirstLocatorFile = expectedFirstLocator;
                    changed = true;
                }
                if (!string.Equals(NistLibrarySettings.Default.SecondLocatorFile, expectedSecondLocator, StringComparison.OrdinalIgnoreCase))
                {
                    NistLibrarySettings.Default.SecondLocatorFile = expectedSecondLocator;
                    changed = true;
                }
                if (!string.Equals(NistLibrarySettings.Default.WorkingDir, expectedWorkingDir, StringComparison.OrdinalIgnoreCase))
                {
                    NistLibrarySettings.Default.WorkingDir = expectedWorkingDir;
                    changed = true;
                }

                if (changed)
                {
                    log.Info($"Re-rooted NIST locator paths under install directory '{installRoot}': FirstLocatorFile='{expectedFirstLocator}', SecondLocatorFile='{expectedSecondLocator}', WorkingDir='{expectedWorkingDir}'.");
                    NistLibrarySettings.Default.Save();
                }
            }
            catch (Exception ex)
            {
                log.Warn("AlignLocatorPathsToInstall failed with exception: ", ex);
            }
        }

        // Scans every ready drive's root for a "NIST*" folder containing MSSEARCH\nistms.exe.
        // Different machines install the NIST library on different drives (C:, D:, ...), so the
        // configured/default path can't be relied on until the user has set it once via Settings.
        private bool TryAutoDetect()
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (!drive.IsReady)
                    continue;

                log.Info($"Searching drive '{drive.Name}' for a NIST installation...");

                DirectoryInfo[] candidateDirs;
                try
                {
                    candidateDirs = drive.RootDirectory.GetDirectories("NIST*", SearchOption.TopDirectoryOnly);
                }
                catch (Exception ex)
                {
                    log.Warn($"Could not search drive '{drive.Name}'.", ex);
                    continue;
                }

                foreach (var dir in candidateDirs)
                {
                    var candidatePath = Path.Combine(dir.FullName, "MSSEARCH", "nistms.exe");

                    if (!File.Exists(candidatePath))
                        continue;

                    log.Info($"Found NIST installation at '{candidatePath}'. Saving as the configured NIST executable path.");

                    NistLibrarySettings.Default.NistExecutable = candidatePath;
                    NistLibrarySettings.Default.Save();

                    WasAutoDetected = true;
                    return true;
                }
            }

            log.Warn("NIST installation was not found on any connected drive.");
            return false;
        }


        public void InteractiveSearch(NistTextSpectrum s, bool append = true)
        {
            if (!_installed)
            {
                log.Warn($"NIST executable not found at '{NistLibrarySettings.Default.NistExecutable}'. Configure the path in Settings > Nist Library.");
                return;
            }

            log.Info($"Starting NIST interactive search (append={append}) using '{NistLibrarySettings.Default.NistExecutable}'.");
            CreateSearchFiles(s, append);
            StartSearch();
        }

        public void StartSearch()
        {
            try
            {
                var process = new Process
                {
                    StartInfo =
                    {
                        FileName = NistLibrarySettings.Default.NistExecutable,
                        Arguments = "/instrument",
                        WindowStyle = ProcessWindowStyle.Normal
                    }
                };
                // Configure the process using the StartInfo properties.
                process.Start();
                log.Info($"NIST process started: '{NistLibrarySettings.Default.NistExecutable}'.");
            }
            catch (Exception ex)
            {
                log.Warn("Start process failed", ex);

            }

            //return process;
        }

        public void CreateSearchFiles(NistTextSpectrum s, bool append)
        {
            try
            {
                var filename = CreateFileName(s);
                var datafilePath = Path.Combine(NistLibrarySettings.Default.WorkingDir, filename);

                WriteSpectrum(datafilePath, s);

                WriteSecondFileLocator(datafilePath, append);
                WriteFirstFileLocator();
            }
            catch (Exception ex)
            {

                log.Warn("CreateSearchFiles failed with exception: ", ex);
            }
        }

        private void WriteFirstFileLocator()
        {
            using (TextWriter writer = new StreamWriter(NistLibrarySettings.Default.FirstLocatorFile))
            {
                writer.WriteLine(NistLibrarySettings.Default.SecondLocatorFile);
            }
        }

        private void WriteSecondFileLocator(string datafilePath, bool append)
        {
            string par = append ? "APPEND" : "OVERWRITE"; 
            using (TextWriter writer = new StreamWriter(NistLibrarySettings.Default.SecondLocatorFile))
            {
                writer.WriteLine($"{datafilePath} {par}");
            }
        }

        private void WriteSpectrum(string filename, NistTextSpectrum s)
        {
            using (TextWriter writer = new StreamWriter(filename))
            {
                s.Write(writer);
            }
        }

        private void CreateDirs()
        {
            if(!_installed)
                return;

            try
            {
                CreateDir(GetDirName(NistLibrarySettings.Default.FirstLocatorFile));
                CreateDir(GetDirName(NistLibrarySettings.Default.SecondLocatorFile));
                CreateDir(NistLibrarySettings.Default.WorkingDir);
            }
            catch (Exception ex)
            {
                
                log.Warn("CreateDirs failed with exception: ", ex);
            }

        }


        private void CreateDir(string dirName)
        {
           var di = new DirectoryInfo(dirName);

            if (!di.Exists)
            {
                Directory.CreateDirectory(dirName);
            }
        }

        private string GetDirName(string filename)
        {
            var fi = new FileInfo(filename);
            return fi.DirectoryName;
        }

        private string CreateFileName(NistTextSpectrum s)
        {
            var now = DateTime.Now;
            //var nowStr = now.ToString("_yyyy-MM-dd-HHmmssff");
            var nowStr = "";
            if (string.IsNullOrEmpty(s.ChromFilename))
            {
                return $"mt_search{nowStr}.dat";
            }

            var fi = new FileInfo(s.ChromFilename);
            var name = Path.GetFileNameWithoutExtension(fi.Name);

            return $"{name}{nowStr}.dat";
        }
    }
}