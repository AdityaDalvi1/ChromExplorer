using System.IO;
using System.IO.Compression;

namespace ChromExplorer.ModasClient
{
    /// <summary>
    /// Zip utility functions
    /// </summary>
    public static class ZipHelper
    {
        public static byte[] Zip(byte[] data)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var zipStream = new GZipStream(memoryStream, CompressionMode.Compress))
                {
                    zipStream.Write(data, 0, data.Length);
                }

                return memoryStream.ToArray();
            }
        }

        public static byte[] Unzip(byte[] zippedBuffer)
        {
            using (var zippedStream = new MemoryStream(zippedBuffer))
            {
                using (var zipStream = new GZipStream(zippedStream, CompressionMode.Decompress))
                {
                    using (var resultStream = new MemoryStream())
                    {
                        zipStream.CopyTo(resultStream);
                        return resultStream.ToArray();

                    }
                }
            }
        }
    }
}