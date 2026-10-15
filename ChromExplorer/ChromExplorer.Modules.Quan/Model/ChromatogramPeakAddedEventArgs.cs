using System;

namespace ChromExplorer.Modules.Quan.Model
{
    public class ChromatogramPeakAddedEventArgs : EventArgs
    {
        public ChromatogramPeakAddedEventArgs(ChromatogramPeakInfo info)
        {
            Peak = info;
        }


        public ChromatogramPeakInfo Peak { get; }
    }
}