using System.IO;
using Modas.MTRemote.Data;

namespace ChromExplorer.ModasClient
{
    /// <summary>
    /// Method file utilities
    /// </summary>
    public class MTMethodHelper
    {
        /// <summary>
        /// Create data container with method file 
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static MTDataContainer GetContainer(string filename)
        {
            var fi = new FileInfo(filename);
            if(!fi.Exists)
                return new MTDataContainer();

            var c = new MTDataContainer();
            c.FullTypeName = "MethodFileXML";

            c.Payload = File.ReadAllBytes(filename);

            if(MTRemoteGlobals.CompressData)
                c.Compress();

            c.UpdateHash();

            return c;
        }

        public static byte[] GetMethodBytes(MTDataContainer c)
        {
            c.Decompress();
            return c.Payload;
        }
    }
}