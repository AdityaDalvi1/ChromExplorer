using Modas.Grpc.Models;
using System;
using System.Collections.Generic;
using System.Data;
using Modas.Remote.Grpc;
using MTIonPolarity = Modas.Remote.Grpc.MTIonPolarity;
using MTMassSpecState = Modas.Grpc.Models.MTMassSpecState;
using MTMeasurement = Modas.Grpc.Models.MTMeasurement;
using MTMeasurementDataSet = Modas.Grpc.Models.MTMeasurementDataSet;
using MTMeasurementDataSetHeader = Modas.Grpc.Models.MTMeasurementDataSetHeader;
using MTMeasurementDataSetType = Modas.Grpc.Models.MTMeasurementDataSetType;
using MTSpectrumType = Modas.Grpc.Models.MTSpectrumType;
using MTSyringeAction = Modas.Remote.Grpc.MTSyringeAction;
using StatusItem = Modas.Grpc.Models.StatusItem;


namespace Modas.Grpc.Client
{
    public static class TypeMapper
    {
        public static List<StatusItem> Map(IList<Remote.Grpc.StatusItem> grpcItems)
        {
            var items = new List<StatusItem>();
            foreach (var grpcItem in grpcItems)
            {
                var item = new StatusItem()
                {
                    Name = grpcItem.Name,
                    Value = grpcItem.Value,
                    Category = grpcItem.Cat
                };
                items.Add(item);
            }
            return items;
        }

        public static MessageSeverity MapEnum(Remote.Grpc.MTMessageSeverity s)
        {
            return (MessageSeverity)s;
        }

        public static ErrorCode MapEnum(Remote.Grpc.MTErrorCodes c)
        {
            return (ErrorCode)c;
        }

        public static RemoteMessage Map(Remote.Grpc.MTMessage msg)
        {
            return new RemoteMessage()
            {
                Error = MapEnum(msg.ErrorCode),
                Exception = msg.Exception,
                Message = msg.Message,
                OccuredAt = msg.OccurredAt.ToDateTime(),
                Severity = MapEnum(msg.Severity)
            };
        }

        public static MTMassSpecState MapEnum(Remote.Grpc.MTMassSpecState state)
        {
            return (MTMassSpecState)state;
        }

        public static Remote.Grpc.MTSpectrumType MapEnum(MTSpectrumType type)
        {
            return (Remote.Grpc.MTSpectrumType)type;
        }

        public static MTMeasurementDataSetType MapEnum(Remote.Grpc.MTMeasurementDataSetType state)
        {
            return (MTMeasurementDataSetType)state;
        }

        public static MTMeasurementDataSet Map(Remote.Grpc.MTMeasurementDataSet data)
        {
            var retval = new MTMeasurementDataSet();

            retval.Type = MapEnum(data.DatasetType);
            retval.TimeStamp = data.OccurredAt.ToDateTime();
            retval.Header = Map(data.Header);
            retval.Measurements = new List<MTMeasurement>();

            if (data.Measurements != null)
            {
                retval.Measurements = new List<MTMeasurement>();

                foreach (var grpcMeasurement in data.Measurements)
                {
                    var measurement = new MTMeasurement()
                    {
                        MassChargeRatio = grpcMeasurement.MassChargeRatio,
                        Abundance = grpcMeasurement.Abundance
                    };
                    retval.Measurements.Add(measurement);
                }
            }

            return retval;
        }

        private static MTMeasurementDataSetHeader Map(Remote.Grpc.MTMeasurementDataSetHeader dataHeader)
        {
            var header = new MTMeasurementDataSetHeader();

            header.ScanNumber = dataHeader.ScanNumber;

            header.RetentionTimeMs = dataHeader.RetentionTimeMs;

            header.LowMass = dataHeader.LowMass;
            header.HighMass = dataHeader.HighMass;

            header.LowMass = dataHeader.LowMass;
            header.MeasurementType = MapEnum(dataHeader.MeasurementType);
            header.MassRange = MapEnum(dataHeader.MassRange);
            header.InjectionTimeMs = dataHeader.InjectionTimeMs;
            header.ParentIon = dataHeader.ParentIon;
            header.FragmentationWidth = dataHeader.FragmentationWidth;
            header.FragmentationEnergy = dataHeader.FragmentationEnergy;
            header.IsolationWidth = dataHeader.IsolationWidth;
            header.MicroscanCount = dataHeader.MicroscanCount;
            header.SpectrumType = MapEnum(dataHeader.SpectrumType);

            return header;
        }

        private static MTSpectrumType MapEnum(Remote.Grpc.MTSpectrumType type)
        {
            return (MTSpectrumType)type;
        }

        private static MTMassRanges MapEnum(Remote.Grpc.MTMassRange range)
        {
            return (MTMassRanges)range;
        }

        private static MTMeasurementTypes MapEnum(Remote.Grpc.MTMeasurementType type)
        {
            return (MTMeasurementTypes)type;
        }

        public static MTSyringeAction MapEnum(Models.MTSyringeAction configAction)
        {
            return (MTSyringeAction)configAction;
        }

        public static MTIonPolarity MapEnum(Models.MTIonPolarity ionPolarityValue)
        {
            return (MTIonPolarity)ionPolarityValue;
        }

        public static MTMeasurementType MapEnum(MTMeasurementTypes measurementTypeValue)
        {
            return (MTMeasurementType)measurementTypeValue;
        }
    }    
}
