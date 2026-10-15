using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using ChromExplorer.DataModel.Core;
using ChromExplorer.DataModel.PostProcessing;
using ChromExplorer.DataModel.Spectrum;
using ChromExplorer.Infrastructure.Events;
using log4net;
using Microsoft.Practices.Prism.Events;

namespace ChromExplorer.Services.PostProcessingService.Services
{
    [Export(typeof(IPostProcessingService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class PostProcessingService : IPostProcessingService, IPartImportsSatisfiedNotification
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);


        [Import]
        private IEventAggregator _eventAggregator;

        [ImportMany]
        private List<IPostProcessingOperation> _operations;

        #region Implementation of IPostProcessingService

        public IEnumerable<IPostProcessingOperation> Operations
        {
            get { return _operations; }
        }

        public IEnumerable<IPostProcessingOperation> ActiveOperations
        {
            get
            {
                return _operations == null ? null : _operations.Where(operation => operation.IsActive);
            }
        }

        public IEnumerable<IPostProcessingOperation> ActiveMsOperations
        {
            get
            {
                return _operations == null ? null : _operations.Where(operation => operation.IsActive && operation.Type == PostProcessingType.MS);
            }
        }

        public IEnumerable<IPostProcessingOperation> ActiveEicOperations
        {
            get
            {
                return _operations == null ? null : _operations.Where(operation => operation.IsActive && operation.Type == PostProcessingType.EIC);
            }
        }

        public void InPlaceProcessing(MsSpectrum spectrum)
        {
            if (ActiveMsOperations == null) return;

            if (spectrum == null)
                return;

            bool applied = false;
            foreach (var operation in ActiveMsOperations)
            {
                operation.ApplyInPlace(spectrum);
                applied = true;
            }

            if (applied)
                spectrum.UpdateStatistics();
        }

        public MsSpectrum Processing(MsSpectrum spectrum)
        {
            try
            {
                if (ActiveMsOperations == null) return null;

                if (spectrum == null)
                    return null;

                return ActiveMsOperations.Aggregate(spectrum, (current, operation) => operation.Apply(current));
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }

            return null;
        }

        public void InPlaceProcessing(SimpleDataSource dataSource)
        {
            if (ActiveEicOperations == null) return;
            if (dataSource == null) return;

            foreach (var operation in ActiveEicOperations)
            {
                operation.ApplyInPlace(dataSource);
            }
        }

        public SimpleDataSource Processing(SimpleDataSource dataSource)
        {
            try
            {
                if (ActiveEicOperations == null) return dataSource;
                if (dataSource == null) return null;

                return ActiveEicOperations.Aggregate(dataSource, (current, operation) => operation.Apply(current));
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }

            return dataSource;
        }

        public void RaisePostProcessingApplied(object type = null)
        {
            _eventAggregator.GetEvent<PostProcessingAppliedEvent>().Publish(type);
        }

        public void SaveSettings()
        {
            foreach (var operation in _operations)
            {
                operation.SaveSettings();
            }
        }

        public void SaveMsSettings()
        {
            if (_operations == null) return;
            foreach (var operation in _operations.Where(o => o.Type == PostProcessingType.MS))
            {
                operation.SaveSettings();
            }
        }

        public void SaveEicSettings()
        {
            if (_operations == null) return;
            foreach (var operation in _operations.Where(o => o.Type == PostProcessingType.EIC))
            {
                operation.SaveSettings();
            }
        }

        public void Reset()
        {
            if (_operations == null) return;
            foreach (var operation in _operations)
            {
                operation.IsActive = false;
            }
        }

        #endregion

        #region Implementation of IPartImportsSatisfiedNotification

        public void OnImportsSatisfied()
        {
            _operations.Sort((l, r) => l.Order.CompareTo(r.Order));

            if (_eventAggregator != null)
            {
                _eventAggregator.GetEvent<ChromatogramChangedEvent>().Subscribe(_ => ResetOperationStateForNewData());
                _eventAggregator.GetEvent<DataProviderChangedEvent>().Subscribe(_ => ResetOperationStateForNewData());
            }

            //if (_operations.Count > 0)
            //    _operations[0].IsActive = true;
        }

        private void ResetOperationStateForNewData()
        {
            if (_operations == null) return;

            foreach (var operation in _operations)
            {
                if (operation == null) continue;

                // Deactivate the filter so a freshly loaded file starts out unfiltered (raw TIC),
                // instead of carrying over whatever was enabled for the previous file.
                operation.IsActive = false;

                // Only reset internal iterative state on operations that have it (Gaussian, Savitzky-Golay and Median filters in this codebase).
                var type = operation.GetType();
                var typeName = type.Name;
                if (typeName.IndexOf("Gaussian", StringComparison.OrdinalIgnoreCase) < 0
                    && typeName.IndexOf("SavitzkyGolay", StringComparison.OrdinalIgnoreCase) < 0
                    && typeName.IndexOf("Median", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                try
                {
                    // Reset iteration counter
                    SetPrivateFieldIfExists(type, operation, "_currentIterations", 1);

                    // Reset common "last input" tracking (so first apply behaves as a first pass)
                    SetPrivateFieldIfExists(type, operation, "_lastInput", null);
                    SetPrivateFieldIfExists(type, operation, "_lastX", null);
                    SetPrivateFieldIfExists(type, operation, "_lastY", null);
                    SetPrivateFieldIfExists(type, operation, "_lastLength", -1);
                    SetPrivateFieldIfExists(type, operation, "_lastXStart", double.NaN);
                    SetPrivateFieldIfExists(type, operation, "_lastXEnd", double.NaN);
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                }
            }
        }

        private static void SetPrivateFieldIfExists(Type type, object instance, string fieldName, object value)
        {
            var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) return;
            field.SetValue(instance, value);
        }

        #endregion
    }
}
