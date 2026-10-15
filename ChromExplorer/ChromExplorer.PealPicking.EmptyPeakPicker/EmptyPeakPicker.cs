using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Controls;
using System.Windows.Media;
using ChromExplorer.DataModel.PeakPicking;
using ChromExplorer.Infrastructure.Interfaces;
using ChromExplorer.PealPicking.EmptyPeakPicker.Views;

namespace ChromExplorer.PealPicking.EmptyPeakPicker
{
    [Export(typeof(IPeakPicker))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class EmptyPeakPicker : IPeakPicker
    {
        //[Import]
        private EmptySettingsView _settings;

        public EmptyPeakPicker()
        {
            _settings = new EmptySettingsView();
            ObjectKey = "EmptyPeakPicker";
            Title = "None";
            Description = "Empty peak picker (do nothing)";
            Tooltip = "Empty peak picker (do nothing)";
        }

 

        #region Implementation of IResourceDescriptor

        public string ObjectKey { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Tooltip { get; set; }
        public ImageSource Icon { get; set; }
       

        #endregion

        // TODO [Log(EntryLevel = LogLevel.Debug, ExitLevel = LogLevel.Debug, ExceptionLevel = LogLevel.Error)]
        public List<PeakInfo> FindPeaks<T>(double[] xs, double[] ys, double min, double max, double threshold, bool percentage) where T : IPeakAnalyzer, new()
        {
            return new List<PeakInfo>();
        }

        public UserControl ConfigView
        {
            get { return _settings; }
        }
    }
}
