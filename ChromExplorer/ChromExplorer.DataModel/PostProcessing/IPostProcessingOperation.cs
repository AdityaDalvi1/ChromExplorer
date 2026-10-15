using System.ComponentModel;
using ChromExplorer.DataModel.Core;
using ChromExplorer.DataModel.Spectrum;

namespace ChromExplorer.DataModel.PostProcessing
{
    public enum PostProcessingType
    {
        MS,
        EIC
    }

    public interface IPostProcessingOperation : INotifyPropertyChanged
    {
        string ObjectKey { get; set; }
        string Title { get; set; }
        bool IsActive { get; set; }
        object ConfigView { get; }
        int Order { get; }
        PostProcessingType Type { get; }

        void ApplyInPlace(MsSpectrum spectrum);
        MsSpectrum Apply(MsSpectrum spectrum);
        
        void ApplyInPlace(SimpleDataSource dataSource);
        SimpleDataSource Apply(SimpleDataSource dataSource);

        void SaveSettings();

    }
}
