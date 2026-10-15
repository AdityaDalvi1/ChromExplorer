using System;

namespace ChromExplorer.ModasClient
{
    /// <summary>
    /// A single, simple measurement result 
    /// </summary>
    [Serializable]
    public class MTMeasurement
    {
        public MTMeasurement()
        {
            MassChargeRatio = 0.0f;
            Abundance = 0.0f;
        }

        public MTMeasurement(float mz, float abundance)
        {
            MassChargeRatio = mz;
            Abundance = abundance;
        }

        public float MassChargeRatio { get; set; }

        public float Abundance { get; set; }
    }
}