
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;

using Horeich.Services.Runtime;
using Horeich.Services.Exceptions;
using Horeich.Services.Diagnostics;
using Horeich.Services.StorageAdapter;
// using FluentScheduler;
using Hangfire;

namespace Horeich.Services.VirtualDevice
{

public class EdgeDeviceManager
{
    // TODO: min allowed update interval for devices
    private readonly ILogger _logger;
    private readonly IDataHandler _dataHandler;
    private readonly IStorageAdapterClient _storageClient;
    private CancellationTokenSource _cts;


    EdgeDevice _edgeDevice;

    private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);


    private Dictionary<string, EdgeDevice> _devices = new Dictionary<string, EdgeDevice>(); // list of all running virtual sensors


    public EdgeDeviceManager(EdgeDevice device)
    {
        _edgeDevice = device;
        _cts = new CancellationTokenSource();
        _cts.From
        // _edgeDevice.LifespanTimeout += DisposeEdgeDevice;

        JobManager.Initialize();
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
        // model.HubString = _dataHandler.GetString(result.HubId, string.Empty) + ".azure-devices.net";
        // if (model.HubString == String.Empty)
        // {
        //     throw new InvalidConfigurationException($"Unable to load configuration value for '{result.HubId}'");
        // }

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

    private async Task TimeoutAction(object sender, EventArgs ea)
    {
        // This is executed in EdgeDevice's context
        await _semaphore.WaitAsync();
        try 
        {
            EdgeDevice edgeDevice = (EdgeDevice)sender;
            EdgeDevice foundDevice = _devices[edgeDevice.DeviceId];
            if (foundDevice == null)
            {
                // We do not expect this
                throw new NullReferenceException(); // TODO: needed?
            }

            CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(10));
            await foundDevice.UpdatePropertyAsync("Status", "offline", cts.Token);

            foundDevice.ResetDeviceTimeout();
            _devices[edgeDevice.DeviceId] = null;
        }
        catch (Exception e)
        {
            
        }
        finally
        {
            _semaphore.Release();
        }
    }



    public async Task BridgeDeviceAsync(string deviceId, DeviceTelemetry telemetry)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (!_devices.ContainsKey(deviceId))
            {
                // Load device model (throws)
                DeviceApiModel model = await LoadDeviceApiModelAsync(deviceId);

                // Create device and send online status (throws)
                EdgeDevice edgeDevice = await EdgeDevice.Create(model, TimeoutAction, _logger, cts.Token); // TODO own data handler?
                
                // Add to internal list on success
                _devices.Add(deviceId, edgeDevice);

            }

            // Get reference to existing virtual sensor
            EdgeDevice device = _devices[deviceId];

            // Send telemetry async
            await device.SendDeviceTelemetryAsync(telemetry.Data, _config.IoTHubTimeout);
            _logger.Debug("Telemetry successfully sent to IoT Central");
        }
        catch
        {
            // TODO: log device creation error
        }
        finally
        {
            _semaphore.Release();
        }
    }

      
    }
}

