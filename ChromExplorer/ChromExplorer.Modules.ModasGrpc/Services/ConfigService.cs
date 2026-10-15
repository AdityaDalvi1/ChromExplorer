using ChromExplorer.Infrastructure.Interfaces;
using Modas.Grpc.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using static System.Environment;

namespace ChromExplorer.Modules.ModasGrpc.Services
{

    [Export(typeof(ConfigService))]
    [ExactTypeMetadata(typeof(ConfigService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class ConfigService
    {
        private readonly List<string> _analysisTypes = new List<string>();
        public ConfigService()
        {
            Methods = LoadMethods();
            if (Methods == null)
            {
                Methods = AnalysisMethodCollection.DefaultMethods;
                SaveMethods(Methods);
            }

            foreach (var m in Methods)
            {
                _analysisTypes.Add(m.SampleType);
            }
        }

        public AnalysisMethodCollection Methods { get; private set; }

        public List<string> AnalysisTypes => _analysisTypes;

        public void SaveMethods()
        {
            SaveMethods(Methods);
        }

        public static AnalysisMethodCollection LoadMethods()
        {

            var filename = GetMethodFileName();

            if (filename == null)
                return null;

            var fi = new FileInfo(filename);
            if (!fi.Exists)
                return null;

            using (StreamReader file = File.OpenText(filename))
            {
                JsonSerializer serializer = new JsonSerializer();
                AnalysisMethodCollection methods = (AnalysisMethodCollection)serializer.Deserialize(file, typeof(AnalysisMethodCollection));
                return methods;
            }
        }

        public static void SaveMethods(AnalysisMethodCollection methods)
        {

            var filename = GetMethodFileName();

            if (filename == null)
                return;

            var path = Path.GetDirectoryName(filename);

            if (path == null)
                return;

            Directory.CreateDirectory(path);

            using (StreamWriter file = File.CreateText(filename))
            {
                JsonSerializer serializer = new JsonSerializer { Formatting = Formatting.Indented };
                serializer.Serialize(file, methods);
            }



        }

        private static string GetMethodFileName()
        {
            var loc = GetFolderPath(SpecialFolder.CommonDocuments);

            var methodFile = Path.Combine(loc, "MassTech\\ChromExplorer\\MTE30.method.json");

            return methodFile;
        }
    }
}   