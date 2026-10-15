using System;

namespace Modas.Grpc.Controls
{
    // Raised by StatusViewViewModel the moment a watched row transitions INTO an out-of-range
    // state (not on every poll it stays that way), so a subscriber can write one audit log entry
    // per occurrence instead of one per status update.
    public class StatusOutOfRangeEventArgs : EventArgs
    {
        public string Name { get; }
        public string Value { get; }
        public string Category { get; }
        public string Reason { get; }

        public StatusOutOfRangeEventArgs(string name, string value, string category, string reason)
        {
            Name = name;
            Value = value;
            Category = category;
            Reason = reason;
        }
    }
}
