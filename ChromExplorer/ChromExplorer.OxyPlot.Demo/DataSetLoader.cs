using OxyPlot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChromExplorer.OxyPlot.Demo
{
    public class DataSetLoader
    {
        public static List<DataPoint> Load(string filename)
        {
            List<DataPoint> ret = new List<DataPoint>();
            using (var fileStream = File.OpenRead(filename))
            using (var streamReader = new StreamReader(fileStream))
            {
                String line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    var parts = line.Split(new char[] { ' ', ',', '\t'});

                    if(parts.Length == 2)
                    {
                        double x, y;

                        if(double.TryParse(parts[0], out x) &&
                        double.TryParse(parts[1], out y))
                        {
                            var point = new DataPoint(x, y);
                            ret.Add(point);
                        }
                    }
                }
            }

            return ret;
        }

    }
}
