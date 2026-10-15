using ChromExplorer.Infrastructure.Properties;

namespace ChromExplorer.Infrastructure
{
    public class ChromDataProviderKeys
    {

        public static string ModasDataProvider
        {
            get { return Resources.ModasChromProviderKey; }
        }

        public static string MappedFileProvider
        {
            get { return Resources.MappedFileProviderKey; }
        }

    }

    public class ChromDataExporterKeys
    {
        public static string MascotExportService
        {
            get { return Resources.MascotExportServiceKey; }
        }
    }
}
