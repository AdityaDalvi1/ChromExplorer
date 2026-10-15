using System.IO;

namespace ChromExplorer.DataModel.Modas
{
    public class ModasFileInfo
    {
        private readonly FileInfo _fi;

        public ModasFileInfo(string path)
        {
            _fi = new FileInfo(path);
        }

        public bool Exists
        {
            get { return _fi.Exists;  }
        }

        public string FullPath
        {
            get { return _fi.FullName; }
        }

        public string Name
        {
            get { return _fi.Name; }
        }
    }
}
