using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
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

using Microsoft.Azure.DigitalTwins.Parser;

namespace Horeich.SensingSolutions.Services.VirtualDevice
{
    public interface IVirtualDevice
    {
        Task SendDeviceTelemetryAsync(List<string> telemetryDataPoints, int timeout);

        Task SendDeviceTelemetryAsync(string binaryTelemetry, int timeout);

        Task<bool> IsActive();
    }
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

        private TwinServiceModel _twinModel;

        public static VirtualDeviceTwin Create(DeviceTwinCredentials credentials, ILogger logger)
        {
            VirtualDeviceTwin device = new VirtualDeviceTwin(credentials, logger);
            return device;
        }

        public VirtualDeviceTwin(DeviceTwinCredentials credentials, ILogger logger)
        {
            _logger = logger;
            IAuthenticationMethod authMethod = new DeviceAuthenticationWithRegistrySymmetricKey(credentials.DeviceId, credentials.DeviceKey);
            _deviceClient = DeviceClient.Create(credentials.HubString, authMethod, Microsoft.Azure.Devices.Client.TransportType.Amqp);
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

        private void Dispose(bool disposing)
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

            await SendTelemetryAsync(payload, timeout);
        }

        private async Task SendTelemetryAsync(string payload, int timeout)
        {
            // Reset timer
            _uptime.Reset();

            // Cancellation token
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(timeout);

            // Create message and forward it to iot hub
            using (var message = new Message(Encoding.ASCII.GetBytes(payload)))
            {
                // Send event but cancel after given timeout (OperationCanceledException)
                Task sendTask = _deviceClient.SendEventAsync(message, cts.Token);

                //_log.Info(string.Format("{0} > Sending telemetry: {1}", DateTime.Now, messageString), () => {});
                await sendTask;
            }
        }
    }
}