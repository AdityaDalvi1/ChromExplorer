using System.Collections.Generic;
using ChromExplorer.DataModel.Core;
using ChromExplorer.DataModel.Spectrum;

namespace ChromExplorer.DataModel.PostProcessing
{
    public interface IPostProcessingService
    {
        IEnumerable<IPostProcessingOperation> Operations { get; }
        IEnumerable<IPostProcessingOperation> ActiveOperations { get; }
        IEnumerable<IPostProcessingOperation> ActiveMsOperations { get; }
        IEnumerable<IPostProcessingOperation> ActiveEicOperations { get; }

        void InPlaceProcessing(MsSpectrum spectrum);
        MsSpectrum Processing(MsSpectrum spectrum);

        void InPlaceProcessing(SimpleDataSource dataSource);
        SimpleDataSource Processing(SimpleDataSource dataSource);

        void RaisePostProcessingApplied(object type = null);
        void SaveSettings();
        void SaveMsSettings();
        void SaveEicSettings();
        void Reset();
    }
}
