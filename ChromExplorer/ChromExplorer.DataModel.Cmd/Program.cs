using System;
using System.Data.Common;
using System.Data.SQLite;
using System.Diagnostics;

namespace ChromExplorer.DataModel.Cmd
{
    class Program
    {
        private static string _createTableQuery =
@"CREATE TABLE [SpectrumData] (Id INT NOT NULL PRIMARY KEY ,
[SpectrumIndex] INT NOT NULL DEFAULT 0,
[PointIndex] INT NOT NULL DEFAULT 0,
[Mz] FLOAT NOT NULL DEFAULT 0,
[Intensity] FLOAT NOT NULL DEFAULT 0);";

        private static string _createIndexQuery =
            @"
CREATE UNIQUE INDEX [SpectrumData_PI_SI_idx] ON [SpectrumData](
[PointIndex]  ASC,
[SpectrumIndex]  ASC
);
CREATE INDEX [SpectrumData_SpectrumIndex_idx] ON [SpectrumData](
[SpectrumIndex]  DESC
);
CREATE INDEX [SpectrunData_PointIndex_idx] ON [SpectrumData](
[PointIndex]  DESC
);
";

        private static string _insertQuery =
            @"INSERT INTO [SpectrumData](Id, SpectrumIndex, PointIndex, Mz, Intensity) 
              VALUES(?,?,?,?,?)";

        private static int _spectrums = 1000;
        private static int _points = 50000;
        private static double _ppd = 25;
        private static double _mFrom = 200;

        static void Main(string[] args)
        {
            using (var conn = new SQLiteConnection("Data Source=indexed.db3"))
            {

                conn.Open();

                var mycommand = new SQLiteCommand(conn) { CommandText = _createTableQuery };

                var rowsUpdated = mycommand.ExecuteNonQuery();

                //_spectrums = 1;
                //_points = 10;

                var timer = Stopwatch.StartNew();

                FastInsertMany(conn);

                

                timer.Stop();

                Console.WriteLine("Insertion of {0} records took {1} ms", _points * _spectrums, timer.ElapsedMilliseconds);

                timer.Start();

                mycommand = new SQLiteCommand(conn) { CommandText = _createIndexQuery };

                mycommand.ExecuteNonQuery();

                conn.Close();
            }
        }

        internal static void FastInsertMany(DbConnection cnn)
        {
            int idValue = 1;

            for (int i = 0; i < _spectrums; i++)
            {
                Console.Write("{0}.", i);
                using (DbTransaction dbTrans = cnn.BeginTransaction())
                {

                    using (DbCommand cmd = cnn.CreateCommand())
                    {

                        cmd.CommandText = _insertQuery;

                        var id = cmd.CreateParameter();
                        var spectrumIndex = cmd.CreateParameter();
                        var pointIndex = cmd.CreateParameter();
                        var mz = cmd.CreateParameter();
                        var intesity = cmd.CreateParameter();

                        cmd.Parameters.Add(id);
                        cmd.Parameters.Add(spectrumIndex);
                        cmd.Parameters.Add(pointIndex);
                        cmd.Parameters.Add(mz);
                        cmd.Parameters.Add(intesity);




                        for (int j = 0; j < _points; j++)
                        {
                            id.Value = idValue++;
                            spectrumIndex.Value = i;
                            pointIndex.Value = j;

                            mz.Value = _mFrom + (double)j / _ppd;
                            intesity.Value = i + 1;

                            cmd.ExecuteNonQuery();
                        }

                    }

                    dbTrans.Commit();

                }
            }

        }
    }
}
