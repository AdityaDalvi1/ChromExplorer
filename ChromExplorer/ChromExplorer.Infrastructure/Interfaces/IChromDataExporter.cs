using System;

namespace ChromExplorer.Infrastructure.Interfaces
{
    public interface IChromDataExporter : IResourceDescriptor
    {
        Uri ExportSettingsViewKey { get; }
        void Export(IExportSettings settings);
    }
}
