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

namespace Horeich.Services.VirtualDevice
{
    public class IoTHubValue
    {
        Object Value { get; set; }
        Type Type { get; set; }
    }


    public class EdgeDevice
    {
        public string DeviceId { get; }
        private ILogger _logger;
        public delegate void SelfDestroyer(object sender, EventArgs ea);
        private Func<object, EventArgs, Task> _timeoutEvent;
        private List<Tuple<string, Type>> _mapping = new List<Tuple<string, Type>>();
        private readonly TimeSpan _sendInterval;

        public event SelfDestroyer LifespanTimeout;
        private DeviceClient _client;
        private Dictionary<string, string> _properties;
        public DeviceUptime _uptime;


        public static async Task<EdgeDevice> Create(DeviceApiModel model, Func<object, EventArgs, Task> timeoutEvent, ILogger logger, CancellationToken token)
        {
            EdgeDevice _edgeDevice = new EdgeDevice(model, timeoutEvent, logger); //new EdgeDevice(model, logger);

            try
            {
                // Sends basic properties and online status
                await _edgeDevice.UpdateDevicePropertiesAsync(model.Properties, token);

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
        }

        private EdgeDevice(DeviceApiModel model, Func<object, EventArgs, Task> timeoutEvent, ILogger logger)
        {
            _logger = logger;
            _timeoutEvent = timeoutEvent;
            _mapping = model.Mapping;
            // DeviceId = model.DeviceId;
            // _deviceKey = model.DeviceKey;
            // _hubString = model.HubString;
            // _sendInterval = TimeSpan.FromSeconds(model.SendInterval);
            _properties = model.Properties;
            _properties.Add("Status", "online");

            _uptime = new DeviceUptime(); // store device uptime

            // Create iot hub device client
            IAuthenticationMethod authMethod = new DeviceAuthenticationWithRegistrySymmetricKey(model.DeviceId, model.DeviceKey);
            _client = DeviceClient.Create(model.HubString, authMethod, Microsoft.Azure.Devices.Client.TransportType.Amqp);
        }

        public async Task OnTimeoutEventAsync()
        {
            Func<object, EventArgs, Task> tempAction = _timeoutEvent;
            if (tempAction != null)
            {
                await tempAction(this, new EventArgs());
            }
        }

        private async Task Test1()
        {
            return;
        }
    
        private void EnableDeviceTimeout()
        {
            BackgroundJob.Schedule("new", (() => OnTimeoutEventAsync()), _sendInterval);
//             var schedule = new Schedule(
//             async () =>
//             {
//                 using var client = new WebClient();
//                 var content = await client.DownloadStringTaskAsync("http://example.com");
//                 Console.WriteLine(content);
//             },
//             run => run.Now()
// );
//             // 
//             var schedule = new Schedule(
//                 async () =>
//                 {
//                     await OnTimeoutEvent();
//                 },
//                 x => x.
            
//             JobManager.RemoveJob("LEV1");
//             JobManager.AddJob(() => 
//                 OnTimeoutEvent(),
//                 s => s.ToRunOnceIn((int)_sendInterval.TotalSeconds + 120));
        }

        public void ResetDeviceTimeout()
        {
            _timeoutEvent = null;
            JobManager.RemoveJob("LEV1");
        }

        public async Task<bool> UpdatePropertyAsync(string key, string value, CancellationToken token)
        {    
            _properties[key] = value;
            await UpdateDevicePropertiesAsync(_properties, token);
            return false;
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

        public async Task SendDeviceTelemetryAsync(List<IoTHubValue> telemetry, int timeout)
        {
            // Assign value to variable names
            Dictionary<string, object> serializeableData = new Dictionary<string, object>();
            for (int i = 0; i < telemetry.Count; ++i)
            {
                Type type = _mapping[i].Item2;
                serializeableData.Add(_mapping[i].Item1, Convert.ChangeType(telemetryDataPoints[i], type));
            }
            var payload = JsonConvert.SerializeObject(serializeableData, Formatting.Indented);

            await SendTelemetryAsync(payload, timeout);
        }
    }


}