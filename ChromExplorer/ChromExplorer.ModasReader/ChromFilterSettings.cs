using System.Collections.ObjectModel;
using System.ComponentModel;
using ChromExplorer.DataModel.Properties;
using ChromExplorer.Infrastructure;
using Mindscape.LightSpeed;
using Mindscape.LightSpeed.Querying;

namespace ChromExplorer.ModasReader

{

    public class ChromFilterSettings : INotifyPropertyChanged
    {

        private ChromFilters _filterType;

        private ObservableCollection<MethodFileLite> _methods;

        private MethodFileLite _selectedMethod;

        private double _parentIonMass;

        private double _massAccuracy;

        private MsnTypes _msnType;

        private ObservableCollection<double> _injectionTimes;

        private double _selectedInjectionTime;

        private ObservableCollection<MassRangeOption> _massRanges;

        private MassRangeOption _selectedMassRange;


        public ChromFilterSettings()

        {

            FilterType = ChromFilters.None;

            ParentIonMass = 0.0;

            MassAccuracy = 1.0;

            MsnType = MsnTypes.MS;

        }


        public ChromFilters FilterType

        {

            get { return _filterType; }

            set
            {

                if (value == _filterType) return;

                _filterType = value;

                OnPropertyChanged("FilterType");

            }

        }


        public ObservableCollection<MethodFileLite> Methods

        {

            get { return _methods; }

            set
            {

                if (Equals(value, _methods)) return;

                _methods = value;

                OnPropertyChanged("Methods");

            }

        }


        public MethodFileLite SelectedMethod

        {

            get { return _selectedMethod; }

            set
            {

                if (Equals(value, _selectedMethod)) return;

                _selectedMethod = value;

                OnPropertyChanged("SelectedMethod");

            }

        }


        public ObservableCollection<double> InjectionTimes

        {

            get
            {

                return _injectionTimes;

            }

            set
            {

                if (Equals(value, _injectionTimes)) return;

                _injectionTimes = value;

                OnPropertyChanged(nameof(InjectionTimes));

            }

        }


        public double SelectedInjectionTime

        {

            get
            {

                return _selectedInjectionTime;

            }

            set
            {

                if (value.Equals(_selectedInjectionTime)) return;

                _selectedInjectionTime = value;

                OnPropertyChanged(nameof(SelectedInjectionTime));

            }

        }



        public double ParentIonMass

        {

            get { return _parentIonMass; }

            set
            {

                if (value.Equals(_parentIonMass)) return;

                _parentIonMass = value;

                OnPropertyChanged("ParentIonMass");

            }

        }


        public double MassAccuracy

        {

            get { return _massAccuracy; }

            set
            {

                if (value.Equals(_massAccuracy)) return;

                _massAccuracy = value;

                OnPropertyChanged("MassAccuracy");

            }

        }


        public MsnTypes MsnType

        {

            get { return _msnType; }

            set
            {

                if (value == _msnType) return;

                _msnType = value;

                OnPropertyChanged("MsnType");

            }

        }


        public ObservableCollection<MassRangeOption> MassRanges

        {

            get { return _massRanges; }

            set
            {

                if (Equals(value, _massRanges)) return;

                _massRanges = value;

                OnPropertyChanged(nameof(MassRanges));

            }

        }


        public MassRangeOption SelectedMassRange

        {

            get { return _selectedMassRange; }

            set
            {

                if (Equals(value, _selectedMassRange)) return;

                _selectedMassRange = value;

                OnPropertyChanged(nameof(SelectedMassRange));

                OnPropertyChanged(nameof(ParentMass));

            }

        }

        // Read-only display for the Parent Mass textbox: the parent mass(es) that share the
        // selected mass range (e.g. "609, 609.1" for range 270-620). Filtering itself is done
        // by mass range alone, covering every parent mass listed here together.
        public string ParentMass

        {

            get
            {

                return _selectedMassRange?.ParentMassDisplay;

            }

        }


        public QueryExpression Query

        {

            get { return BuildQueryExpression(); }

        }


        private QueryExpression BuildQueryExpression()

        {

            switch (FilterType)

            {

                case ChromFilters.Method:

                    return BuildMethodExpression();


                case ChromFilters.ParentIon:

                    return BuildParentIonExpression();


                case ChromFilters.MsN:

                    return BuildMsnFilterExpression();


                case ChromFilters.InjectionTime:

                    return BuildInjectionTimeExpression();


                case ChromFilters.MassRange:

                    return BuildMassRangeExpression();

            }


            return null;

        }


        private QueryExpression BuildInjectionTimeExpression()

        {

            return Entity.Attribute("InjectionTime") == SelectedInjectionTime;

        }


        private QueryExpression BuildMassRangeExpression()

        {

            if (SelectedMassRange == null || SelectedMassRange.MethodIds.Count == 0)

                return null;

            // A mass range can be shared by several MethodIds (e.g. 609 and 609.1 both scan
            // 270-620) — include every scan belonging to any of them.
            QueryExpression expr = null;

            foreach (var methodId in SelectedMassRange.MethodIds)
            {
                var clause = Entity.Attribute("MethodId") == methodId;
                expr = expr == null ? clause : expr || clause;
            }

            return expr;

        }


        private QueryExpression BuildMsnFilterExpression()

        {

            return Entity.Attribute("MsPower") == (int)MsnType;

        }


        private QueryExpression BuildParentIonExpression()

        {

            return Entity.Attribute("ParentIonMass") > ParentIonMass - MassAccuracy

                   && Entity.Attribute("ParentIonMass") < ParentIonMass + MassAccuracy;

        }


        private QueryExpression BuildMethodExpression()

        {

            if (SelectedMethod == null)

                return null;


            return Entity.Attribute("MethodId") == SelectedMethod.ID;

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