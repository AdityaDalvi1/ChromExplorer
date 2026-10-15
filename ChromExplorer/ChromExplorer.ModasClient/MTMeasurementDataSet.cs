using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Modas.MTRemote.Data;

namespace ChromExplorer.ModasClient
{
    /// <summary>
    /// Represents a collection of measurements and the corresponding profile data / measurement type.
    /// </summary>
    [Serializable]
    public class MTMeasurementDataSet
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public MTMeasurementDataSet()
        {
            TimeStamp = DateTime.Now;
            Type = MTMeasurementDataSetType.MSScan;
            Header = new MTMeasurementDataSetHeader();
            Measurements = new List<MTMeasurement>();
        }

        public MTMeasurementDataSet(MTMeasurementDataSetType type)
        {
            TimeStamp = DateTime.Now;
            Type = type;
            Header = new MTMeasurementDataSetHeader();
            Measurements = new List<MTMeasurement>();
        }


       
        public DateTime TimeStamp { get; set; }
       
        public MTMeasurementDataSetType Type { get; set; }
       
        public MTMeasurementDataSetHeader Header { get; set; }
       
        public List<MTMeasurement> Measurements { get; set; }

        public static MTMeasurementDataSet Restore(MTDataContainer data)
        {
            if (data.Payload == null)
                return new MTMeasurementDataSet();

            if (data.Payload.Length == 0)
                return new MTMeasurementDataSet();

            data.Decompress();

            try
            {
                using (MemoryStream stream = new MemoryStream(data.Payload))
                {

                    BinaryFormatter f = new BinaryFormatter();
                    object o = f.Deserialize(stream);


                    MTMeasurementDataSet ds = (MTMeasurementDataSet)o;

                    return ds;
                }
            }
            catch (Exception ex)
            {
                log.Warn(ex);
            }

            return new MTMeasurementDataSet();
        }

        public MTDataContainer GetContainer()
        {
            MTDataContainer container = new MTDataContainer();
            container.FullTypeName = typeof(MTMeasurementDataSet).FullName;

            try
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    BinaryFormatter f = new BinaryFormatter();
                    f.Serialize(stream, this);
                    container.Payload = stream.ToArray();

                    if (MTRemoteGlobals.CompressData)
                        container.Compress();

                    container.UpdateHash();
                }

            }
            catch (Exception e)
            {
                log.Warn(e);

            }

            return container;
        }
    }

    public class TestMTMeasurementDataSetFactory
    {
        public static MTMeasurementDataSet Create(int size)
        {
            var set = new MTMeasurementDataSet();
            set.Header.LowMass = 0;
            set.Header.HighMass = size;

            for (int i = 0; i < size; i++)
            {
                set.Measurements.Add(new MTMeasurement(size, size * size));
            }

            return set;
        }
    }

   
}