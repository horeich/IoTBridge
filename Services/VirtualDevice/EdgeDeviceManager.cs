// Copyright (c) Horeich GmbH, all rights reserved

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Horeich.Services.Runtime;
using Horeich.Services.Exceptions;
using Horeich.Services.Diagnostics;
using Horeich.Services.StorageAdapter;

namespace Horeich.Services.VirtualDevice
{
    public interface IEdgeDeviceManager
    {
        Task SendTelemetryAsync(string deviceId, DeviceTelemetry telemetry);
        Task DisconnectDeviceAsync(string deviceId);
    }

    public class EdgeDeviceManager : IEdgeDeviceManager
    {
        private readonly ILogger _logger;
        private readonly IDataHandler _dataHandler;
        private readonly IStorageAdapterClient _storageClient;
        private readonly CancellationTokenSource _cts;

        // As it's a single semaphore the implmentation of IDisposable will be foregone
        private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        private Dictionary<string, EdgeDevice> _devices = new Dictionary<string, EdgeDevice>(); // list of all running virtual sensors

        public EdgeDeviceManager(
            IStorageAdapterClient storageClient,
            IDataHandler dataHandler,
            IServicesConfig config,
            ILogger logger)
        {
            _cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(config.IoTHubTimeout));
            _storageClient = storageClient;
            _dataHandler = dataHandler;
            _logger = logger;
        }

        protected async Task<DeviceDataModel> LoadDeviceDataModelAsync(string deviceId)
        {
            DeviceDataModel deviceDataModel = new()
            {
                DeviceId = deviceId
            };

            // Get device info from storage (throws)
            DevicePropertiesServiceModel devicePropertiesModel = await _storageClient.GetDevicePropertiesAsync(deviceId);

            // Copy items to device data model
            deviceDataModel.SendInterval = devicePropertiesModel.SendInterval;
            deviceDataModel.HubConnString = devicePropertiesModel.HubId + ".azure-devices.net";
            deviceDataModel.DeviceKey = _dataHandler.GetString(deviceDataModel.DeviceId, string.Empty); // get Device Key from key vault
            if (deviceDataModel.DeviceKey == String.Empty)
            {
                throw new NullReferenceException($"Unable to load configuration value for '{devicePropertiesModel.HubId}'"); // TODO: which exception here?
            }
  
            // Get mapping from storage (throws)
            MappingServiceModel mappingModel = await _storageClient.GetDeviceMappingAsync(devicePropertiesModel.Category, devicePropertiesModel.MappingVersion);
            
            // Copy mapping to device data model
            foreach (MappingItem item in mappingModel.Mapping)
            {
                TypeItem typeItem = new TypeItem 
                { 
                    Id = item.Id, 
                    Type = Type.GetType(item.TypeString, throwOnError:true)
                };
                // throws if conversion fails
                deviceDataModel.MappingScheme.Add(typeItem);
            }
            return deviceDataModel;
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

                await edgeDevice.SetOnlineStatusAsync(false, _cts.Token);
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
                if (!_devices.TryGetValue(deviceId, out edgeDevice))
                {
                    // Load device model (throws)
                    DeviceDataModel model = await LoadDeviceDataModelAsync(deviceId);
                    
                    // Create device and send online status (throws)
                    edgeDevice = new EdgeDevice(model, OnDisconnectDeviceAsync, _logger);

                    // (throws)
                    await edgeDevice.SetOnlineStatusAsync(true, _cts.Token);

                    _devices.Add(model.DeviceId, edgeDevice);
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

                    await edgeDevice.SetOnlineStatusAsync(false, _cts.Token);
                    
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
            if (telemetry == null)
            {
                throw new NullReferenceException();
            }

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