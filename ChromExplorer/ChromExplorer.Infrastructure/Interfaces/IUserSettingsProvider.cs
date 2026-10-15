namespace ChromExplorer.Infrastructure.Interfaces
{
    public interface IModasImportUserSettings
    {
        TimeUnits TimeUnits { get; set; }
        bool RebuildTic { get; set; }
        double PPD { get; set; }
        bool RemoveBaseLine { get; set; }
        double NoiseFilterLevel { get; set; }
        bool NormalizeByIT { get; set; }

        double ReferenceIT { get; set; }
    }

    public interface IUserSettingsProvider
    {
        void Save();

        IModasImportUserSettings ModasImportSettings { get; }


    }
}
