using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using ChromExplorer.ModasClient;

namespace Modas.MTRemote.Data
{
    /// <summary>
    /// Transport container for sending large objects to remote client as gzipped binary arrays
    /// </summary>
    public partial struct MTDataContainer
    {
        /// <summary>
        /// Compress current <see cref="Payload"></see> array
        /// </summary>
        public void Compress()
        {
            Compressed = true;
            var compressed = ZipHelper.Zip(Payload);
            Payload = compressed;
        }

        /// <summary>
        /// Decompress <typeparamref name="Payload"></typeparamref> array
        /// </summary>
        public void Decompress()
        {
            if (Compressed)
            {
                var decompressed = ZipHelper.Unzip(Payload);
                Payload = decompressed;
            }
        }

        /// <summary>
        /// Computes MD5 hash of the current <typeparamref name="Payload"></typeparamref> array
        /// </summary>
        public void UpdateHash()
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                md5.TransformFinalBlock(Payload, 0, Payload.Length);
                MD5Hash = md5.Hash.ToHex(true);
            }
        }
    }
}