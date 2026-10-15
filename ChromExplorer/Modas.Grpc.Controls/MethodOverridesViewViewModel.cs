using System.Threading.Tasks;
using Modas.Grpc.Client.Shared;
using Modas.Grpc.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Modas.Grpc.Controls
{
    public class MethodOverridesViewViewModel : INotifyPropertyChanged
    {
        private readonly IModasRemoteClient _client;
        private MethodOverrides _overrides;
        private bool _isApplyVisible = false;
        private bool _selectAllOverrides = false;

        public MethodOverridesViewViewModel(IModasRemoteClient client)
        {
            _client = client;
            Overrides = new MethodOverrides();
            Overrides.MeasurementType.PropertyChanged += MeasurementType_PropertyChanged;
            UpdateVisibility();
            ApplyCommand = new AsyncDelegateCommand(OnApply);
        }

        public bool SelectAllOverrides
        {
            get => _selectAllOverrides;
            set
            {
                if (!SetField(ref _selectAllOverrides, value)) return;

                Overrides.IonPolarity.Override = value;
                Overrides.MeasurementType.Override = value;
                Overrides.ParentIonMz.Override = value;
                Overrides.FragmentationAmplitude.Override = value;
                Overrides.SecondaryParentIonMz.Override = value;
                Overrides.SecondaryFragmentationAmplitude.Override = value;
                Overrides.InjectionTimeMs.Override = value;
                Overrides.MFrom.Override = value;
                Overrides.MTo.Override = value;
                Overrides.AgcTarget.Override = value;
                Overrides.MicroscanCount.Override = value;
            }
        }

        private Task OnApply()
        {

            _client.ApplyMethodOverrides(Overrides);

            return Task.CompletedTask;
        }

        public AsyncDelegateCommand ApplyCommand { get; private set; }

        private void MeasurementType_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(e.PropertyName == nameof(Overrides.MeasurementType.Value))
            {
                UpdateVisibility();
            }
        }

        public bool IsApplyVisible
        {
            get => _isApplyVisible;
            set => SetField(ref _isApplyVisible, value);
        }

        public MethodOverrides Overrides
        {
            get => _overrides;
            set
            {
                if (Equals(value, _overrides))
                {
                    return;
                }
                 
                if(_overrides != null && _overrides.MeasurementType != null)
                    _overrides.MeasurementType.PropertyChanged -= MeasurementType_PropertyChanged;

                _overrides = value;

                if(_overrides != null && _overrides.MeasurementType != null)
                    _overrides.MeasurementType.PropertyChanged += MeasurementType_PropertyChanged;
    
                UpdateVisibility();

                OnPropertyChanged();
            }
        }

        private void UpdateVisibility()
        {
            if (Overrides.MeasurementType.Value == MTMeasurementTypes.MS)
            {
                Overrides.ParentIonMz.IsVisible = false;
                Overrides.SecondaryParentIonMz.IsVisible = false;

                Overrides.FragmentationAmplitude.IsVisible = false;
                Overrides.SecondaryFragmentationAmplitude.IsVisible = false;

            }
            else if (Overrides.MeasurementType.Value == MTMeasurementTypes.MS2)
            {
                Overrides.ParentIonMz.IsVisible = true;
                Overrides.SecondaryParentIonMz.IsVisible = false;

                Overrides.FragmentationAmplitude.IsVisible = true;
                Overrides.SecondaryFragmentationAmplitude.IsVisible = false;
            }
            else if (Overrides.MeasurementType.Value == MTMeasurementTypes.MS3)
            {
                Overrides.ParentIonMz.IsVisible = true;
                Overrides.SecondaryParentIonMz.IsVisible = true;

                Overrides.FragmentationAmplitude.IsVisible = true;
                Overrides.SecondaryFragmentationAmplitude.IsVisible = true;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}