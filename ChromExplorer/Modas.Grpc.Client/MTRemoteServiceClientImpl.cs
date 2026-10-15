using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Modas.Grpc.Client.Shared;
using Modas.Grpc.Models;
using Modas.Remote.Grpc;
using Serilog;
using System;
using System.Globalization;
using System.Runtime.Remoting.Contexts;
using System.Threading;
using System.Threading.Tasks;
using IonSourceConfig = Modas.Grpc.Models.IonSourceConfig;
using MethodOverrides = Modas.Grpc.Models.MethodOverrides;
using MTMassSpecState = Modas.Grpc.Models.MTMassSpecState;
using SyringeConfig = Modas.Grpc.Models.SyringeConfig;

namespace Modas.Grpc.Client
{
    public class MTRemoteServiceClientImpl : Modas.Remote.Grpc.MTRemoteService.MTRemoteServiceClient, IModasRemoteClient, IDisposable
    {
        private readonly CallOptions _callOptions = new CallOptions().WithWaitForReady(true);
        private CancellationTokenSource _statusFeedCts = new CancellationTokenSource();
        private CancellationTokenSource _callsCts = new CancellationTokenSource();
        private CancellationTokenSource _measCts = new CancellationTokenSource();
        private readonly Channel _channel = null;
        private string _clientId = "";

        public MTRemoteServiceClientImpl(Channel channel) : base(channel)
        {
            _channel = channel;

        }

        

        public void SetClientId(string clientId)
        {
            _clientId = clientId;
        }

        public ClientInfo ClientInfo =>
            new ClientInfo()
            {
                ClientId = _clientId,
                OccurredAt = Timestamp.FromDateTime(DateTime.Now.ToUniversalTime()),
            };

        public static Timestamp Timestamp =>
            Timestamp.FromDateTime(DateTime.Now.ToUniversalTime());

        public event EventHandler<StatusReceivedEventArgs> StatusReceived;
        public event EventHandler<MeasurementsReceivedEventArgs> MeasurementReceived;
        public event EventHandler MeasurementsCompleted;

        public async Task<Modas.Grpc.Models.RequestControlStatus> RequestControl()
        {
            Log.Logger.Debug("Requesting control");
            var resp = new RequestControlStatus("", "", false);

            try
            {
                var reply = await RequestControlAsync(new RequestControlRequest() { OccurredAt = Timestamp.FromDateTime(DateTime.Now.ToUniversalTime()) }, _callOptions);

                SetClientId(reply.ClientId);
                
                resp = new RequestControlStatus(reply.MsSerialNumber, reply.SoftwareVersion, reply.IsAccessGranted);

                return await Task.FromResult(resp);
            }
            catch (Exception e)
            {
                Log.Logger.Warning(e, "Exception occurred in RequestControl");
            }

            return await Task.FromResult(resp);
        }

        public async Task<string> Ping()
        {
            string retval = string.Empty;
            try
            {
                var reply = await PingAsync(new PingRequest() { OccurredAt = MTRemoteServiceClientImpl.Timestamp }, _callOptions);
                retval = reply.OccurredAt.ToDateTime().ToString(CultureInfo.InvariantCulture);
            }
            catch (Exception e)
            {
                Log.Logger.Warning(e, "Exception occurred in Ping");
            }

            return await Task.FromResult(retval);
        }

        public async Task UpdateStatusFeed(int intervalMs = 5000)
        {
            try
            {
                var statusStream
                    = SetDeviceDataReadbackEnabled(new DeviceDataReadbackSettings() { ClientInfo = ClientInfo, IntervalMsec = 1000 }
                        , new CallOptions().WithCancellationToken(_statusFeedCts.Token));


                while (await statusStream.ResponseStream.MoveNext())
                {
                    var response = statusStream.ResponseStream.Current;
                    var items = TypeMapper.Map(response.StatusItems);
                    var state = TypeMapper.MapEnum(response.State);
                    var ionSourceId = response.IonSourceId;

                    var args = new StatusReceivedEventArgs()
                        { OccuredAt = response.OccurredAt.ToDateTime(), StatusItems = items, State = state, IonSourceID = ionSourceId };

                    OnStatusReceived(args);
                }

            }
            catch (RpcException ex)
            {
                // Handle gRPC-specific errors like deadlines or cancellation
                Log.Logger.Warning("UpdateStatusFeed: RPC error:  " + ex.Status.Detail);
            }
            catch (Exception e)
            {
                Log.Logger.Warning("UpdateStatusFeed: RPC error: " + e.Message);
            }
        }

        public void StopStatusFeed()
        {
            if (_statusFeedCts == null)
                return;

            ResetStatusTokenSource();


        }

        public async Task<RemoteMessage> Reset()
        {
            try
            {
                ResetMeasTokenSource();

                var reply = await ResetErrorStateAsync(ClientInfo, _callOptions);
                return await Task.FromResult(TypeMapper.Map(reply));

            }
            catch (Exception e)
            {
                Log.Logger.Warning(e, "Exception on OnReset");
            }

            return await Task.FromResult(new RemoteMessage());
        }

        public async Task<RemoteMessage> ToPowerOff()
        {
            var msg = new RemoteMessage();
            try
            {
                var reply = await SetToPowerOffModeAsync(ClientInfo, _callOptions);
                msg = TypeMapper.Map(reply);
            }
            catch (Exception e)
            {
                Log.Logger.Warning(e, "Exception occurred in OnOff");
            }

            return await Task.FromResult(msg);
        }

        public async Task<RemoteMessage> ToStandby()
        {
            var msg = new RemoteMessage();
            try
            {
                var reply = await SetToStandbyModeAsync(ClientInfo, _callOptions);
                msg = TypeMapper.Map(reply);
            }
            catch (Exception e)
            {
                Log.Logger.Warning(e, "Exception occurred in ToStandby");
            }

            return await Task.FromResult(msg);
        }

        public async Task<RemoteMessage> ToReady()
        {
            var msg = new RemoteMessage();
            try
            {
                var reply = await SetToOperationalModeAsync(ClientInfo, _callOptions);
                msg = TypeMapper.Map(reply);
            }
            catch (Exception e)
            {
                Log.Logger.Warning(e, "Exception occurred in ToStandby");
            }

            return await Task.FromResult(msg);
        }

        public async Task<MTMassSpecState> GetInstrumentState()
        {
            var state = MTMassSpecState.Unknown;
            try
            {
                var reply = await GetInstrumentStateAsync(ClientInfo, _callOptions);
                state = TypeMapper.MapEnum(reply.State);
            }
            catch (Exception e)
            {
                Log.Logger.Warning(e, "Exception in GetInstrumentState");
            }

            return await Task.FromResult(state);
        }

        public async Task<string> GetIonSourceInfo()
        {
            string ionSourceID = "None";

            try
            {
                var reply = await GetIonSourceInfoAsync(ClientInfo, _callOptions);
                ionSourceID = reply.IonSourceId;
                    ;
            }
            catch (Exception e)
            {
                Log.Logger.Warning(e, "Exception in GetIonSourceInfo");
            }

            return await Task.FromResult(ionSourceID);
        }

        public async Task StartMeasurements(AcquisitionConfig config)
        {
            ResetMeasTokenSource();

            var type = TypeMapper.MapEnum(config.Type);

            var msg = new StartMeasurementsRequest()
            {
                ClientInfo = ClientInfo,
                ScanCount = config.ScanCount,
                ScanDurationSec = config.ScanDurationSec,
                Type = type,
                Filename = config.Filename,
                Category = config.Category ?? "",
                SampleDescription = config.SampleDescription ?? "",
                SampleReference = config.SampleReference ?? ""

            };

            try
            {
                var measStream
                    = StartMeasurementStream(msg, new CallOptions().WithCancellationToken(_measCts.Token));
               
                while (await measStream.ResponseStream.MoveNext())
                {
                    var response = measStream.ResponseStream.Current;
                    var args = new MeasurementsReceivedEventArgs() { Dataset = TypeMapper.Map(response) };
                    OnMeasurementReceived(args);
                }

                OnMeasurementsCompleted();
            }
            catch (RpcException ex)
            {
                // Handle gRPC-specific errors like deadlines or cancellation
                Log.Logger.Warning("RPC error: " + ex.Status.Detail);
            }
            catch (Exception e)
            {
                Log.Logger.Warning("RPC error: " + e.Message);
            }
        }


        public void StopMeasurements()
        {
            ResetMeasTokenSource();
        }

        public async Task<RemoteMessage> SyringeControl(SyringeConfig config)
        {
            var msg = new Remote.Grpc.SyringeConfig();

            msg.ClientInfo = ClientInfo;
            msg.Volume = config.Volume;
            msg.Diameter = config.Diameter;
            msg.Rate = config.Rate;
            msg.Action = TypeMapper.MapEnum(config.Action);

            var rmsg = new RemoteMessage();
            try
            {
                var reply = await SyringeControlAsync(msg, _callOptions);
                rmsg = TypeMapper.Map(reply);
            }
            catch (Exception e)
            {
                Log.Logger.Warning(e, "Exception occurred in SyringeControl");
            }

            return await Task.FromResult(rmsg);
        }

        public async Task<RemoteMessage> UpdateSprayVoltage(IonSourceConfig config)
        {
            var rmsg = new RemoteMessage();
            try
            {
                var msg = new Remote.Grpc.IonSourceConfig()
                {
                    ClientInfo = ClientInfo,
                    SprayVoltage = config.SprayVoltage
                };

                var reply = await SetupIonSourceAsync(msg, _callOptions);


            }
            catch (Exception e)
            {
                Log.Logger.Warning(e, "Exception on OnReset");
            }

            return await Task.FromResult(rmsg);
        }

        public async Task<RemoteMessage> UpdateDsapHeater(IonSourceConfig config)
        {
            var rmsg = new RemoteMessage();
            try
            {
                var msg = new Remote.Grpc.IonSourceConfig()
                {
                    ClientInfo = ClientInfo,
                    
                };

                msg.DsapConfig = new Remote.Grpc.DsapHeaterConfig()
                {
                    DsapPower = config.DsapHeaterConfig.DsapPower,
                    DsapEnabled = config.DsapHeaterConfig.Enabled
                };

                var reply = await SetupIonSourceAsync(msg, _callOptions);
            }
            catch (Exception e)
            {
                Log.Logger.Warning(e, "Exception on UpdateDsapHeater");
            }

            return await Task.FromResult(rmsg);
        }

        public async Task<RemoteMessage> LoadMethodFile(string fileName)
        {
            var rmsg = new RemoteMessage();

            try
            {
                var msg = new Remote.Grpc.MethodConfig()
                {
                    ClientInfo = ClientInfo,
                    MethodFile = fileName
                };

                var reply = await LoadMethodAsync(msg, _callOptions);
                rmsg = TypeMapper.Map(reply);
            }
            catch (Exception e)
            {
                Log.Logger.Warning(e, "Exception on LoadMethodFile");
            }

            return await Task.FromResult(rmsg);
        }

        public async Task<RemoteMessage> ApplyMethodOverrides(MethodOverrides overrides)
        {
            var rmsg = new RemoteMessage();

            try
            {
                var msg = new Remote.Grpc.MethodOverrides()
                {
                    ClientInfo = ClientInfo,

                };

                if(overrides.IonPolarity.IsVisible && overrides.IonPolarity.Override)
                {
                    msg.IonPolarity = TypeMapper.MapEnum(overrides.IonPolarity.Value);
                }

                if (overrides.MeasurementType.IsVisible && overrides.MeasurementType.Override)
                {
                    msg.MeasType = TypeMapper.MapEnum(overrides.MeasurementType.Value);
                }

                if(overrides.ParentIonMz.IsVisible && overrides.ParentIonMz.Override)
                {
                    msg.ParentIon = new ParentIonInfo()
                    {
                        ParentIon = overrides.ParentIonMz.Value,
                        FragmentationAmplitude = overrides.FragmentationAmplitude.Value
                    };
                }

                if (overrides.SecondaryParentIonMz.IsVisible && overrides.SecondaryParentIonMz.Override)
                {
                    msg.SecondaryParentIon= new ParentIonInfo()
                    {
                        ParentIon = overrides.SecondaryParentIonMz.Value,
                        FragmentationAmplitude = overrides.SecondaryFragmentationAmplitude.Value
                    };
                }

                if (overrides.InjectionTimeMs.IsVisible && overrides.InjectionTimeMs.Override)
                {
                    msg.InjectionTime = overrides.InjectionTimeMs.Value / 1000.0;
                }

                if (overrides.MFrom.Override && overrides.MTo.Override)
                {
                    msg.MFrom = overrides.MFrom.Value;
                    msg.MTo = overrides.MTo.Value;
                }

                if (overrides.AgcTarget.Override)
                {
                    msg.AgcSettings = new AgcInfo();
                    msg.AgcSettings.AgcTarget = overrides.AgcTarget.Value;

                    if(overrides.AgcTarget.Value > 0)
                        msg.AgcSettings.AgcEnabled = true;
                    else
                        msg.AgcSettings.AgcEnabled = false;
                }

                if (overrides.MicroscanCount.Override)
                {
                    msg.MicroscanCount = overrides.MicroscanCount.Value;
                }



                var reply = await ApplyMethodOverridesAsync(msg, _callOptions);
                rmsg = TypeMapper.Map(reply);
            }
            catch (Exception e)
            {
                Log.Logger.Warning(e, "Exception on ApplyMethodOverrides");
            }



            return await Task.FromResult(rmsg);
        }


        protected virtual void OnStatusReceived(StatusReceivedEventArgs e)
        {
            StatusReceived?.Invoke(this, e);
        }

        private void ResetStatusTokenSource()
        {
            _statusFeedCts.Cancel();
            _statusFeedCts.Dispose();
            _statusFeedCts = new CancellationTokenSource();
        }

        private void ResetMeasTokenSource()
        {
            _measCts.Cancel();
            _measCts.Dispose();
            _measCts = new CancellationTokenSource();
        }

        protected virtual void OnMeasurementReceived(MeasurementsReceivedEventArgs e)
        {
            MeasurementReceived?.Invoke(this, e);
        }

        public void Dispose()
        {
            _statusFeedCts?.Cancel();
            _callsCts?.Cancel();
            _measCts?.Cancel();
        }

        protected virtual void OnMeasurementsCompleted()
        {
            MeasurementsCompleted?.Invoke(this, EventArgs.Empty);
        }
    }    
}