// Copyright (c) HOREICH GmbH. All rights reserved.

using System;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Horeich.Services.Models;
using Horeich.Services.Diagnostics;
using Horeich.Services.Exceptions;

namespace Horeich.Services.EdgeDevice
{
    public interface IEdgeDevice
    {
        public string Id { get; }
        public int TimeoutMs { get; }
        Task SetOnlineStatusAsync(bool online, CancellationToken token);
        Task SendDeviceTelemetryAsync(List<string> telemetry, CancellationToken token);
        void Dispose();
    }

    public sealed class EdgeDevice : IEdgeDevice, IDisposable
    {
        public string Id { get; }
        public int TimeoutMs { get; }
        private ILogger _logger;
        public delegate void SelfDestroyer(object sender, EventArgs ea);
        private Func<object, EventArgs, Task> _timeoutEvent;
        private readonly List<TypeItem> _mapping;
        private readonly string _deviceKey;
        private readonly Dictionary<string, string> _properties;
        private System.Threading.Timer _connectionTimeout;
        private bool _disposed;
        private DeviceClient _deviceClient;
        private bool _isExecuting;

        public EdgeDevice(DeviceDataModel model, Func<object, EventArgs, Task> timeoutEvent, ILogger logger)
        {
            _logger = logger;

            _timeoutEvent = timeoutEvent;
            _mapping = model.MappingScheme;
            Id = model.DeviceId;
            TimeoutMs = model.TimeoutInterval * 1000; // convert to milliseconds

            _deviceKey = model.DeviceKey;
            _properties = model.Properties;
            _disposed = false;
            _isExecuting = false;

            // Create iot hub device client
            IAuthenticationMethod authMethod = new DeviceAuthenticationWithRegistrySymmetricKey(model.DeviceId, model.DeviceKey);
            _deviceClient = DeviceClient.Create(model.HubConnString, authMethod, Microsoft.Azure.Devices.Client.TransportType.Mqtt);

            // _deviceClient.SetRetryPolicy() // set custom retry policy
        }

        public async Task OnTimeoutEventAsync()
        {
            if (!_isExecuting) // never execute more than once
            {
                _isExecuting = true;
                Func<object, EventArgs, Task> tempFunc = _timeoutEvent;
                if (tempFunc != null)
                {
                    await tempFunc(this, new EventArgs()); // EventArgs currently unused
                }
            }
        }

        private bool UpdateDisconnectTimerAsync()
        {
            if (_connectionTimeout == null)
            {
                // Start disconnect timer
                _connectionTimeout = new System.Threading.Timer(async (obj) =>
                {
                    // This timer holds reference to this instance, so it must be disposed
                    await OnTimeoutEventAsync();
                    // Do not forget to dispose timer (call dispose)
                }, null, TimeoutMs, System.Threading.Timeout.Infinite);
                return true;
            }
            else
            {
                // Update disconnect timer
                return _connectionTimeout.Change(TimeoutMs, System.Threading.Timeout.Infinite);
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
            try
            {
                if (online)
                {
                    Dictionary<string, string> properties = _properties; // add device info properties when connecting
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
            catch (Exception ex)
            {
                throw new EdgeDeviceException($"Failed update online status ('{Id}')).", ex);
            }
        }

        public async Task SendDeviceTelemetryAsync(List<string> telemetry, CancellationToken token)
        {
            // Assign value to variable names
            Dictionary<string, object> serializeableData = new Dictionary<string, object>();

            // Throw exception if mapping does not fit the
            if (telemetry.Count > _mapping.Count)
            {
                throw new MappingMismatchException($"Telemetry (Count: {telemetry.Count}) and Mapping (Count: {_mapping.Count}) do not fit.");
            }

            // Map telemetry according to given scheme
            for (int i = 0; i < telemetry.Count; ++i)
            {
                // Convert data according to mapping (throws)
                try
                {
                    serializeableData.Add(_mapping[i].Id, Convert.ChangeType(telemetry[i], _mapping[i].Type));
                }
                catch (FormatException ex)
                {
                    throw new MappingMismatchException($"Telmetry value '{telemetry[i]}' cannot be mapped to given data type '{_mapping[i].Type}'.", ex);
                }
            }
            var payload = JsonConvert.SerializeObject(serializeableData, Formatting.Indented);

            // Send via device client (throws)
            await SendTelemetryAsync(payload, token);
            
            // Update timout if successfully sent message
            UpdateDisconnectTimerAsync();
        }

        ~EdgeDevice()
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
            if (!_disposed)
            {
                if (disposing)
                {
                    _deviceClient.Dispose();
                    _connectionTimeout?.Dispose();
                    _timeoutEvent = null;
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