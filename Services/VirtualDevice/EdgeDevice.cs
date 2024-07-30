// Copyright (c) HOREICH GmbH. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Horeich.Services.Runtime;
using Horeich.Services.Diagnostics;

namespace Horeich.Services.VirtualDevice
{
    public interface IEdgeDevice
    {
        public string Id { get; }
        Task SetOnlineStatusAsync(bool online, CancellationToken token);
        Task SendDeviceTelemetryAsync(List<string> telemetry, CancellationToken token);
    }

    public class EdgeDevice : IEdgeDevice, IDisposable
    {
        public string Id { get; }
        public Dictionary<string, string> DeviceInfoProperties { get; set; }
        private ILogger _logger;
        public delegate void SelfDestroyer(object sender, EventArgs ea);
        private Func<object, EventArgs, Task> _timeoutEvent;
        private readonly List<TypeItem> _mapping;
        private readonly TimeSpan _sendInterval;
        private readonly string _deviceKey;
        private System.Threading.Timer _connectionTimeout;
        private bool _disposed;
        private DeviceClient _deviceClient;
        public DeviceUptime _uptime;

        public EdgeDevice(DeviceDataModel model, Func<object, EventArgs, Task> timeoutEvent, ILogger logger)
        {
            _logger = logger;

            _timeoutEvent = timeoutEvent;
            _mapping = model.MappingScheme;
            Id = model.DeviceId;
            _deviceKey = model.DeviceKey;
            _sendInterval = TimeSpan.FromSeconds(model.SendInterval + 200);
            //_properties = model.Properties;
            DeviceInfoProperties = new Dictionary<string, string>();

            _uptime = new DeviceUptime(); // store device uptime
            _disposed = false;

            // Create iot hub device client
            IAuthenticationMethod authMethod = new DeviceAuthenticationWithRegistrySymmetricKey(model.DeviceId, model.DeviceKey);
            _deviceClient = DeviceClient.Create(model.HubConnString, authMethod, Microsoft.Azure.Devices.Client.TransportType.Amqp);
        }

        public async Task OnTimeoutEventAsync()
        {
            Func<object, EventArgs, Task> tempFunc = _timeoutEvent;
            if (tempFunc != null)
            {
                await tempFunc(this, new EventArgs()); // EventArgs currently unused
            }
        }

        private bool UpdateDisconnectTimerAsync()
        {
            // Start disconnect timer
            if (_connectionTimeout == null)
            {
                _connectionTimeout = new System.Threading.Timer(async (obj) =>
                {
                    await OnTimeoutEventAsync();
                    // Do not forget to dispose timer
                }, null, _sendInterval.Seconds, System.Threading.Timeout.Infinite);
                return true;
            }
            else
            {
                return _connectionTimeout.Change(_sendInterval.Seconds, System.Threading.Timeout.Infinite);
            }
        }

        private async Task UpdateDevicePropertiesAsync(Dictionary<string, string> properties, CancellationToken token)
        {
            TwinCollection reportedProperties = new TwinCollection();
            foreach (KeyValuePair<string, string> property in properties)
            {
                reportedProperties[property.Key] = property.Value;
            }
            await _deviceClient.UpdateReportedPropertiesAsync(reportedProperties, token);
        }

        private async Task SendTelemetryAsync(string payload, CancellationToken token)
        {
            // Create message and forward it to iot hub
            using var message = new Message(Encoding.ASCII.GetBytes(payload));

            // Send event but cancel after given timeout (OperationCanceledException)
            await _deviceClient.SendEventAsync(message, token);
        }

        public async Task SetOnlineStatusAsync(bool online, CancellationToken token)
        {
            if (online)
            {
                Dictionary<string, string> properties = DeviceInfoProperties; // add device info properties when connecting
                properties.Add("Status", "online");
                await UpdateDevicePropertiesAsync(properties, token);

                // Updating the properties will set the online status to online.
                // Therefore, we're going to start the disconnect timer immediately.
                UpdateDisconnectTimerAsync();
            }
            else // offline
            {
                Dictionary<string, string> properties = new Dictionary<string, string>();
                properties.Add("Status", "offline");
                await UpdateDevicePropertiesAsync(properties, token);
                await _deviceClient.CloseAsync();
            }
        }

        public async Task SendDeviceTelemetryAsync(List<string> telemetry, CancellationToken token)
        {
            // Assign value to variable names
            Dictionary<string, object> serializeableData = new Dictionary<string, object>();
            for (int i = 0; i < telemetry.Count; ++i)
            {
                // Convert data according to mapping (throws)
                serializeableData.Add(_mapping[i].Id, Convert.ChangeType(telemetry[i], _mapping[i].Type));
            }
            var payload = JsonConvert.SerializeObject(serializeableData, Formatting.Indented);

            // Send via device client (throws)
            await SendTelemetryAsync(payload, token);

            _logger.Debug($"Successfully sent data to IoTHub (device: {Id})");
            
            // Update timout if successfully sent message
            UpdateDisconnectTimerAsync();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            _logger.Debug("Client has been disposed");
            if (!_disposed)
            {
                if (disposing)
                {
                    _deviceClient.Dispose();
                    _connectionTimeout?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}


// private void EnableDeviceTimeout()
//         {
//             // _backgroundClient.Schedule("new", (() => OnTimeoutEventAsync()), _sendInterval);
//             // var id = BackgroundJob.Schedule("new", (() => OnTimeoutEventAsync()), _sendInterval);
// //             var schedule = new Schedule(
// //             async () =>
// //             {
// //                 using var client = new WebClient();
// //                 var content = await client.DownloadStringTaskAsync("http://example.com");
// //                 Console.WriteLine(content);
// //             },
// //             run => run.Now()
// // );
// //             // 
// //             var schedule = new Schedule(
// //                 async () =>
// //                 {
// //                     await OnTimeoutEvent();
// //                 },
// //                 x => x.
            
// //             JobManager.RemoveJob("LEV1");
// //             JobManager.AddJob(() => 
// //                 OnTimeoutEvent(),
// //                 s => s.ToRunOnceIn((int)_sendInterval.TotalSeconds + 120));
//         }