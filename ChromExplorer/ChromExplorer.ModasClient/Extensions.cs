using System.Text;

namespace Modas.MTRemote.Data
{
    public static class Extensions
    {
        public static string ToHex(this byte[] data, bool toUpper = true)
        {
            StringBuilder result = new StringBuilder(data.Length * 2);

            for (int i = 0; i < data.Length; i++)
                result.Append(data[i].ToString(toUpper ? "X2" : "x2"));

            return result.ToString();
        }
    }
}