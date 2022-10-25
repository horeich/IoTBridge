/// <summary>
/// Copyright (c) Horeich UG
/// /// \author: Andreas Reichle
/// </summary>

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Newtonsoft.Json;

using Horeich.SensingSolutions.Services.Runtime;
using Horeich.SensingSolutions.Services.Exceptions;
using Horeich.SensingSolutions.Services.Diagnostics;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.Specialized;

using System.Web;

using Horeich.SensingSolutions.Services.StorageAdapter;

namespace Horeich.SensingSolutions.Services.VirtualDevice
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
        private readonly ILogger _log;
        static TwinCollection reportedProperties = new TwinCollection();
        public TimeSpan SendTimeout { set; get; }
        private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        // List of all running virtual sensors
        private Dictionary<string, IVirtualDevice> _devices = new Dictionary<string, IVirtualDevice>();

        /// <summary>
        /// CTOR
        /// </summary>
        /// <param name="dataHandler"></param>
        /// <param name="logger"></param>
        public VirtualDeviceManager(
            IStorageAdapterClient storageClient,
            IDataHandler dataHandler,
            IServicesConfig config,
            ILogger logger)
        {
            _storageClient = storageClient;
            _config = config;
            _dataHandler = dataHandler;
            _log = logger;
        }

         /// <summary>
        /// Get type of data points from given string
        /// </summary>
        /// <param name="dataType"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Periodically running task to free unused resources
        /// </summary>
        /// <param name="updateInterval"></param>
        /// <returns></returns>
        private async void UpdateDeviceList(int updateInterval)
        {
            await Task.Run(async () =>
            {
                int count = 1;
                while(count > 0)
                {
                    await Task.Delay(updateInterval).ConfigureAwait(false);
                    await _semaphore.WaitAsync();
                    try
                    {
                        foreach (KeyValuePair<string, IVirtualDevice> device in _devices)
                        {
                            bool active = await device.Value.IsActive();
                            if (!active)
                            {
                                _log.Debug("Removing device from device list", () => {});
                                _devices.Remove(device.Key);
                            }
                        }
                        count = _devices.Count;
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }
            });
        }

         /// <summary>
        /// The API model of the sensor is stored in a SQL storage and is accessed when the sensor is being created or
        /// has been inactive for a while
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        private async Task<DeviceApiModel> LoadDeviceApiModel(string deviceId)
        {
            DeviceApiModel model = new DeviceApiModel();
            model.DeviceId = deviceId;

            // Get device info from storage (throws)
            ValueApiModel result = await _storageClient.GetAsync(_config.StorageAdapterDeviceCollectionKey, deviceId);
            model.SendInterval = result.SendInterval;
            model.Properties = result.Properties;

            // Get connection string from key vault
            model.HubString = _dataHandler.GetString(result.HubId, string.Empty) + ".azure-devices.net";
            if (model.HubString == String.Empty)
            {
                throw new InvalidConfigurationException($"Unable to load configuration value for '{result.HubId}'");
            }

            // Get device key from key vault
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
                    _log.Debug("Adding device to device list", () => {});
                    DeviceApiModel model = await LoadDeviceApiModel(deviceId);
                    _devices.Add(model.DeviceId, await VirtualDevice.Create(model, _log)); // TODO own data handler?
                    UpdateDeviceList(_config.DeviceUpdateInterval);
                }

                // Get reference to existing virtual sensor
                IVirtualDevice device = _devices[deviceId];

                // Send telemetry async
                await device.SendDeviceTelemetryAsync(telemetry.Data, _config.IoTHubTimeout);
                _log.Debug("Telemetry successfully sent to IoT Central", () => {});
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}