using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ChromExplorer.OxyPlot.Demo
{
    public class SICDescriptor : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public SICDescriptor(SpectrumXRangeAnnotation annotation)
        {
            Annotation = annotation;
        }

        public SpectrumXRangeAnnotation Annotation { get; }
        public MinMaxLineSeries LineSeries { get; set; }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}