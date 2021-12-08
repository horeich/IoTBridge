using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

using System.Text;
using Newtonsoft.Json;
using System.Threading;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;

using Horeich.SensingSolutions.Services.Runtime;
using Horeich.SensingSolutions.Services.Exceptions;
using Horeich.SensingSolutions.Services.Http;
using Horeich.SensingSolutions.Services.Diagnostics;

using Horeich.Services.Models;

using Newtonsoft.Json.Linq;


using Microsoft.Azure.DigitalTwins.Parser;

namespace Horeich.SensingSolutions.Services.VirtualDevice
{
    public class VirtualDeviceTwin : IVirtualDevice, IDisposable
    {
        private ILogger _logger;
        private DeviceClient _deviceClient;

        // private TelemetryMapping _telemetryMapping;
        private List<Tuple<string, Type>> _mapping = new List<Tuple<string, Type>>();
        private readonly string _deviceId;
        private readonly string _deviceKey;
        private readonly string _hubString;
        private Dictionary<string, string> _properties;
        private readonly TimeSpan _sendInterval;
        public DeviceUptime _uptime;
        private bool _disposed = false;
        private const int propertyUpdateTime = 10000;

        private ConcurrentDictionary<String, Object> _downlinkProperties = new ConcurrentDictionary<String, Object>();

        private TwinServiceModel _twinModel;

        public static VirtualDeviceTwin Create(DeviceTwinCredentials credentials, ILogger logger)
        {
            VirtualDeviceTwin virtualDevice = new VirtualDeviceTwin(credentials, logger);
            return virtualDevice;
        }

        private void OnConnectionStatusChange(ConnectionStatus status, ConnectionStatusChangeReason reason)
        {
            _logger.Info(string.Format("Connection status changed to {0}, because {1}", status, reason), () => {});
        }

        private async Task OnDesiredPropertyChanged(TwinCollection desiredProperties, object userContext)
        {
            // TODO: Try-catch
            foreach (KeyValuePair<string, object> desiredProperty in desiredProperties)
            {
                // Adds value to properties or updates existing value
                // see https://docs.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentdictionary-2.addorupdate?redirectedfrom=MSDN&view=net-6.0#overloads
                _downlinkProperties.AddOrUpdate(desiredProperty.Key, desiredProperty.Value, (oldkey, oldvalue) => desiredProperty.Value);
            }
        }

        public PropertyServiceModel GetDownlinkProperties()
        {
            PropertyServiceModel propertyServiceModel = new PropertyServiceModel();

            foreach (KeyValuePair<string, object> downlinkProperty in _downlinkProperties)
            {
                object propertyValue;
                if (_downlinkProperties.TryRemove(downlinkProperty.Key, out propertyValue)) // remove property from pending downlink list
                {
                    propertyServiceModel.Properties[downlinkProperty.Key] = propertyValue;
                }
            }

            return propertyServiceModel;
        }

        public VirtualDeviceTwin(DeviceTwinCredentials credentials, ILogger logger)
        {
            _logger = logger;            
        }

        public VirtualDeviceTwin(ILogger logger)
        {
            _logger = logger;            
        }

        public async Task ConnectDevice(DeviceTwinCredentials credentials)
        {
            IAuthenticationMethod authMethod = new DeviceAuthenticationWithRegistrySymmetricKey(credentials.DeviceId, credentials.DeviceKey);
            _deviceClient = DeviceClient.Create(credentials.HubString, authMethod, Microsoft.Azure.Devices.Client.TransportType.Amqp);

            _deviceClient.SetConnectionStatusChangesHandler(OnConnectionStatusChange);

            await _deviceClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertyChanged, null);

            await _deviceClient.OpenAsync();

            await RetrieveDeviceTwin();
        }

        public async Task CloseDevice()
        {
            await _deviceClient.CloseAsync();
        }

        public static VirtualDeviceTwin Create(TwinServiceModel model, ILogger logger)
        {
            VirtualDeviceTwin device = new VirtualDeviceTwin(model, logger);
            
            return device;
        }
        public static async Task<VirtualDeviceTwin> Create(DeviceApiModel model, ILogger logger)
        {
            VirtualDeviceTwin device = new VirtualDeviceTwin(model, logger);
            //await device.UpdateDevicePropertiesAsync(model.Properties, propertyUpdateTime);
            return device;
        }

        public VirtualDeviceTwin(TwinServiceModel model, ILogger logger)
        {
            _logger = logger;
            _twinModel = model;
        }

        private VirtualDeviceTwin(DeviceApiModel model, ILogger logger)
        {
            _logger = logger;
            _mapping = model.Mapping;
            _deviceId = "LEV2";
            _deviceKey = "bWyXou7mwmlzyWj1Ftr/+QOzfZQt+utorCwBLwqEob8=";
            _hubString = "HHub.azure-devices.net";//;SharedAccessKeyName=iothubowner;SharedAccessKey=JM6aRNjmVDQUUQSzF/K1sngKqa39Rm5ZTTaHQ7QMcyg=";
            _sendInterval = TimeSpan.FromSeconds(model.SendInterval);
            _properties = model.Properties;
            _properties.Add("Status", "online");

            _uptime = new DeviceUptime();

            // Create iot hub device client
            IAuthenticationMethod authMethod = new DeviceAuthenticationWithRegistrySymmetricKey(_deviceId, _deviceKey);
            _deviceClient = DeviceClient.Create(_hubString, authMethod, Microsoft.Azure.Devices.Client.TransportType.Amqp);
            // _deviceClient.OpenAsync()
        }

        ~VirtualDeviceTwin()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            _logger.Debug("Client has been disposed", () => { });
            if (!_disposed)
            {
                if (disposing)
                {
                    _deviceClient.Dispose();
                }
                _disposed = true;
            }
        }

        public async Task<bool> IsActive()
        {
            TimeSpan test = _uptime.Duration;
            if (TimeSpan.Compare(test, _sendInterval) > 0)
            {
                _properties["Status"] = "offline";
                await UpdateDevicePropertiesAsync(_properties, propertyUpdateTime);
                return false;
            }
            return true;
        }

        public async Task RetrieveDeviceTwin()
        {
            Twin deviceTwin = await _deviceClient.GetTwinAsync();
            TwinCollection desiredProperties = deviceTwin.Properties.Desired;

            await OnDesiredPropertyChanged(desiredProperties, null);
        }

        public async Task SyncDeviceFunctionsAsync(TelemetryApiModel model)
        {
            // Find out wheter telemetry is property or telemetry
            // TODO: rename telemetry in paypload

            Twin deviceTwin = await _deviceClient.GetTwinAsync();
            TwinCollection desiredProperties = deviceTwin.Properties.Desired; // User-changeable properties

            // TODO: what happens if property does not exist??

            TwinCollection updatedProperties = new TwinCollection();

            // Handle properties
            foreach (KeyValuePair<String, List<Object>> payloadValue in model.Telemetry)
            {
                if (desiredProperties.Contains(payloadValue.Key))
                {
                    Type propertyType = desiredProperties.GetType(); 
                    
                    // TODO: type matching?
                    Type type = TypeFromString(payloadValue.Value[1].ToString());
                    updatedProperties[payloadValue.Key] = Convert.ChangeType(payloadValue.Value[0], propertyType);
                }
            }

            foreach (KeyValuePair<String, Object> updatedProperty in updatedProperties)
            {
                if (model.Telemetry.ContainsKey(updatedProperty.Key))
                {
                    model.Telemetry.Remove(updatedProperty.Key);
                }
            }

            await SyncDevicePropertiesAsync(updatedProperties);
            
            // Handle telemetry

            DateTime timeStamp = DateTime.UtcNow;

            Dictionary<string, object> serializeableData = new Dictionary<string, object>();
            foreach (KeyValuePair<String, List<Object>> value in model.Telemetry)
            {
                // TODO: TypeFromString
                if (value.Key == "Timestamp")
                {
                    Type t = TypeFromString(value.Value[1].ToString());
                    long unixTimeStamp = (long)Convert.ChangeType(value.Value[0], t);
                    DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(unixTimeStamp);
                    timeStamp = dateTimeOffset.UtcDateTime;
                }

                Type type = TypeFromString(value.Value[1].ToString());
                serializeableData.Add(value.Key, Convert.ChangeType(value.Value[0], type));
            }

            var telemetry = JsonConvert.SerializeObject(serializeableData, Formatting.Indented);
            await SendTelemetryAsync(telemetry, timeStamp, 4000);
        }

        private async Task SyncDevicePropertiesAsync(TwinCollection properties)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(4000);
            await _deviceClient.UpdateReportedPropertiesAsync(properties, cts.Token);
        }

        private async Task SendTelemetryAsync(string payload, DateTime timeStamp, int timeout)
        {
            // Reset timer
            _uptime.Reset();

            // Cancellation token
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(timeout);

            // Create message and forward it to iot hub
            using (var message = new Message(Encoding.ASCII.GetBytes(payload))) // TODO: UTF8?
            {
                // Send event but cancel after given timeout (OperationCanceledException)
                message.Properties.Add("iothub-creation-time-utc", timeStamp.ToString());
                
                Task sendTask = _deviceClient.SendEventAsync(message, cts.Token);
                //_log.Info(string.Format("{0} > Sending telemetry: {1}", DateTime.Now, messageString), () => {});
                await sendTask;
            }
        }

        private Type TypeFromString(string dataType)
        {
            if (String.Compare(dataType, "INTEGER") == 0)
            {
                return typeof(int);
            }
            else if (String.Compare(dataType, "BOOL") == 0)
            {
                return typeof(bool);
            }
            else if (String.Compare(dataType, "DOUBLE") == 0)
            {
                return typeof(double);
            }
            else if (String.Compare(dataType, "STRING") == 0)
            {
                return typeof(string);
            }
            else if (String.Compare(dataType, "FLOAT") == 0)
            {
                return typeof(float);
            }
            else
            {
                // Unknown payload type
                throw new DevicePayloadTypeException("data type not found");
            }
        }



        public async Task SendDeviceTelemetryAsync(List<string> telemetryDataPoints, int timeout)
        {
            // Assign value to variable names
            Dictionary<string, object> serializeableData = new Dictionary<string, object>();

            // Thingsboard requires extra device id string
            serializeableData.Add("deviceId", _deviceId);
            serializeableData.Add("temp", 5);
            // for (int i = 0; i < telemetryDataPoints.Count; ++i)
            // {
            //     Type type = _mapping[i].Item2;

                 //serializeableData.Add(_mapping[i].Item1, Convert.ChangeType(telemetryDataPoints[i], type));
            // }

            var payload = JsonConvert.SerializeObject(serializeableData, Formatting.Indented);

            // await SendTelemetryAsync(payload, timeout);
        }

       


        
        public async Task SendDeviceTelemetryAsync(string telemetryDataPoints, int timeout)
        {
            // foreach (var item in _twinModel.TelemetryMapping)
            // {
            //     // TODO: ArgumentOutOfRangeException
            //     String hexStringValue = telemetryDataPoints.Substring(i*8, 8);
            //     UInt32 byteValue = uint.Parse(hexStringValue, System.Globalization.NumberStyles.AllowHexSpecifier);
            //     Byte[] binValue = BitConverter.GetBytes(byteValue);

            //     switch (item.Value)
            //     {
            //         case DTEntityKind.Float:
            //             float value = BitConverter.ToSingle(binValue);
            //             _logger.Debug(String.Format("Decimal value {0}", value), () => {});
            //             break;
            //         case DTEntityKind.Integer:
            //             Int32 value1 = BitConverter.ToInt32(binValue);
            //             _logger.Debug(String.Format("Decimal value {0}", value1), () => {});
            //             break;
            //     }
            // }
        }

        private async Task UpdateDevicePropertiesAsync(Dictionary<string, string> propertyDataPoints, int timeout)
        {
            TwinCollection reportedProperties = new TwinCollection();
            foreach (KeyValuePair<string, string> property in propertyDataPoints)
            {
                reportedProperties[property.Key] = property.Value;
            }

            CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(timeout);
            await _deviceClient.UpdateReportedPropertiesAsync(reportedProperties, cts.Token);

            // Twin twin = await _deviceClient.
        }

    }
}