// Copyright (c) Horeich GmbH, all rights reserved

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;

using Horeich.Services.Runtime;
using Horeich.Services.Exceptions;
using Horeich.Services.Diagnostics;
using Horeich.Services.StorageAdapter;
using Microsoft.AspNetCore.Mvc.TagHelpers;

namespace Horeich.Services.VirtualDevice
{
    public interface IEdgeDeviceManager
    {
        Task SendTelemetryAsync(string deviceId, DeviceTelemetry telemetry);
        Task DisconnectDeviceAsync(string deviceId);
    }

    public class EdgeDeviceManager : IEdgeDeviceManager
    {
        // TODO: min allowed update interval for devices
        private readonly ILogger _logger;
        private readonly IDataHandler _dataHandler;
        private readonly IServicesConfig _config;
        private readonly IStorageAdapterClient _storageClient;
        private CancellationTokenSource _cts;

        // As it's a single semaphore the implmentation of IDisposable will be foregone
        private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        private Dictionary<string, EdgeDevice> _devices = new Dictionary<string, EdgeDevice>(); // list of all running virtual sensors

        public EdgeDeviceManager(
            IStorageAdapterClient storageClient,
            IDataHandler dataHandler,
            IServicesConfig config,
            ILogger logger)
        {
            _config = config;

            _cts = new CancellationTokenSource(TimeSpan.FromSeconds(_config.IoTHubTimeout));
            _storageClient = storageClient;
            _dataHandler = dataHandler;
            _logger = logger;

            // var id = BackgroundJob.Schedule("new", (() => OnTimeoutEventAsync()), _sendInterval);

            // RecurringJob.AddOrUpdate("SyncUsers", j => j.Execute(_logger), Cron.Minutely);

            // _backgroundClient = new BackgroundJobClient();

            // RecurringJob.AddOrUpdate("name", () => Console.WriteLine("Hello, {0}!", "world"), Cron.Minutely);
            // var schedule = new Schedule(
            // async () =>
            // {
            //     using var client = new WebClient();
            //     var content = await client.DownloadStringTaskAsync("http://example.com");
            //     Console.WriteLine(content);
            // },
            // run => run.Now()
            // );

            // _edgeDevice.LifespanTimeout += DisposeEdgeDevice;
            // JobManager.Initialize();
        }

        private async Task<DeviceApiModel> LoadDeviceApiModelAsync(string deviceId)
        {
            DeviceApiModel model = new DeviceApiModel();
            model.DeviceId = deviceId;

            // Get device info from storage (throws)
            DevicePropertiesServiceModel deviceModel = await _storageClient.GetDevicePropertiesAsync(deviceId);
            model.SendInterval = deviceModel.SendInterval;
            model.HubConnString = deviceModel.HubId + ".azure-devices.net";
            model.DeviceKey = _dataHandler.GetString(model.DeviceId, string.Empty); // get Device Key from key vault
            if (model.DeviceKey == String.Empty)
            {
                throw new InvalidConfigurationException($"Unable to load configuration value for '{deviceModel.HubId}'");
            }


            // Get mapping from storage (throws)
            // result = await _storageClient.GetDeviceMappingAsync(deviceModel.Type, deviceModel.Version);
            // model.Mapping = new List<Tuple<string, Type>>(result.Mapping.Count);

            // // Convert string to type (TODO: error handling?)
            // for (int i = 0; i < result.Mapping.Count; ++i)
            // {
            //     Type varType = TypeFromString(result.Mapping[i][1]);
            //     model.Mapping.Add(Tuple.Create(result.Mapping[i][0], varType));
            // }
            return model;
        }

        private async Task OnDisconnectDeviceAsync(object sender, EventArgs eventArgs)
        {
            // This is executed in EdgeDevice's context
            // Currently, we do not retry updating offline properties. This should be changed in future releases
            await _semaphore.WaitAsync();
            EdgeDevice edgeDevice = null;
            try
            {
                EdgeDevice ed = (EdgeDevice)sender;
                edgeDevice = _devices[ed.Id]; // element must be in list!

                await edgeDevice.SetOnlineStatusPropertyAsync(false, _cts.Token);
            }
            catch (Exception ex)
            {
                throw new EdgeDeviceException($"Failed to update device offline status on timeout ({edgeDevice?.Id})", ex); 
            }
            finally
            {
                // The edge device will be disposed in any case
                _devices.Remove(edgeDevice.Id);
                edgeDevice?.Dispose();
                _semaphore.Release();
            }
        }

        private async Task<EdgeDevice> ConnectDeviceAsync(string deviceId)
        {
            EdgeDevice edgeDevice = null;
            try
            {
                
                if (!_devices.ContainsKey(deviceId))
                {
                    // Load device model (throws)
                    // DeviceApiModel model = await LoadDeviceApiModelAsync(deviceId);
                    DeviceApiModel model = new DeviceApiModel();
                    model.DeviceId = "LEY3";
                    model.DeviceKey = "ZqNKe0NRtGREbvjhF+Lbe+6Jq2PDOMZDaM7sEPgX5sc=";
                    model.HubConnString = "iotc-2fee92c0-e1fe-4a11-b9ac-0e826cac1889.azure-devices.net";
                    model.SendInterval = 10;
                    model.Type = "levelsense";

                    // Create device and send online status (throws)
                    edgeDevice = new EdgeDevice(model, OnDisconnectDeviceAsync, _logger);

                    // (throws)
                    await edgeDevice.SetOnlineStatusPropertyAsync(true, _cts.Token);

                    _devices.Add(model.DeviceId, edgeDevice);
                }
                else
                {
                    edgeDevice = _devices[deviceId];
                }
                return edgeDevice;
            }
            catch (Exception ex)
            {
                edgeDevice?.Dispose(); // In case of an exception clear all reasources
                throw new EdgeDeviceException($"Failed load device model or update device online status ({deviceId})", ex);
            }
        }

        public async Task DisconnectDeviceAsync(string deviceId)
        {
            await _semaphore.WaitAsync();
            EdgeDevice edgeDevice = null;
            try
            {
                if (!_devices.ContainsKey(deviceId))
                {
                    edgeDevice = _devices[deviceId];

                    await edgeDevice.SetOnlineStatusPropertyAsync(false, _cts.Token);
                    
                    // Only dispose if there is no preceeding exception
                    _devices.Remove(edgeDevice.Id);
                    edgeDevice?.Dispose();
                }
            }
            catch (Exception ex)
            {
                throw new EdgeDeviceException($"Failed to update device offline status ({edgeDevice?.Id})", ex); 
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task SendTelemetryAsync(string deviceId, DeviceTelemetry telemetry)
        {
            await _semaphore.WaitAsync();
            try
            {
                // Setup device connection on first time
                EdgeDevice edgeDevice = await ConnectDeviceAsync(deviceId);

                // Send telemetry to IoT Hub
                await edgeDevice.SendDeviceTelemetryAsync(telemetry.Data, _cts.Token);

                // Debug message
                _logger.Debug(String.Format($"Telemetry of device {deviceId} successfully sent to IoT Hub"));
            }
            catch (Exception ex)
            {
                _logger.Error(String.Format($"Sending telemetry to IoT Hub failed {ex}"));
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}


// Create device and send online status (throws)
// edgeDevice = await EdgeDevice.Create(model, TimeoutAction, _logger, _cts.Token); // TODO own data handler?


// public async Task StartRecurringJobAsync()
//         {
//             // StdSchedulerFactory factory = new StdSchedulerFactory();
//             // IScheduler scheduler = await factory.GetScheduler();

//             // // Start scheduler
//             // await scheduler.Start();

//             // IJobDetail timeoutJob = JobBuilder.Create<TimeoutJob>()
//             //     .WithIdentity("TimeoutJob")
//             //     .Build();

//             // JobDataMap

//             // ITrigger timeoutTrigger = TriggerBuilder.Create()
//             //     .WithIdentity("TimeoutTrigger")
//             //     .StartNow()
//             //     .WithSimpleSchedule(x => x.WithIntervalInSeconds(20).RepeatForever())
//             //     .Build();

//             // await scheduler.ScheduleJob(timeoutJob, timeoutTrigger);

//             // LogProvider.SetCurrentLogProvider(_logger);
//         }