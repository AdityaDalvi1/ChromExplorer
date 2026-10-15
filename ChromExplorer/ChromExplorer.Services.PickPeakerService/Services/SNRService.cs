using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using ChromExplorer.DataModel.PeakPicking;
using ChromExplorer.DataModel.Core;
using ChromExplorer.Infrastructure.Interfaces;

namespace ChromExplorer.Services.PickPeakerService.Services
{
    [Export(typeof(ISNRService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class SNRService : ISNRService
    {
        public SNRResult CalculateSNR(
            SimpleDataSource points,
            List<PeakInfo> peaks,
            PeakInfo targetPeak,
            SNRCalculationSettings settings)
        {
            try
            {
                if (points == null || targetPeak == null)
                    return new SNRResult { Success = false, ErrorMessage = "Invalid input data" };

                double[] times = points.Xvals;
                double[] intensities = points.Yvals;
                int n = times.Length;

                // 1. Calculate Signal
                double signalValue = 0;

                bool useIntegrationRange = settings != null
                                          && settings.IsSignalIntegrationActive
                                          && !double.IsNaN(settings.SignalIntegrationStartTime)
                                          && !double.IsNaN(settings.SignalIntegrationEndTime)
                                          && settings.SignalIntegrationStartTime != settings.SignalIntegrationEndTime;

                if (useIntegrationRange)
                {
                    double start = Math.Min(settings.SignalIntegrationStartTime, settings.SignalIntegrationEndTime);
                    double end = Math.Max(settings.SignalIntegrationStartTime, settings.SignalIntegrationEndTime);

                    if (settings.SignalDefinition == "Area")
                    {
                        signalValue = points.Integrate(start, end);
                    }
                    else if (settings.SignalDefinition == "Height")
                    {
                        double maxIntensity = double.MinValue;
                        for (int i = 0; i < n; i++)
                        {
                            if (times[i] >= start && times[i] <= end)
                            {
                                if (intensities[i] > maxIntensity)
                                    maxIntensity = intensities[i];
                            }
                        }

                        signalValue = maxIntensity == double.MinValue ? 0 : maxIntensity;
                    }
                }
                else if (settings.SignalDefinition == "Height")
                {
                    // For Height: Signal = Intensity - baseline.
                    // In ChromExplorer PeakInfo, Intensity is usually the max intensity of the peak.
                    // If baseline is not available, we use the raw intensity.
                    signalValue = targetPeak.Intensity;
                }
                else if (settings.SignalDefinition == "Area")
                {
                    // EXACTLY as UpdatePeakMarkers() does:
                    // Find the CLOSEST peak index to targetPeak.Position
                    int peakIndex = 0;
                    double minDist = double.MaxValue;
                    for (int i = 0; i < n; i++)
                    {
                        double dist = Math.Abs(times[i] - targetPeak.Position);
                        if (dist < minDist)
                        {
                            minDist = dist;
                            peakIndex = i;
                        }
                    }

                    // Find left valley (start of peak)
                    int leftIndex = peakIndex;
                    while (leftIndex > 0 && intensities[leftIndex - 1] < intensities[leftIndex])
                    {
                        leftIndex--;
                    }

                    // Find right valley (end of peak)
                    int rightIndex = peakIndex;
                    while (rightIndex < n - 1 && intensities[rightIndex + 1] < intensities[rightIndex])
                    {
                        rightIndex++;
                    }

                    // EXACTLY use points.Integrate(x[leftIndex], x[rightIndex]) as UpdatePeakMarkers() does!
                    signalValue = points.Integrate(times[leftIndex], times[rightIndex]);
                }

                // 2. Determine Noise Region
                double noiseValue = 0;
                string noiseRegionUsed = "";

                if (settings != null
                    && settings.NoiseDefinition == "AutoRMS")
                {
                    double noiseStart = Math.Min(settings.AutoRmsNoiseStartTime, settings.AutoRmsNoiseEndTime);
                    double noiseEnd = Math.Max(settings.AutoRmsNoiseStartTime, settings.AutoRmsNoiseEndTime);

                    if (double.IsNaN(noiseStart) || double.IsNaN(noiseEnd) || noiseStart == noiseEnd)
                        return new SNRResult { Success = false, ErrorMessage = "Invalid time range for AutoRMS" };

                    // Ensure the noise window is within chromatogram bounds.
                    double minTime = times != null && times.Length > 0 ? times[0] : double.NaN;
                    double maxTime = times != null && times.Length > 0 ? times[times.Length - 1] : double.NaN;

                    if (double.IsNaN(minTime) || double.IsNaN(maxTime) || maxTime <= minTime)
                        return new SNRResult { Success = false, ErrorMessage = "Invalid chromatogram time axis" };

                    noiseStart = Math.Max(minTime, noiseStart);
                    noiseEnd = Math.Min(maxTime, noiseEnd);

                    // Ensure the window has enough points to compute RMS.
                    int noiseStartIndex = points.GetIndexByX(noiseStart);
                    int noiseEndIndex = points.GetIndexByX(noiseEnd);
                    if (noiseStartIndex < 0 || noiseEndIndex < 0 || noiseEndIndex - noiseStartIndex < 1)
                    {
                        return new SNRResult
                        {
                            Success = false,
                            ErrorMessage = "AutoRMS noise window is too small or outside data range. Adjust Time Range."
                        };
                    }

                    noiseValue = CalculateAutoRmsNoise(times, intensities, noiseStart, noiseEnd);
                    if (noiseValue <= 0)
                    {
                        return new SNRResult
                        {
                            Success = false,
                            ErrorMessage = "AutoRMS noise could not be computed (noise is zero). Adjust Time Range."
                        };
                    }
                    noiseRegionUsed = string.Format("{0:0.00} - {1:0.00} min", noiseStart, noiseEnd);

                    return new SNRResult
                    {
                        Success = true,
                        SignalValue = signalValue,
                        NoiseValue = noiseValue * settings.Multiplier,
                        SNRatio = noiseValue > 0 ? signalValue / (noiseValue * settings.Multiplier) : 0,
                        NoiseRegionUsed = noiseRegionUsed,
                        NoiseRegionStartTime = noiseStart,
                        NoiseRegionEndTime = noiseEnd,
                        NoiseRegionStartIndex = noiseStartIndex,
                        NoiseRegionEndIndex = noiseEndIndex,
                        SignalDefinition = settings.SignalDefinition,
                        NoiseDefinition = settings.NoiseDefinition
                    };
                }

                if (settings.IsAutomaticDetection)
                {
                    var region = FindAutomaticNoiseRegion(times, intensities, peaks, settings.DesiredLength, settings.MinimumLength);
                    if (region != null)
                    {
                        if (settings.NoiseDefinition == "Peak-to-Peak")
                        {
                            noiseValue = CalculatePeakToPeakNoise(intensities, region.StartIndex, region.EndIndex);
                        }
                        else if (settings.NoiseDefinition == "RMS")
                        {
                            noiseValue = CalculateRMSNoise(intensities, region.StartIndex, region.EndIndex);
                        }

                        if (noiseValue <= 0)
                        {
                            return new SNRResult
                            {
                                Success = false,
                                ErrorMessage = "Noise could not be computed (noise is zero). Adjust noise settings or select a valid noise region."
                            };
                        }
                        noiseRegionUsed = string.Format("{0:0.00} - {1:0.00} min", region.StartTime, region.EndTime);
                        return new SNRResult
                        {
                            Success = true,
                            SignalValue = signalValue,
                            NoiseValue = noiseValue * settings.Multiplier,
                            SNRatio = noiseValue > 0 ? signalValue / (noiseValue * settings.Multiplier) : 0,
                            NoiseRegionUsed = noiseRegionUsed,
                            NoiseRegionStartTime = region.StartTime,
                            NoiseRegionEndTime = region.EndTime,
                            NoiseRegionStartIndex = region.StartIndex,
                            NoiseRegionEndIndex = region.EndIndex,
                            SignalDefinition = settings.SignalDefinition,
                            NoiseDefinition = settings.NoiseDefinition
                        };
                    }
                    else
                    {
                        return new SNRResult { Success = false, ErrorMessage = "Could not find a valid automatic noise region" };
                    }
                }
                else
                {
                    var region = ParseManualRegion(times, settings.SpecificTimeRange);
                    if (region != null)
                    {
                        if (settings.NoiseDefinition == "Peak-to-Peak")
                        {
                            noiseValue = CalculatePeakToPeakNoise(intensities, region.StartIndex, region.EndIndex);
                        }
                        else if (settings.NoiseDefinition == "RMS")
                        {
                            noiseValue = CalculateRMSNoise(intensities, region.StartIndex, region.EndIndex);
                        }

                        if (noiseValue <= 0)
                        {
                            return new SNRResult
                            {
                                Success = false,
                                ErrorMessage = "Noise could not be computed (noise is zero). Please enter a valid manual noise region."
                            };
                        }
                        noiseRegionUsed = settings.SpecificTimeRange;
                        double startT = times[region.StartIndex];
                        double endT = times[region.EndIndex];
                        return new SNRResult
                        {
                            Success = true,
                            SignalValue = signalValue,
                            NoiseValue = noiseValue * settings.Multiplier,
                            SNRatio = noiseValue > 0 ? signalValue / (noiseValue * settings.Multiplier) : 0,
                            NoiseRegionUsed = noiseRegionUsed,
                            NoiseRegionStartTime = startT,
                            NoiseRegionEndTime = endT,
                            NoiseRegionStartIndex = region.StartIndex,
                            NoiseRegionEndIndex = region.EndIndex,
                            SignalDefinition = settings.SignalDefinition,
                            NoiseDefinition = settings.NoiseDefinition
                        };
                    }
                    else
                    {
                        return new SNRResult { Success = false, ErrorMessage = "Invalid manual noise region specified" };
                    }
                }
            }
            catch (Exception ex)
            {
                return new SNRResult { Success = false, ErrorMessage = "Error calculating S/N: " + ex.Message };
            }
        }

        private double CalculatePeakToPeakNoise(double[] intensities, int startIndex, int endIndex)
        {
            if (startIndex < 0 || endIndex >= intensities.Length || startIndex >= endIndex)
                return 0;

            double min = double.MaxValue;
            double max = double.MinValue;

            for (int i = startIndex; i <= endIndex; i++)
            {
                if (intensities[i] < min) min = intensities[i];
                if (intensities[i] > max) max = intensities[i];
            }

            return max - min;
        }

        private double CalculateRMSNoise(double[] intensities, int startIndex, int endIndex)
        {
            if (startIndex < 0 || endIndex >= intensities.Length || startIndex >= endIndex)
                return 0;

            double sumOfSquares = 0;
            int count = endIndex - startIndex + 1;

            for (int i = startIndex; i <= endIndex; i++)
            {
                sumOfSquares += intensities[i] * intensities[i];
            }

            return Math.Sqrt(sumOfSquares / count);
        }

        private double CalculateAutoRmsNoise(double[] times, double[] intensities, double startTime, double endTime)
        {
            if (times == null || intensities == null || times.Length != intensities.Length || times.Length < 2)
                return 0;

            double start = Math.Min(startTime, endTime);
            double end = Math.Max(startTime, endTime);

            double sumX = 0;
            double sumY = 0;
            double sumXX = 0;
            double sumXY = 0;
            int count = 0;

            for (int i = 0; i < times.Length; i++)
            {
                if (times[i] >= start && times[i] <= end)
                {
                    double x = times[i];
                    double y = intensities[i];
                    sumX += x;
                    sumY += y;
                    sumXX += x * x;
                    sumXY += x * y;
                    count++;
                }
            }

            if (count < 2)
                return 0;

            double denom = (count * sumXX - sumX * sumX);
            double a;
            double b;

            if (Math.Abs(denom) > 1e-12)
            {
                b = (count * sumXY - sumX * sumY) / denom;
                a = (sumY - b * sumX) / count;
            }
            else
            {
                b = 0;
                a = sumY / count;
            }

            double sumSq = 0;
            int n = 0;
            for (int i = 0; i < times.Length; i++)
            {
                if (times[i] >= start && times[i] <= end)
                {
                    double baseline = a + b * times[i];
                    double resid = intensities[i] - baseline;
                    sumSq += resid * resid;
                    n++;
                }
            }

            if (n == 0)
                return 0;

            return Math.Sqrt(sumSq / n);
        }

        private NoiseRegion FindAutomaticNoiseRegion(double[] times, double[] intensities, List<PeakInfo> peaks, double desiredLen, double minLen)
        {
            // Scanning the chromatogram for the most stable region without peaks
            int n = times.Length;
            if (n < 2) return null;

            // FIX 2: Mark peak zones using HWHM-based boundaries instead of LeftIndex/RightIndex
            // This ensures the full peak tail is excluded from the noise region search,
            // preventing the tail from being incorrectly selected as the noise baseline.
            bool[] inPeak = new bool[n];
            foreach (var peak in peaks)
            {
                double halfBase = peak.HWHM * 2.0;
                double peakStartT = peak.Position - halfBase;
                double peakEndT = peak.Position + halfBase;

                for (int i = 0; i < n; i++)
                {
                    if (times[i] >= peakStartT && times[i] <= peakEndT)
                        inPeak[i] = true;
                }
            }

            NoiseRegion bestRegion = null;
            double lowestFluctuation = double.MaxValue;

            // Sliding window search
            int windowSize = FindIndexCountForDuration(times, desiredLen);
            int minWindowSize = FindIndexCountForDuration(times, minLen);

            // Need at least 2 points in a noise window to compute a meaningful noise value.
            if (minWindowSize < 2)
                return null;

            if (windowSize > n) windowSize = n;
            if (minWindowSize > n) minWindowSize = n;

            for (int i = 0; i <= n - minWindowSize; i++)
            {
                int currentWindowSize = Math.Min(windowSize, n - i);
                if (currentWindowSize < 2)
                    continue;
                bool windowHasPeak = false;

                for (int j = 0; j < currentWindowSize; j++)
                {
                    if (inPeak[i + j])
                    {
                        windowHasPeak = true;
                        break;
                    }
                }

                if (!windowHasPeak)
                {
                    double ptp = CalculatePeakToPeakNoise(intensities, i, i + currentWindowSize - 1);
                    if (ptp < lowestFluctuation)
                    {
                        lowestFluctuation = ptp;
                        bestRegion = new NoiseRegion
                        {
                            StartIndex = i,
                            EndIndex = i + currentWindowSize - 1,
                            StartTime = times[i],
                            EndTime = times[i + currentWindowSize - 1]
                        };
                    }
                }
            }

            return bestRegion;
        }

        private int FindIndexCountForDuration(double[] times, double duration)
        {
            if (times.Length < 2) return 0;
            double avgSampling = (times[times.Length - 1] - times[0]) / (times.Length - 1);
            if (avgSampling <= 0) return 0;
            return (int)Math.Ceiling(duration / avgSampling);
        }

        private NoiseRegion ParseManualRegion(double[] times, string rangeStr)
        {
            try
            {
                // Handle ranges like "0.7-0.85" or "0.7-0.85,0.9-1.0"
                // We'll take the first valid range for simplicity
                string[] ranges = rangeStr.Split(',');
                foreach (string r in ranges)
                {
                    string trimmed = r.Trim();
                    if (string.IsNullOrEmpty(trimmed)) continue;

                    var parts = trimmed.Split('-');
                    if (parts.Length != 2) continue;

                    if (double.TryParse(parts[0], out double start) && double.TryParse(parts[1], out double end))
                    {
                        int startIndex = -1;
                        int endIndex = -1;

                        for (int i = 0; i < times.Length; i++)
                        {
                            if (startIndex == -1 && times[i] >= start) startIndex = i;
                            if (times[i] <= end) endIndex = i;
                        }

                        if (startIndex != -1 && endIndex != -1 && startIndex < endIndex)
                        {
                            return new NoiseRegion
                            {
                                StartIndex = startIndex,
                                EndIndex = endIndex,
                                StartTime = times[startIndex],
                                EndTime = times[endIndex]
                            };
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        private class NoiseRegion
        {
            public int StartIndex { get; set; }
            public int EndIndex { get; set; }
            public double StartTime { get; set; }
            public double EndTime { get; set; }
        }
    }
}