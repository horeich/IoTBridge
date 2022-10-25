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

namespace Horeich.SensingSolutions.Services.VirtualDevice
{
    public interface IVirtualDevice
    {
        Task SendDeviceTelemetryAsync(List<string> telemetryDataPoints, int timeout);
        Task<bool> IsActive();
    }
    public class VirtualDevice : IVirtualDevice, IDisposable
    {
        private ILogger _logger;
        private DeviceClient _client;
        private List<Tuple<string, Type>> _mapping = new List<Tuple<string, Type>>();
        private readonly string _deviceId;
        private readonly string _deviceKey;
        private readonly string _hubString;
        private Dictionary<string, string> _properties;
        private readonly TimeSpan _sendInterval;
        public DeviceUptime _uptime;
        private bool _disposed = false;
        private const int propertyUpdateTime = 10000;

        public static async Task<VirtualDevice> Create(DeviceApiModel model, ILogger logger)
        {
            VirtualDevice device = new VirtualDevice(model, logger);
            await device.UpdateDevicePropertiesAsync(model.Properties, propertyUpdateTime);
            return device;
        }

        private VirtualDevice(DeviceApiModel model, ILogger logger)
        {
            _logger = logger;
            _mapping = model.Mapping;
            _deviceId = model.DeviceId;
            _deviceKey = model.DeviceKey;
            _hubString = model.HubString;
            _sendInterval = TimeSpan.FromSeconds(model.SendInterval);
            _properties = model.Properties;
            _properties.Add("Status", "online");

            _uptime = new DeviceUptime();

            // Create iot hub device client
            IAuthenticationMethod authMethod = new DeviceAuthenticationWithRegistrySymmetricKey(model.DeviceId, model.DeviceKey);
            _client = DeviceClient.Create(model.HubString, authMethod, Microsoft.Azure.Devices.Client.TransportType.Amqp);
        }

        ~VirtualDevice()
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
            _logger.Debug("Client has been disposed", () => {});
            if (!_disposed)
            {
                if (disposing)
                {
                    _client.Dispose();
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
            await _client.UpdateReportedPropertiesAsync(reportedProperties, cts.Token);
        }

        public async Task SendDeviceTelemetryAsync(List<string> telemetryDataPoints, int timeout)
        {
            // Assign value to variable names
            Dictionary<string, object> serializeableData = new Dictionary<string, object>();
            for (int i = 0; i < telemetryDataPoints.Count; ++i)
            {
                Type type = _mapping[i].Item2;
                serializeableData.Add(_mapping[i].Item1, Convert.ChangeType(telemetryDataPoints[i], type));
            }
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
                Task sendTask = _client.SendEventAsync(message, cts.Token);

                //_log.Info(string.Format("{0} > Sending telemetry: {1}", DateTime.Now, messageString), () => {});
                await sendTask;
            }
        }
    }
}