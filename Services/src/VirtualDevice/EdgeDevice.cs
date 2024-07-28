using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;

using Horeich.Services.Runtime;
using Horeich.Services.Diagnostics;

using FluentScheduler;
using Hangfire;
using Newtonsoft.Json;
// using Quartz.Impl;
// using Quartz;
// using Quartz.Impl;

namespace Horeich.Services.VirtualDevice
{
    public class IotHubValue
    {
        Object Value { get; set; }
        Type Type { get; set; }
    }

    public interface IEdgeDevice
    {
        public string Id { get; }
        Task SendDeviceTelemetryAsync(List<string> telemetryDataPoints, int timeout);
        Task<bool> UpdateConnectionStatusAsync();
    }

    public class EdgeDevice : IEdgeDevice, IDisposable
    {
        public string Id { get; }
        public Dictionary<string, string> DeviceInfoProperties { get; set; }

        private ILogger _logger;
        public delegate void SelfDestroyer(object sender, EventArgs ea);
        private Func<object, EventArgs, Task> _timeoutEvent;
        private List<Tuple<string, Type>> _mapping = new List<Tuple<string, Type>>();
        private readonly TimeSpan _sendInterval;
        private readonly string _deviceKey;
        private readonly string _hubConnString;
        private System.Threading.Timer _connectionTimeout;

        // private readonly BackgroundJobClient _backgroundClient;
        private bool _disposed;


        public event SelfDestroyer LifespanTimeout;
        private DeviceClient _client;
        public DeviceUptime _uptime;


        public static async Task<EdgeDevice> Create(DeviceApiModel model, Func<object, EventArgs, Task> timeoutEvent, ILogger logger, CancellationToken token)
        {
            EdgeDevice _edgeDevice = new EdgeDevice(model, timeoutEvent, logger); //new EdgeDevice(model, logger);

            try
            {
                // Sends basic properties and online status
                await _edgeDevice.UpdateDevicePropertiesAsync(_edgeDevice.DeviceInfoProperties, token);

                // Enable destroy client on timeout
                _edgeDevice.EnableDeviceTimeout();
            }
            catch (Exception e)
            {
                // Log exception
            }
            finally
            {
                _edgeDevice = null;
            }
            return _edgeDevice;

            // Current JobStorage instance has not been initialized yet. You must set it before using Hangfire Client or Server API. For .NET Core applications please call the `IServiceCollection.AddHangfire` extension method from Hangfire.NetCore or Hangfire.AspNetCore package depending on your application type when configuring the services and ensure service-based APIs are used instead of static ones, like `IBackgroundJobClient` instead of `BackgroundJob` and `IRecurringJobManager` instead of `RecurringJob`.
        }

        public EdgeDevice(DeviceApiModel model, Func<object, EventArgs, Task> timeoutEvent, ILogger logger)
        {
            _logger = logger;

            // StdSchedulerFactory factory = new StdSchedulerFactory();
            // IScheduler scheduler = await factory.GetScheduler();

            // scheduler.Start();

            _timeoutEvent = timeoutEvent;
            _mapping = model.Mapping;
            Id = model.DeviceId;
            _deviceKey = model.DeviceKey;
            _hubConnString = model.HubConnString;
            _sendInterval = TimeSpan.FromSeconds(model.SendInterval);
            //_properties = model.Properties;
            DeviceInfoProperties = new Dictionary<string, string>();
            // Properties.Add("Status", "online");

            _uptime = new DeviceUptime(); // store device uptime

            _disposed = false;

            // Create iot hub device client
            IAuthenticationMethod authMethod = new DeviceAuthenticationWithRegistrySymmetricKey(model.DeviceId, model.DeviceKey);
            _client = DeviceClient.Create(model.HubConnString, authMethod, Microsoft.Azure.Devices.Client.TransportType.Amqp);
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

        public async Task SetOnlineStatusPropertyAsync(bool online, CancellationToken token)
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
            }
        }

        public async Task UpdateDevicePropertiesAsync(Dictionary<string, string> properties, CancellationToken token)
        {
            TwinCollection reportedProperties = new TwinCollection();
            foreach (KeyValuePair<string, string> property in properties)
            {
                reportedProperties[property.Key] = property.Value;
            }
            await _client.UpdateReportedPropertiesAsync(reportedProperties, token);
        }

        public async Task SendDeviceTelemetryAsync(List<string> telemetry, CancellationToken token)
        {
            // Assign value to variable names
            Dictionary<string, object> serializeableData = new Dictionary<string, object>();
            for (int i = 0; i < telemetry.Count; ++i)
            {
                Type type = _mapping[i].Item2;
                serializeableData.Add(_mapping[i].Item1, Convert.ChangeType(telemetry[i], type));
            }
            var payload = JsonConvert.SerializeObject(serializeableData, Formatting.Indented);


            // await SendTelemetryAsync(payload, timeout);


            UpdateDisconnectTimerAsync();
        }

        Task<bool> IEdgeDevice.UpdateConnectionStatusAsync()
        {
            throw new NotImplementedException();
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
            _logger.Debug("Client has been disposed");
            if (!_disposed)
            {
                if (disposing)
                {
                    _client.Dispose();
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