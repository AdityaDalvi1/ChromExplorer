using System.Reflection;

namespace ChromExplorer
{
    public class CEVersionInfo
    {
        static CEVersionInfo()
        {
            var suffix = string.Empty;
            var name = typeof (CEVersionInfo).Assembly.GetName();
            if (name.ProcessorArchitecture == ProcessorArchitecture.Amd64 || name.ProcessorArchitecture == ProcessorArchitecture.IA64)
            {
                suffix = "(x64)";
            }
            var version = name.Version;
            ProgramTitle = string.Format("ChromExplorer {0}.{1}.{2}", version.Major, version.Minor, version.Build);

            if (!string.IsNullOrEmpty(suffix))
                ProgramTitle += " " + suffix;
        }

        public static string ProgramTitle { get; private set; }
 
    }
}
