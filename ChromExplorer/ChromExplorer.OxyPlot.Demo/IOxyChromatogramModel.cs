using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using ChromExplorer.DataModel.Chromatogram;
using ChromExplorer.DataModel.Spectrum;
using OxyPlot;

namespace ChromExplorer.OxyPlot.Demo
{
    
    public interface IOxyChromatogramModel : INotifyPropertyChanged
    {
        List<ChromatogramPoint> InstrumentChromatogram { get; }
        ISpectrum CurrentSpectrum { get; }

        int SelectedSpectumIndex { get; set; }

        void SetSelectedSpectrumIndexes(double xmin, double xmax);

        string Title { get; }
        string Description { get; }

        string Filename { get; }

        double PPD { get; }

        ObservableCollection<SICDescriptor> SICDescriptors { get; }

        void FilterChromatogram(IChromFilter filter);

        void AddSelectedChromatogram(SpectrumXRangeAnnotation annotation);
        void RemoveSelectedChromatogram(SpectrumXRangeAnnotation annotation);

        void CreateSicChromatogram(SpectrumXRangeAnnotation annotation);

        void ExportChromPlots(string filename);

        List<ChromatogramPoint> GetChromatogram(IChromFilter filter);
       

        //TODO Delete ???
        ISpectrum GetAggregatedSpectrum(List<ChromatogramPoint> points);


    }
}