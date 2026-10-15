using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Reflection;
using ChromExplorer.ModasReader.Properties;
using log4net;
using Mindscape.LightSpeed;

namespace ChromExplorer.ModasReader
{
    public class VersionUpdater
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static List<string> MandatoryTableNames;

        private readonly LightSpeedContext<ModasDataModelUnitOfWork> _context;

        static VersionUpdater()
        {
            CurrentVersion = Assembly.GetExecutingAssembly().GetName().Version;

            MandatoryTableNames = new List<string>() { "Calibration", "Calibrations", "KeyTable", "Method", "Spectrum", "SpectrumData", "SpetrumCalibrations" };
        }

        public VersionUpdater(LightSpeedContext<ModasDataModelUnitOfWork> context)
        {
            _context = context;
        }

        public bool IsModasFile
        {
            get
            {
                try
                {
                    var tables = GetTables();

                    foreach (var mandatoryTableName in MandatoryTableNames)
                    {
                        if (!tables.Contains(mandatoryTableName))
                            return false;
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    log.WarnFormat("IsModasFile: Error detected: {0}", ex);
                }

                return false;
            }
        }

        public bool NeedUpdate
        {
            get
            {
                try
                {
                    if (!IsModasFile)
                        return false;
                    
                    using (var uof = _context.CreateUnitOfWork())
                    {
                        var versions = uof.Find<ModasVersion>();

                        if (versions.Count > 0)
                        {
                            var v = versions[0];

                            Version sv;
                            if (Version.TryParse(v.Name, out sv))
                            {
                                if (CurrentVersion.CompareTo(sv) == 1)
                                    return true;
                                else
                                {
                                    return false;
                                }
                            }

                            return true;
                        }
                    }

                }
                catch (Exception ex)
                {

                }

                return true;
            }
        }

        public static Version CurrentVersion { get; private set; }

        public void WriteVersion()
        {
            using (var uof = _context.CreateUnitOfWork())
            {
                var v = new ModasVersion
                {
                    Name = CurrentVersion.ToString(),
                    Major = CurrentVersion.Major,
                    Minor = CurrentVersion.Minor,
                    Revision = CurrentVersion.Build
                };


                uof.Add(v);

                var tran = uof.BeginTransaction();
                uof.SaveChanges();
                tran.Commit();
            }
        }

        public void UpdateFromPreviousVersions()
        {
            ExecCustomNonQuery("ALTER TABLE SPECTRUM ADD COLUMN MsPower INT NOT NULL DEFAULT 0");
            ExecCustomNonQuery("ALTER TABLE SPECTRUM ADD COLUMN ParentIonMass FLOAT NOT NULL DEFAULT 0");
            ExecCustomNonQuery("ALTER TABLE SPECTRUM ADD COLUMN InjectionTime FLOAT NOT NULL DEFAULT 0");
            ExecCustomNonQuery("ALTER TABLE SPECTRUM ADD COLUMN CalibrationsId INT DEFAULT NULL");
            ExecCustomNonQuery("ALTER TABLE CALIBRATIONS ADD COLUMN RawData BLOB");
            ExecCustomNonQuery("DROP TABLE [ModasVersion]");
            ExecCustomNonQuery("CREATE TABLE [ModasVersion] (Id INT NOT NULL PRIMARY KEY, [Name] TEXT NULL, [Major] INT NULL, [Minor] INT NULL, [Revision] INT NULL);");
            WriteVersion();
        }

        public void CreateMissedIndexes()
        {
            ExecCustomNonQuery(Resources.CreateSpectrumDataSpectrumIDIndex);
            ExecCustomNonQuery(Resources.CreateSpectrumCalibrationsCalibrationIndex);
            ExecCustomNonQuery(Resources.CreateSpectrumCalibrationsSpectrumIDIndex);
        }

        private void ExecCustomNonQuery(string query)
        {
            try
            {

                using (var uof = _context.CreateUnitOfWork())
                {
                    var command = new SQLiteCommand(query) { CommandType = CommandType.Text };
                    var dbCommand = uof.PrepareCommand(command);
                    dbCommand.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                log.Info(ex);
            }
        }

        private IDataReader ExecCustomQuery(string query)
        {
            try
            {

                using (var uof = _context.CreateUnitOfWork())
                {
                    var command = new SQLiteCommand(query) { CommandType = CommandType.Text };
                    var dbCommand = uof.PrepareCommand(command);
                    return dbCommand.ExecuteReader();
                }
            }
            catch (Exception ex)
            {
                log.Info(ex);
            }

            return null;
        }

        public string[] GetTables()
        {
            try
            {

                using (var uof = _context.CreateUnitOfWork())
                {
                    var command = new SQLiteCommand(Resources.SelectTablesQuery) { CommandType = CommandType.Text };
                    var dbCommand = uof.PrepareCommand(command);
                    var reader = dbCommand.ExecuteReader();

                    if (reader != null)
                    {
                        var tables = new List<string>();
                        while (reader.Read())
                        {
                            if (reader.FieldCount > 0)
                            {
                                tables.Add(reader.GetString(0));
                            }
                        }

                        return tables.ToArray();
                    }

                }
            }
            catch (Exception ex)
            {
                log.Info(ex);
            }

            return null;
        }
    }

}
