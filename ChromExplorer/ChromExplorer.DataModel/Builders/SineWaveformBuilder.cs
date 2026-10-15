using System;
using System.Collections.Generic;
using ChromExplorer.DataModel.Core;

namespace ChromExplorer.DataModel.Builders
{
    public class SineWaveformBuilder
    {
        public SimpleDataSource CreateDataSource(WaveformConfigBase config)
        {
            return GenerateDS(config);
        }

        public double[] CreateArray(WaveformConfigBase config)
        {
            return GenerateArray(config);
        }

        private double[] GenerateArray(WaveformConfigBase config)
        {
            var yvals = new List<double>(config.Samples);

            var step = config.Frequency / config.SamplingRate * 2 * Math.PI;

            for (var i = 0; i < config.Samples; i++)
            {
               
                yvals.Add(config.Amplitude * Math.Sin(config.Phase + i * step) + config.Offset);
            }

            return yvals.ToArray();
        }

        private static SimpleDataSource GenerateDS(WaveformConfigBase config)
        {
            var xvals = new List<double>(config.Samples);
            var yvals = new List<double>(config.Samples);

            var step = config.Frequency / config.SamplingRate * 2 * Math.PI;
            var dt = 1.0 / config.SamplingRate;
          
            for(var i = 0; i < config.Samples; i++)
            {
                xvals.Add(dt * (double)i);
                yvals.Add( config.Amplitude * Math.Sin(config.Phase + i * step) + config.Offset);
            }

            return new SimpleDataSource(xvals.ToArray(), yvals.ToArray());
        }

    }
}
