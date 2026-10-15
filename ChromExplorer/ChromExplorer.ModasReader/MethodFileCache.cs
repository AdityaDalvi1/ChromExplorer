using System.Collections.Generic;
using System.Linq;

namespace ChromExplorer.ModasReader
{
    public class MethodFileCache
    {
        private readonly Dictionary<int, MethodFileLite> _methods;

        public MethodFileCache()
        {
            _methods = new Dictionary<int, MethodFileLite>();
        }

        public MethodFileCache(IEnumerable<Method> methods)
        {
            _methods = new Dictionary<int, MethodFileLite>();
            foreach (var method in methods)
            {
                _methods.Add(method.Id, new MethodFileLite(method));
            }
        }

        public void Add(Method method)
        {
            if(!_methods.ContainsKey(method.Id))
            {
                _methods.Add(method.Id, new MethodFileLite(method));
            }
        }

        public MethodFileLite GetMethod(int id)
        {
            if (_methods.ContainsKey(id))
                return _methods[id];

            return null;
        }

        public IList<MethodFileLite> Methods
        {
            get
            {
                return _methods.Select(k => k.Value).ToList();
            }
        }

        public void Clear()
        {
            _methods.Clear();
        }
    }
}
