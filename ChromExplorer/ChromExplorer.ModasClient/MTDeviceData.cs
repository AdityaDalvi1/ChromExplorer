using System;

namespace ChromExplorer.ModasClient
{
    /// <summary>
    /// A simple name-value-pair for exchanging various kinds of device data
    /// </summary>
    [Serializable]
    public class MTDeviceData
    {
        public string Name { get; set; }
        public object Value { get; set; }

        public override string ToString()
        {
            return string.Format("{0}:{1}", Name, Value);
        }
    }
}
