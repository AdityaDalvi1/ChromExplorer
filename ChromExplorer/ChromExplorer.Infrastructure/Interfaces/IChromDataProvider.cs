using System;
using ChromExplorer.DataModel.Chromatogram;

namespace ChromExplorer.Infrastructure.Interfaces
{
    public interface IChromDataProvider : IResourceDescriptor
    {
        Uri ImportSettingsViewKey { get; }

    
        bool Load(string filename, bool async = true);
        GenericChromatogram Chromatogram { get; }
        string Filename { get; }

    }
}
