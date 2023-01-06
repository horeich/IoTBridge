// Copyright (c) Horeich UG (andreas.reichle@horeich.de)

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Horeich.Services.Runtime;
using Horeich.Services.Exceptions;
using Horeich.Services.Diagnostics;
using Horeich.Services.StorageAdapter;

namespace Horeich.Services.VirtualDevice
{
    public interface IVirtualDeviceManager
    {
        Task BridgeDeviceAsync(string deviceId, DeviceTelemetry telemetry);
    }

    public class VirtualDeviceManager : IVirtualDeviceManager
    {
        private readonly IStorageAdapterClient _storageClient;
        private readonly IServicesConfig _config;
        private readonly IDataHandler _dataHandler;
        private readonly ILogger _logger;
        private Task _updateTask;
        // private CancellationTokenSource _cts;
        private Dictionary<string, IVirtualDevice> _devices = new Dictionary<string, IVirtualDevice>(); // list of all running virtual sensors
        private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public VirtualDeviceManager(
            IStorageAdapterClient storageClient,
            IDataHandler dataHandler,
            IServicesConfig config,
            ILogger logger)
        {
            _storageClient = storageClient;
            _config = config;
            _dataHandler = dataHandler;
            _logger = logger;
            _updateTask = Task.Run(() => UpdateDevices());
            // _cts = new CancellationTokenSource();
        }

        private Type TypeFromString(string dataType)
        {
            if (String.Compare(dataType, "int") == 0)
            {
                return typeof(int);
            }
            else if (String.Compare(dataType, "bool") == 0)
            {
                return typeof(bool);
            }
            else if (String.Compare(dataType, "double") == 0)
            {
                return typeof(double);
            }
            else if (String.Compare(dataType, "string") == 0)
            {
                return typeof(string);
            }
            else if (String.Compare(dataType, "float") == 0)
            {
                return typeof(float);
            }
            else
            {
                // Unknown payload type
                throw new DevicePayloadTypeException("data type not found");
            }
        }

        private async Task UpdateDevices()
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(_config.DeviceUpdateInterval)).ConfigureAwait(false);
                await _semaphore.WaitAsync();
                try
                {
                    foreach (KeyValuePair<string, IVirtualDevice> device in _devices)
                    {
                        bool active = await device.Value.UpdateConnectionStatusAsync();
                        if (!active)
                        {
                            _logger.Info($"Removing device '{device.Value.DeviceId}' from device list");
                            _devices.Remove(device.Key);
                        }
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
                _logger.Debug("Updated devices");
            }
        }

        private async Task<DeviceApiModel> LoadDeviceApiModelAsync(string deviceId)
        {
            DeviceApiModel model = new DeviceApiModel();
            model.DeviceId = deviceId;

            // Get device info from storage (throws)
            ValueApiModel result = await _storageClient.GetAsync(_config.StorageAdapterDeviceCollectionKey, deviceId);
            model.SendInterval = result.SendInterval;
            model.Properties = result.Properties;

            // Get Iot Hub connection string from key vault
            model.HubString = _dataHandler.GetString(result.HubId, string.Empty) + ".azure-devices.net";
            if (model.HubString == String.Empty)
            {
                throw new InvalidConfigurationException($"Unable to load configuration value for '{result.HubId}'");
            }

            // Get Device Key from key vault
            model.DeviceKey = _dataHandler.GetString(model.DeviceId, string.Empty);
            if (model.DeviceKey == String.Empty)
            {
                throw new InvalidConfigurationException($"Unable to load configuration value for '{result.HubId}'");
            }

            // Get mapping from storage (throws)
            result = await _storageClient.GetAsync(_config.StorageAdapterMappingCollection, result.Type);
            model.Mapping = new List<Tuple<string, Type>>(result.Mapping.Count);

            // Convert string to type (TODO: error handling?)
            for (int i = 0; i < result.Mapping.Count; ++i)
            {
                Type varType = TypeFromString(result.Mapping[i][1]);
                model.Mapping.Add(Tuple.Create(result.Mapping[i][0], varType));
            }
            return model;
        }

        public async Task BridgeDeviceAsync(string deviceId, DeviceTelemetry telemetry)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (!_devices.ContainsKey(deviceId))
                {
                    _logger.Info($"Adding device '{deviceId}' to internal device list");
                    DeviceApiModel model = await LoadDeviceApiModelAsync(deviceId);
                    _devices.Add(model.DeviceId, await VirtualDevice.Create(model, _logger)); // TODO own data handler?
                }

                // Get reference to existing virtual sensor
                IVirtualDevice device = _devices[deviceId];

                // Send telemetry async
                await device.SendDeviceTelemetryAsync(telemetry.Data, _config.IoTHubTimeout);
                _logger.Debug("Telemetry successfully sent to IoT Central");
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}

/// <summary>
/// Periodically running task to free unused resources
/// </summary>
/// <param name="updateInterval"></param>
/// <returns></returns>
// private async void UpdateDeviceList(int updateInterval)
// {
//     await Task.Run(async () =>
//     {
//         int count = 1;
//         while (count > 0)
//         {
//             await Task.Delay(updateInterval).ConfigureAwait(false);
//             await _semaphore.WaitAsync();
//             try
//             {
//                 foreach (KeyValuePair<string, IVirtualDevice> device in _devices)
//                 {
//                     bool active = await device.Value.IsActive();
//                     if (!active)
//                     {
//                         _logger.Info($"Removing device '{device.Value.DeviceId}' from device list");
//                         _devices.Remove(device.Key);
//                     }
//                 }
//                 count = _devices.Count;
//             }
//             finally
//             {
//                 _semaphore.Release();
//             }
//         }
//     });
// }