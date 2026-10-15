using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using Microsoft.Practices.Prism.Regions;

namespace ChromExplorer.Mocks
{
    
    public class MockRegionManager : IRegionManager
    {
        private MockRegionCollection _regions = new MockRegionCollection();

        public IRegionCollection Regions
        {
            get { return _regions; }
        }

        public IRegion AttachNewRegion(object regionTarget, string regionName)
        {
            throw new NotImplementedException();
        }

        public IRegionManager CreateRegionManager()
        {
            throw new NotImplementedException();
        }
    }

    internal class MockRegionCollection : IRegionCollection
    {
        private Dictionary<string, IRegion> regions = new Dictionary<string, IRegion>();

        public IEnumerator<IRegion> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IRegion this[string regionName]
        {
            get { return regions[regionName]; }
        }

        public void Add(IRegion region)
        {
            regions[region.Name] = region;
        }

        public bool Remove(string regionName)
        {
            throw new NotImplementedException();
        }

        public bool ContainsRegionWithName(string regionName)
        {
            throw new NotImplementedException();
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;
    }

    public class MockNewsRegion : IRegion
    {
        public object GetViewReturnValue;
        public string GetViewArgumentName;
        public object ShowArgumentView;
        public string AddArgumentName;
        public IList<object> AddedViews = new List<object>();

        #region IRegion Members

        public string Name { get; set; }

        public Comparison<object> SortComparison
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public IRegionManager Add(object view)
        {
            AddedViews.Add(view);
            return null;
        }

        public void Remove(object view)
        {
            throw new NotImplementedException();
        }

        public IViewsCollection Views
        {
            get { throw new NotImplementedException(); }
        }

        public void Activate(object view)
        {
            ShowArgumentView = view;
        }

        public void Deactivate(object view)
        {
            throw new NotImplementedException();
        }

        public IRegionManager Add(object view, string viewName)
        {
            AddArgumentName = viewName;
            Add(view);
            return null;
        }

        public object GetView(string viewName)
        {
            GetViewArgumentName = viewName;
            return GetViewReturnValue;
        }

        public IRegionManager Add(object view, string viewName, bool createRegionManagerScope)
        {
            throw new NotImplementedException();
        }

        public IRegionManager RegionManager
        {
            set { throw new NotImplementedException(); }
            get { throw new NotImplementedException(); }
        }

        public IRegionBehaviorCollection Behaviors
        {
            get { throw new NotImplementedException(); }
        }

        public IRegionNavigationService NavigationService
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public IViewsCollection ActiveViews
        {
            get { throw new NotImplementedException(); }
        }

        public object Context
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;
        public void RequestNavigate(Uri target, Action<NavigationResult> navigationCallback)
        {
            throw new NotImplementedException();
        }
    }
}
