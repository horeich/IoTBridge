// Copyright (c) HOREICH GmbH, all rights reserved

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Horeich.Services.Runtime;
using Horeich.Services.Exceptions;
using Horeich.Services.Diagnostics;
using Horeich.Services.StorageAdapter;
using Horeich.Services.Models;

namespace Horeich.Services.EdgeDevice
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
        private readonly IEdgeDeviceFactory<IEdgeDevice> _edgeDeviceFactory;
        private readonly IStorageAdapterClient _storageClient;
        private readonly TimeSpan _timeout;

        // As it's a single semaphore the implmentation of IDisposable will be foregone
        private SemaphoreSlim _semaphore;
        private Dictionary<string, IEdgeDevice> _devices = []; // list of all running virtual sensors

        public EdgeDeviceManager(
            IEdgeDeviceFactory<IEdgeDevice> edgeDeviceFactory,
            IStorageAdapterClient storageClient,
            IDataHandler dataHandler,
            IServicesConfig config,
            ILogger logger)
        {
            _dataHandler = dataHandler;
            _logger = logger;
            
            _edgeDeviceFactory = edgeDeviceFactory;
            _storageClient = storageClient;
            _timeout = TimeSpan.FromMilliseconds((double)config.DeviceClientTimeout);

            _semaphore = new SemaphoreSlim(1, 1);
        }

        protected async Task<DeviceDataModel> LoadDeviceDataModelAsync(string deviceId)
        {
            DeviceDataModel deviceDataModel = new()
            {
                DeviceId = deviceId
            };

            // Get device info from storage (throws), HTTP timeout is set to 30s
            DeviceDataSerivceModel devicePropertiesModel = await _storageClient.GetDevicePropertiesAsync(deviceId);

            // Copy items to device data model
            deviceDataModel.TimeoutInterval = devicePropertiesModel.TimeoutInterval;
            deviceDataModel.HubConnString = devicePropertiesModel.HubId + ".azure-devices.net";
            deviceDataModel.Properties = devicePropertiesModel.Properties;
            deviceDataModel.DeviceKey = _dataHandler.GetString(deviceDataModel.DeviceId, string.Empty); // get Device Key from key vault
            if (deviceDataModel.DeviceKey == String.Empty)
            {
                throw new NullReferenceException($"Unable to load configuration value for '{devicePropertiesModel.HubId}'"); // TODO: which exception here?
            }
    
            // Get mapping from storage (throws)
            MappingServiceModel mappingModel = await _storageClient.GetDeviceMappingAsync(devicePropertiesModel.Category, devicePropertiesModel.MappingVersion);
            
            // Copy mapping to device data model
            deviceDataModel.MappingScheme = [];
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
            IEdgeDevice edgeDevice = null;
            try
            {
                IEdgeDevice ed = (IEdgeDevice)sender;
                edgeDevice = _devices[ed.Id]; // element must be in list!
                _devices.Remove(edgeDevice.Id);

                using CancellationTokenSource cts = new(_timeout);
                await edgeDevice.SetOnlineStatusAsync(false, cts.Token);

                _logger.Info($"Successfully disconnected device '{edgeDevice.Id}' due to timeout ({edgeDevice.TimeoutMs/1000}s)");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to disconnect device '{edgeDevice?.Id}' on timeout, Exception: {ex}");
            }
            finally
            {
                // The edge device will be disposed in any case
                // However if called twice, the device might already been removed from the 
                edgeDevice?.Dispose();
                _semaphore.Release();
            }
        }

        private async Task<IEdgeDevice> ConnectDeviceAsync(string deviceId)
        {
            IEdgeDevice edgeDevice = null;
            try
            {
                if (!_devices.TryGetValue(deviceId, out edgeDevice))
                {
                    // Load device model (throws)
                    DeviceDataModel model = await LoadDeviceDataModelAsync(deviceId);
                    
                    // Create device and send online status (throws)
                    edgeDevice = _edgeDeviceFactory.Create(model, OnDisconnectDeviceAsync, _logger);

                    // (throws)
                    using CancellationTokenSource cts = new(_timeout);
                    await edgeDevice.SetOnlineStatusAsync(true, cts.Token);

                    // Add device to device list
                    _devices.Add(model.DeviceId, edgeDevice);

                    // Log successfull device creation
                    _logger.Info($"Successfully connected device '{edgeDevice.Id}' (# of active devices: {_devices.Count})");
                }
                return edgeDevice;
            }
            catch (Exception)
            {
                edgeDevice?.Dispose(); // In case of an exception clear all reasources
                throw;
            }
        }

        public async Task DisconnectDeviceAsync(string deviceId)
        {
            await _semaphore.WaitAsync();
            IEdgeDevice edgeDevice = null;
            try
            {
                if (!_devices.ContainsKey(deviceId))
                {
                    edgeDevice = _devices[deviceId];

                    // Set disconnect state
                    using CancellationTokenSource cts = new(_timeout);
                    await edgeDevice.SetOnlineStatusAsync(false, cts.Token);
                    
                    // Only dispose if there is no preceeding exception
                    _devices.Remove(edgeDevice.Id);
                    edgeDevice?.Dispose();
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task SendTelemetryAsync(string deviceId, DeviceTelemetry telemetry)
        {
            if (telemetry.Data == null)
            {
                throw new NullReferenceException($"Empty telemetry for device '{deviceId}");
            }
            
            await _semaphore.WaitAsync();
            try
            {
                // Setup device connection on first time
                IEdgeDevice edgeDevice = await ConnectDeviceAsync(deviceId);

                // Send telemetry to IoT Hub
                using CancellationTokenSource cts = new(_timeout);
                await edgeDevice.SendDeviceTelemetryAsync(telemetry.Data, cts.Token);

                // Debug message
                _logger.Info(String.Format($"Telemetry of device '{deviceId}' successfully sent to IoT Hub"));
            }
            catch (Exception ex)
            {
                _logger.Error(String.Format($"SendTelemetryAsync failed {ex}"));
                throw;
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