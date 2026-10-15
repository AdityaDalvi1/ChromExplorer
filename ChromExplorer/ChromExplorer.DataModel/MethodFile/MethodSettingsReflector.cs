using System;
using System.Collections.Generic;
using System.Globalization;

namespace ChromExplorer.DataModel.MethodFile
{
    // Generic reflection helpers to read/write settings on objects that don't expose a typed
    // settings contract (IPostProcessingOperation/IPeakPicker deliberately keep their concrete
    // settings objects private, only reachable via ConfigView.DataContext or a "Settings"
    // property) — same "get ConfigView, get its DataContext, walk public properties" pattern
    // PostProcessingViewViewModel already uses for audit logging, extended here to also write.
    public static class MethodSettingsReflector
    {
        public static List<ParamEntry> CaptureConfigViewParameters(object operation)
        {
            var result = new List<ParamEntry>();
            var dataContext = GetConfigViewDataContext(operation);
            if (dataContext == null)
                return result;

            foreach (var p in dataContext.GetType().GetProperties())
            {
                if (p.Name == "Title" || p.Name == "IsActive" || !p.CanRead || !p.CanWrite)
                    continue;

                try
                {
                    var value = p.GetValue(dataContext, null);
                    if (value == null)
                        continue;

                    result.Add(new ParamEntry
                    {
                        Name = p.Name,
                        Value = Convert.ToString(value, CultureInfo.InvariantCulture)
                    });
                }
                catch
                {
                    // Best-effort capture — skip properties that can't be read/converted.
                }
            }

            return result;
        }

        public static void ApplyConfigViewParameters(object operation, List<ParamEntry> parameters)
        {
            var dataContext = GetConfigViewDataContext(operation);
            if (dataContext == null || parameters == null)
                return;

            foreach (var entry in parameters)
            {
                var prop = dataContext.GetType().GetProperty(entry.Name);
                if (prop == null || !prop.CanWrite)
                    continue;

                try
                {
                    var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                    var converted = Convert.ChangeType(entry.Value, targetType, CultureInfo.InvariantCulture);
                    prop.SetValue(dataContext, converted, null);
                }
                catch
                {
                    // Best-effort restore — skip values that no longer fit the property.
                }
            }
        }

        // ModasPeakPicker exposes a public "Settings" property (MinPeakHalfSize/PeakHalfSize) that
        // IPeakPicker doesn't declare — read via reflection so this works without depending on the
        // concrete type.
        public static bool TryGetPeakPickerHalfSizes(object peakPicker, out int minPeakHalfSize, out int peakHalfSize)
        {
            minPeakHalfSize = 0;
            peakHalfSize = 0;

            var settings = peakPicker?.GetType().GetProperty("Settings")?.GetValue(peakPicker, null);
            if (settings == null)
                return false;

            var minProp = settings.GetType().GetProperty("MinPeakHalfSize");
            var peakProp = settings.GetType().GetProperty("PeakHalfSize");
            if (minProp == null || peakProp == null)
                return false;

            minPeakHalfSize = Convert.ToInt32(minProp.GetValue(settings, null));
            peakHalfSize = Convert.ToInt32(peakProp.GetValue(settings, null));
            return true;
        }

        public static void TrySetPeakPickerHalfSizes(object peakPicker, int minPeakHalfSize, int peakHalfSize)
        {
            var settings = peakPicker?.GetType().GetProperty("Settings")?.GetValue(peakPicker, null);
            if (settings == null)
                return;

            settings.GetType().GetProperty("MinPeakHalfSize")?.SetValue(settings, minPeakHalfSize, null);
            settings.GetType().GetProperty("PeakHalfSize")?.SetValue(settings, peakHalfSize, null);
        }

        private static object GetConfigViewDataContext(object operation)
        {
            var configView = operation?.GetType().GetProperty("ConfigView")?.GetValue(operation, null);
            return configView?.GetType().GetProperty("DataContext")?.GetValue(configView, null);
        }
    }
}
