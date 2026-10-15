using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using ChromExplorer.DataModel.Properties;
using ChromExplorer.Infrastructure.Events;
using Microsoft.Practices.Prism.Events;

namespace ChromExplorer.Infrastructure.Interfaces
{
    [Export(typeof (ChromDataProviderFactory))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class ChromDataProviderFactory : INotifyPropertyChanged
    {
        private readonly IEventAggregator _eventAggregator;
        private IEnumerable<Lazy<IChromDataProvider, ExactTypeMetadata>> _providers;
        private IEnumerable<Lazy<IChromDataExporter, ExactTypeMetadata>> _exporters;
        private IChromDataProvider _current;
        private IChromDataExporter _currentExporter;

        [ImportingConstructor]
        public ChromDataProviderFactory(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
        }

        [ImportMany]
        public IEnumerable<Lazy<IChromDataProvider, ExactTypeMetadata>> Providers
        {
            get { return _providers; }
            set
            {
                if (Equals(value, _providers)) return;
                _providers = value;
                OnPropertyChanged("Providers");
            }
        }

        [ImportMany]
        public IEnumerable<Lazy<IChromDataExporter, ExactTypeMetadata>> Exporters
        {
            get { return _exporters; }
            set
            {
                if (Equals(value, _exporters)) return;
                _exporters = value;
                OnPropertyChanged("Exporters");
            }
        }

        public IChromDataProvider Current
        {
            get { return _current; }
            set
            {
                if (Equals(value, _current)) return;
                _current = value;
                OnPropertyChanged("Current");
            }
        }

        public IChromDataExporter CurrentExporter
        {
            get { return _currentExporter; }
            set
            {
                if (Equals(value, _currentExporter)) return;
                _currentExporter = value;
                OnPropertyChanged("CurrentExporter");
            }
        }

        public IChromDataProvider GetProvider(Type type)
        {
            Current = Providers.First(s => s.Metadata.ExactType == type).Value;
            var e = _eventAggregator.GetEvent<DataProviderChangedEvent>();

            if (e != null)
                e.Publish(type);

            return Current;
        }

        public T GetProvider<T>() where T : IChromDataProvider
        {
            return (T) GetProvider(typeof (T));
        }

        public IChromDataExporter GetExporter(Type type)
        {
            CurrentExporter = Exporters.First(s => s.Metadata.ExactType == type).Value;
            //var e = _eventAggregator.GetEvent<DataProviderChangedEvent>();

            //if (e != null)
            //    e.Publish(type);

            return CurrentExporter;
        }

        public T GetExporter<T>() where T : IChromDataExporter
        {
            return (T) GetExporter(typeof (T));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}