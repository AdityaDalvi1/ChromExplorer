using System.IO;
using System.Xml.Serialization;

namespace ChromExplorer.DataModel.MethodFile
{
    // Saves/loads a ChromMethodFile as plain XML — every type it touches (ChromMethodFile and its
    // nested settings classes) is a plain POCO with public settable properties and a parameterless
    // constructor, so XmlSerializer round-trips them with no extra dependencies.
    public static class ChromMethodFileService
    {
        public const string FileExtension = ".cmethod";
        public const string FileDialogFilter = "ChromExplorer Method (*.cmethod)|*.cmethod|All files (*.*)|*.*";

        public static void Save(ChromMethodFile method, string path)
        {
            var serializer = new XmlSerializer(typeof(ChromMethodFile));
            using (var writer = new StreamWriter(path))
            {
                serializer.Serialize(writer, method);
            }
        }

        public static ChromMethodFile Load(string path)
        {
            var serializer = new XmlSerializer(typeof(ChromMethodFile));
            using (var reader = new StreamReader(path))
            {
                return (ChromMethodFile)serializer.Deserialize(reader);
            }
        }
    }
}
