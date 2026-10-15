using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace ChromExplorer.ModasReader
{
    /// <summary>Reads the acquisition settings that the instrument embeds in a .mtd file: the
    /// method XML in the Method table plus per-scan facts (MS power, parent ion, injection time,
    /// timestamps) from the Spectrum table. Values the file does not contain are left unset so
    /// the Method Report can omit them.</summary>
    public static class MtdMethodInfoReader
    {
        // "{SampleName}_{Analysis_Type}_{ddMMyyyy}_{HHmm}" - how the acquisition side names files,
        // e.g. "Caffeine_ESI_MSMS_23122025_1330". The analysis type is a word plus MS/MSMS.
        private static readonly Regex FileNamePattern = new Regex(
            @"^(?<name>.+)_(?<type>[A-Za-z]+_MS\w*)_\d{8}_\d{4}$", RegexOptions.Compiled);

        public static MtdMethodInfo Read(ModasDataReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            var info = new MtdMethodInfo { FilePath = reader.FileName };
            ParseFileName(info);

            IList<Method> methods = reader.GetMethods();
            IList<Spectrum> spectra;
            using (var uof = reader.Context.CreateUnitOfWork())
            {
                spectra = uof.Find<Spectrum>();
            }

            info.ScanCount = spectra.Count;
            if (spectra.Count > 0)
            {
                info.MsPower = spectra.Max(s => s.MsPower);
                info.AcquiredAt = spectra.Min(s => s.Timestamp);
                info.DurationSeconds = (spectra.Max(s => s.Timestamp) - info.AcquiredAt.Value).TotalSeconds;

                info.ParentIons = spectra.Where(s => s.ParentIonMass > 0)
                    .Select(s => Math.Round(s.ParentIonMass, 2)).Distinct().OrderBy(p => p).ToList();
            }

            // Method-level values come from the method the first scan used.
            int? firstMethodId = spectra.OrderBy(s => s.Timestamp).Select(s => s.MethodId).FirstOrDefault();
            Method method = methods.FirstOrDefault(m => m.Id == firstMethodId) ?? methods.FirstOrDefault();
            if (method != null && !string.IsNullOrWhiteSpace(method.Xml))
            {
                var doc = new XmlDocument();
                doc.LoadXml(method.Xml);
                ReadMethodXml(doc.DocumentElement, info);
            }

            SetMeasurementType(info);
            SetInjectionTime(info, spectra);
            return info;
        }

        private static void ParseFileName(MtdMethodInfo info)
        {
            if (string.IsNullOrEmpty(info.FilePath))
                return;

            var match = FileNamePattern.Match(Path.GetFileNameWithoutExtension(info.FilePath));
            if (!match.Success)
                return;

            info.SampleName = match.Groups["name"].Value.Replace('_', ' ');
            info.AnalysisType = match.Groups["type"].Value.Replace('_', ' ');
        }

        private static void ReadMethodXml(XmlElement root, MtdMethodInfo info)
        {
            if (root == null)
                return;

            info.MethodName = Text(root, "Name");
            string fullPath = Text(root, "FullPath");
            info.MethodFile = fullPath == null ? null : fullPath.Substring(fullPath.LastIndexOfAny(new[] { '\\', '/' }) + 1);
            info.SerialNumber = Text(root, "SerialNumber");
            info.Polarity = Text(root, "IonPolarity");
            info.MassFrom = Number(root, "MassFrom");
            info.MassTo = Number(root, "MassTo");
            info.MeasurementType = Text(root, "Type");

            double? microscans = Number(root, "MicroscansCount");
            if (microscans.HasValue && microscans.Value > 0)
                info.Microscans = (int)microscans.Value;

            // (sic) "AGCSettigs" is how the instrument software spells the element
            info.AgcMode = Text(root, "AGCSettigs/Mode");

            double? ms3Parent = Number(root, "Ms3ParentIonMass");
            if (ms3Parent.HasValue && ms3Parent.Value > 0)
                info.SecondaryParentIon = ms3Parent;

            // Fragmentation amplitude actually used = the "Fragmentation" step(s) of the scan
            // sequence (not the 0.03 "Amplitude (FD)" template setting, which is a different scale).
            var amplitudes = new List<double>();
            foreach (XmlNode action in root.SelectNodes("ScanActions/MethodAction"))
            {
                if (!string.Equals(Text(action, "Name"), "Fragmentation", StringComparison.OrdinalIgnoreCase))
                    continue;

                double? amp = Number(action, "FragmentationAmplitude");
                if (amp.HasValue && amp.Value > 0)
                    amplitudes.Add(amp.Value);
            }
            if (amplitudes.Count > 0) info.FragmentationAmplitude = amplitudes[0];
            if (amplitudes.Count > 1) info.SecondaryFragmentationAmplitude = amplitudes[1];

            ReadIonSource(root, info);
        }

        // Spray voltage / heater are only reported when the file holds a non-zero reading. The
        // sample files checked so far carry neither, so this is deliberately tolerant.
        private static void ReadIonSource(XmlElement root, MtdMethodInfo info)
        {
            foreach (XmlNode channel in root.SelectNodes("VoltageChannels/VoltageChannel"))
            {
                string name = Text(channel, "ChannelName") ?? "";
                double? value = Number(channel, "Value");
                if (!value.HasValue || value.Value <= 0)
                    continue;

                if (name.IndexOf("heater", StringComparison.OrdinalIgnoreCase) >= 0)
                    info.HeaterValue = value;
                else if (name.IndexOf("spray", StringComparison.OrdinalIgnoreCase) >= 0)
                    info.SprayVoltage = value;
            }

            string polarityNode = string.Equals(info.Polarity, "Negative", StringComparison.OrdinalIgnoreCase)
                ? "NegativeVoltages" : "PositiveVoltages";
            foreach (XmlNode item in root.SelectNodes("IonSourceInformation/" + polarityNode + "/*"))
            {
                string label = Text(item, "Name") ?? Text(item, "ChannelName") ?? item.Name;
                double? value = Number(item, "Value");
                if (value.HasValue && value.Value > 0 && label.IndexOf("spray", StringComparison.OrdinalIgnoreCase) >= 0)
                    info.SprayVoltage = value;
            }
        }

        private static void SetMeasurementType(MtdMethodInfo info)
        {
            // Scan-level MS power is authoritative; fall back to the method's own Type text.
            int power = info.MsPower;
            if (power <= 0)
            {
                string type = info.MeasurementType ?? "";
                power = type.Contains("3") ? 3 : type.IndexOf("MSMS", StringComparison.OrdinalIgnoreCase) >= 0 ? 2 : 1;
            }

            info.MsPower = power;
            info.MeasurementType = power >= 3 ? "MS3" : power == 2 ? "MS2" : "MS";
        }

        // The Spectrum table stores the injection time summed over the microscans, so the
        // per-scan value is InjectionTime / Microscans (0.6 s over 3 microscans = 200 ms).
        private static void SetInjectionTime(MtdMethodInfo info, IList<Spectrum> spectra)
        {
            var values = spectra.Where(s => s.InjectionTime > 0).Select(s => s.InjectionTime).ToList();
            if (values.Count == 0)
                return;

            double divisor = info.Microscans ?? 1;
            info.InjectionTimeMinMs = Math.Round(values.Min() / divisor * 1000.0, 3);
            info.InjectionTimeMaxMs = Math.Round(values.Max() / divisor * 1000.0, 3);
        }

        private static string Text(XmlNode node, string xpath)
        {
            var found = node.SelectSingleNode(xpath);
            return found == null || string.IsNullOrWhiteSpace(found.InnerText) ? null : found.InnerText.Trim();
        }

        private static double? Number(XmlNode node, string xpath)
        {
            double value;
            return double.TryParse(Text(node, xpath), NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                ? value : (double?)null;
        }
    }
}
