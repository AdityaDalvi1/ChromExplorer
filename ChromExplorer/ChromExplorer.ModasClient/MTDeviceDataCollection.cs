using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Modas.MTRemote.Data;

namespace ChromExplorer.ModasClient
{
    /// <summary>
    /// Collection of <see cref="MTDeviceData"/> objects
    /// </summary>
    [Serializable]
    [DataContract]
    public class MTDeviceDataCollection : List<MTDeviceData>
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public MTDeviceDataCollection()
        {

        }

        public MTDeviceDataCollection(List<Tuple<string, object>> list)
        {
            foreach (var tuple in list)
            {
                Add(new MTDeviceData() {Name = tuple.Item1, Value = tuple.Item2});
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            foreach (var dd in this)
            {
                sb.AppendLine(dd.ToString());
            }

            return sb.ToString();
        }

        /// <summary>
        /// Restore original MTDeviceDataCollection from MTDataContainer
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static MTDeviceDataCollection Restore(MTDataContainer data)
        {
            if(data.Payload == null)
                return new MTDeviceDataCollection();

            if (data.Payload.Length == 0)
                return new MTDeviceDataCollection();

            data.Decompress();

            using (MemoryStream stream = new MemoryStream(data.Payload))
            {
                try
                {
                    BinaryFormatter f = new BinaryFormatter();
                    object o = f.Deserialize(stream);


                    MTDeviceDataCollection ds = (MTDeviceDataCollection)o;

                    return ds;
                }
                catch (Exception ex)
                {
                    log.Warn(ex);
                }
            }

            return new MTDeviceDataCollection();

        }

        /// <summary>
        /// Serialize current object as MTDataContainer
        /// </summary>
        /// <returns></returns>
        public MTDataContainer GetContainer()
        {
            MTDataContainer container = new MTDataContainer();
            container.FullTypeName = typeof(MTDeviceDataCollection).FullName;

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
}

