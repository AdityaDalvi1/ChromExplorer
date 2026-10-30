using Modas.Grpc.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;


namespace Modas.Grpc.Controls
{
    public class StatusViewViewModel
    {
        private readonly Dispatcher _dispatcher;

        // Row name(s) checked against a fixed 0 baseline (not the previous reading, and not an
        // operating-temperature target) — matched by prefix so firmware-version differences in the
        // "(C)" units suffix don't break the match. Capillary Temperature reads 0 before the
        // analysis/heater starts; once it moves more than the tolerance away from that 0 baseline
        // in either direction, the row is flagged.
        private static readonly string[] ZeroBaselineWatchedNamePrefixes = { "Capillary Temperature" };

        // Row name(s) flagged whenever their reading equals a specific "bad" value rather than a
        // numeric drift — e.g. "Buffer Gas" reports a textual level (Low/OK/...), not a number.
        private static readonly string[] LowValueWatchedNamePrefixes = { "Buffer Gas" };
        private const string LowValueText = "Low";

        // Vacuum rows flagged against a fixed threshold (not a between-readings drift, and not a
        // textual value) — matched by prefix so the "(Torr)" unit suffix doesn't break the match.
        // Higher Torr means worse vacuum, so reaching/exceeding the threshold is the bad direction.
        private const string FirstChamberVacuumPrefix = "1st Chamber Vacuum";
        private const double FirstChamberVacuumThreshold = 0.0190;

        private const string SecondChamberVacuumPrefix = "2nd Chamber Vacuum";
        private const double SecondChamberVacuumThreshold = 0.0080;

        // Row Names currently flagged out-of-range, so OutOfRangeDetected only fires once per
        // occurrence (on the transition into the flagged state) instead of on every status poll.
        private readonly HashSet<string> _rowsCurrentlyOutOfRange = new HashSet<string>();

        // Raised the moment a watched row transitions into an out-of-range state - a subscriber
        // (the dashboard view model) uses this to write a single audit log entry per occurrence.
        public event EventHandler<StatusOutOfRangeEventArgs> OutOfRangeDetected;

        public ObservableCollection<StatusItem> Items { get; set; }
        public ICollectionView GroupedItems { get; set; }

        public StatusViewViewModel()
        {
            _dispatcher = Application.Current != null ? Application.Current.Dispatcher : Dispatcher.CurrentDispatcher;

            Items = new ObservableCollection<StatusItem>();
            GroupedItems = CollectionViewSource.GetDefaultView(Items);
            GroupedItems.GroupDescriptions.Add(new PropertyGroupDescription("Category"));
        }

        public void UpdateStatus(IList<StatusItem> items)
        {
            if (_dispatcher.CheckAccess())
            {
                UpdateStatusImpl(items);
            }
            else
            {
                // On a background thread: marshal the call to the UI thread
                _dispatcher.BeginInvoke(new Action(() =>
                {
                    UpdateStatusImpl(items);
                }));
            }
        }

        private void UpdateStatusImpl(IList<StatusItem> items)
        {
            Items.Clear();

            foreach (var item in items)
            {
                if (SafetyChecksEnabled)
                {
                    ApplyCapillarySetpointCheck(item);
                    ApplyLowValueCheck(item);
                    ApplyVacuumThresholdCheck(item);
                }
                Items.Add(item);
            }
        }

        private static bool MatchesAnyPrefix(string name, string[] prefixes)
        {
            foreach (var prefix in prefixes)
            {
                if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        // Capillary Temperature is checked against the SET value of the selected Instrument
        // Method (not a fixed number): the reading must stay within ±CapillaryToleranceDegrees °C of
        // it. Null setpoint (no analysis running / method value unknown) means no check.
        // ONE SWITCH for the instrument checks that can't be satisfied on the simulator:
        // "instrument connected" (pre-start), Buffer Gas / Vacuum / Capillary Temperature (status
        // row highlight + alert log, pre-start block, mid-run auto-abort). Set at startup from
        // App.config "SimulatorMode" (true = simulator = checks off; false = real instrument =
        // checks on).
        public static bool SafetyChecksEnabled = true;

        public const double CapillaryToleranceDegrees = 3.0;
        public double? CapillarySetpoint { get; set; }

        public static bool IsWithinCapillaryWindow(double setpoint, double actual)
        {
            return Math.Abs(actual - setpoint) <= CapillaryToleranceDegrees;
        }

        private void ApplyCapillarySetpointCheck(StatusItem item)
        {
            if (item?.Name == null || !MatchesAnyPrefix(item.Name, ZeroBaselineWatchedNamePrefixes))
                return;

            var setpoint = CapillarySetpoint;
            if (!setpoint.HasValue || !double.TryParse(item.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var currentValue))
            {
                SetOutOfRange(item, false, null);
                return;
            }

            bool isOutOfRange = !IsWithinCapillaryWindow(setpoint.Value, currentValue);
            SetOutOfRange(item, isOutOfRange,
                isOutOfRange
                    ? $"Capillary temperature is {item.Value}, needs to be within ±{CapillaryToleranceDegrees.ToString(CultureInfo.InvariantCulture)} °C of the method's set value {setpoint.Value.ToString(CultureInfo.InvariantCulture)}"
                    : null);
        }

        private void ApplyLowValueCheck(StatusItem item)
        {
            if (item?.Name == null || !MatchesAnyPrefix(item.Name, LowValueWatchedNamePrefixes))
                return;

            bool isOutOfRange = string.Equals(item.Value?.Trim(), LowValueText, StringComparison.OrdinalIgnoreCase);

            SetOutOfRange(item, isOutOfRange, isOutOfRange ? $"reading is {item.Value}" : null);
        }

        private void ApplyVacuumThresholdCheck(StatusItem item)
        {
            if (item?.Name == null)
                return;

            double threshold;
            if (item.Name.StartsWith(FirstChamberVacuumPrefix, StringComparison.OrdinalIgnoreCase))
                threshold = FirstChamberVacuumThreshold;
            else if (item.Name.StartsWith(SecondChamberVacuumPrefix, StringComparison.OrdinalIgnoreCase))
                threshold = SecondChamberVacuumThreshold;
            else
                return;

            if (!double.TryParse(item.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var currentValue))
            {
                SetOutOfRange(item, false, null);
                return;
            }

            bool isOutOfRange = currentValue >= threshold;
            SetOutOfRange(item, isOutOfRange,
                isOutOfRange ? $"{item.Value} reached the {threshold.ToString(CultureInfo.InvariantCulture)} Torr threshold" : null);
        }

        private void SetOutOfRange(StatusItem item, bool isOutOfRange, string reason)
        {
            item.IsOutOfRange = isOutOfRange;
            item.OutOfRangeReason = isOutOfRange ? reason : null;

            bool wasFlagged = _rowsCurrentlyOutOfRange.Contains(item.Name);
            if (isOutOfRange && !wasFlagged)
            {
                _rowsCurrentlyOutOfRange.Add(item.Name);
                OutOfRangeDetected?.Invoke(this, new StatusOutOfRangeEventArgs(item.Name, item.Value, item.Category, reason));
            }
            else if (!isOutOfRange && wasFlagged)
            {
                _rowsCurrentlyOutOfRange.Remove(item.Name);
            }
        }
    }
}


/*
 *Application.Current.Dispatcher.Invoke(() => {
       YourObservableCollection.Add(new Item());
   });
 * */