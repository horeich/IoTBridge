/// <summary>
/// Copyright (c) Horeich UG
/// /// \author: Andreas Reichle
/// </summary>

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Newtonsoft.Json;

using Horeich.SensingSolutions.Services.Runtime;
using Horeich.SensingSolutions.Services.Exceptions;
using Horeich.SensingSolutions.Services.Diagnostics;

using System.Collections.Concurrent;
using System.Collections.Specialized;

using System.IO;

using Horeich.Services.Models;

using Azure.DigitalTwins.Core;
using Azure.Core;
using Azure.Identity;
using Azure;
using Microsoft.Azure.DigitalTwins.Parser;


using System.Web;

using Horeich.SensingSolutions.Services.StorageAdapter;

namespace Horeich.SensingSolutions.Services.VirtualDevice
{   
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

        private ConcurrentDictionary<string, IVirtualDevice> _virtualDeviceTwins = new ConcurrentDictionary<string, IVirtualDevice>();
           
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
                // TODO: other error result.HubId
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

        private async Task<BasicDigitalTwin> LoadDeviceTwin(string deviceId)
        {
            string digitalTwinsInstanceUrl = _config.DigitalTwinHostName;


            // TODO: Try block
            // MUST BE LOGGED IN AZURE IN VS CODE!
            var credential = new DefaultAzureCredential();
            var client = new DigitalTwinsClient(new Uri(digitalTwinsInstanceUrl), credential);

            BasicDigitalTwin twin;
            Response<BasicDigitalTwin> twinResponse = await client.GetDigitalTwinAsync<BasicDigitalTwin>(deviceId);
            return twinResponse.Value;
        }

        // 
        private async Task<TwinServiceModel> LoadTwinApiModel(string model, LwM2MResources resources)
        {
            TwinServiceModel twinModel = null;
            string digitalTwinsInstanceUrl = _config.DigitalTwinHostName;

            var credential = new DefaultAzureCredential();
            var client = new DigitalTwinsClient(new Uri(digitalTwinsInstanceUrl), credential);
            try
            {
                DigitalTwinsModelData dtdlModel = await client.GetModelAsync(model);
                var models = new List<string>(); // must be in form of a list
                models.Add(dtdlModel.DtdlModel);

                var parser = new ModelParser();
                IReadOnlyDictionary<Dtmi, DTEntityInfo> dtdlOM = await parser.ParseAsync(models);

                var interfaces = new List<DTInterfaceInfo>();
                IEnumerable<DTInterfaceInfo> ifenum =
                    from entity in dtdlOM.Values
                    where entity.EntityKind == DTEntityKind.Interface
                    select entity as DTInterfaceInfo;
                interfaces.AddRange(ifenum);
                
                if (interfaces.Count > 0)
                {
                    twinModel = new TwinServiceModel();
                    twinModel.TelemetryMapping = new List<LwM2MAdapterModel>();

                    foreach (DTInterfaceInfo dtif in interfaces)
                    {
                        var sb = new StringBuilder();
                        for (int i = 0; i < 0; i++) 
                        {
                            sb.Append("  ");
                        }
                        _log.Info($"{sb}Interface: {dtif.Id} | {dtif.DisplayName}", () => {});

                        Dictionary<string, DTContentInfo> contents = dtif.Contents;

                        foreach (DTContentInfo item in contents.Values)
                        {
                            switch (item.EntityKind)
                            {
                                case DTEntityKind.Telemetry:
                                    DTTelemetryInfo ti = item as DTTelemetryInfo;
                                    Console.WriteLine($"{sb}--Telemetry: {ti.Name} with schema {ti.Schema}");
                                    LwM2MAdapterModel adapter = new LwM2MAdapterModel();
                                    adapter.DataType = ti.Schema.EntityKind; // TODO: remove later
                                    adapter.Identity = ti.Name;
                                    // TODO: compare ids

                                    

                                    String resource = String.Empty;
                                    foreach(String label in ti.Id.Labels)
                                    {
                                        String num = Regex.Match(label, @"\d+").Value;
                                        // Int32.Parse(num);
                                        // TODO: error handling
                                        resource += num;
                                        resource +="/";
                                    }

                                    resource = resource.Substring(0, resource.Length - 1);

                                    if (resources.Res.Contains(resource))
                                    {
                                        adapter.LwM2MObject = resource;
                                    }
                                    

                                    //  = "<64000/0/5700>";
                                    // twinModel.TelemetryMapping.Add(adapter);
                                    break;
                                    // TODO: other porperites
                            }  
                        }
                    }
                }
            }
            catch (RequestFailedException e)
            {
                _log.Error(string.Format("The requestes model does not exist: {0}", e.Message), () => {}); // 404
            }
            // catch (ArgumentNullException e) // already handled in request
            // {
            //     _log.Error(string.Format("Device id is null"), () => {}); // 406
            // }
            return twinModel; // either empty or error during loading
        }

        public async Task Unregister(string deviceId)
        {
            // TODO: use ConcurrentDictionary
            // Returns false if note found but that's OK
            _log.Debug(string.Format("Elements in list before [{0}]", _devices.Count), () => {});
            _devices.Remove(deviceId);
            _log.Debug(string.Format("Elements in list after [{0}]", _devices.Count), () => {});

            // TODO: check what happens if deregistration is doen a couple of time???
        }

       

        public async Task<TwinServiceModel> Register(string deviceId, LwM2MResources resources)
        {
            //  Environment.SetEnvironmentVariable("AZURE_CLIENT_ID", "4cea9343-0cf4-40a4-bfce-7fcc4beadc49");
            // Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", "If3z_-9EJBBdjzp-vxz1qO21raG0g2_CH1");
            // Environment.SetEnvironmentVariable("AZURE_TENANT_ID", "2bf605eb-355a-4c48-b312-63828a444b8d");
            // TODO: Mutex here between contain and add or use concurrent dict
            // https://docs.microsoft.com/de-de/dotnet/standard/collections/thread-safe/how-to-add-and-remove-items
            // Add first, then load device api model
            
            
            if (!_virtualDeviceTwins.TryGetValue(deviceId, out IVirtualDevice value))
            {
                DeviceTwinCredentials deviceTwinCredentials = new DeviceTwinCredentials();

                deviceTwinCredentials.HubString = "HHub.azure-devices.net"; // TODO: load from key vault
                deviceTwinCredentials.DeviceKey = "I7vXwDV6vuBGIRx5ogMvpyGP/lIcY/bala7xe9eEhfc=";

                // Only adds the virtual twin if it does not exist yet
                _virtualDeviceTwins.TryAdd(deviceId, VirtualDeviceTwin.Create(deviceTwinCredentials, _log));
            }

            // if (!_devices.ContainsKey(deviceId))
            // {
            //     BasicDigitalTwin deviceTwin = await LoadDeviceTwin(deviceId);
            //     DeviceTwinCredentials deviceTwinCredentials = new DeviceTwinCredentials();

            //     // Check for HubId

            //     foreach (string property in deviceTwin.Contents.Keys)
            //     {
            //         if (deviceTwin.Contents.TryGetValue(property, out object value))
            //         {
            //             if (property == "HubId") // TODO: change in settings
            //             {
            //                 _log.Info($"Found {property} with value {value}", () => {});
            //                 deviceTwinCredentials.HubString = value.ToString(); // can only be string format
            //                 break;
            //             }
            //         }
            //     }

            //     if (deviceTwinCredentials.HubString == String.Empty)
            //     {
            //         throw new InvalidConfigurationException($"Unable to load hub id from device twin");
            //         // TODO: throw;
            //     }

            //      // Get connection string from key vault
            //     // model.HubString = _dataHandler.GetString(result.HubId, string.Empty) + ".azure-devices.net";
            //     // if (model.HubString == String.Empty)
            //     // {
            //     //     throw new InvalidConfigurationException($"Unable to load configuration value for '{result.HubId}'");
            //     // }
            //     deviceTwinCredentials.DeviceId = deviceTwin.Id;
            //     deviceTwinCredentials.DeviceKey = "I7vXwDV6vuBGIRx5ogMvpyGP/lIcY/bala7xe9eEhfc=";
            //     // TODO: _dataHandler.GetString(model.DeviceId, string.Empty); // Get device key from key vault
            //     if (deviceTwinCredentials.DeviceKey == String.Empty)
            //     {
            //         // TODO: other error result.HubId
            //         throw new InvalidConfigurationException($"Unable to load configuration value for '{deviceTwinCredentials.DeviceId}'");
            //     }

            //     TwinServiceModel twinModel = await LoadTwinApiModel(deviceTwin.Metadata.ModelId, resources); // model id MUST be there!
            //     if (twinModel != null)
            //     {
            //         twinModel.DeviceId = deviceId;
            //         _devices.Add(deviceId, VirtualDeviceTwin.Create(deviceTwinCredentials, _log));
            //     }
            //     else
            //     {
            //         throw new InvalidInputException("DTDL loading error or DTDL empty");
            //     }
            //     return twinModel;
            // }
            // else
            // {
            //     // Throw already registered error?? See OMA spec
            // }

            return null;
        }

        public async Task SolveBinaryTelemetry(string deviceId, BinaryTelemetryApiModel telemetry)
        {
            IVirtualDevice device = null;
            if (_devices.TryGetValue(deviceId, out device))
            {
                await device.SendDeviceTelemetryAsync(telemetry.Data, 10000);
            }

           

            // if (_devices.ContainsKey(deviceId))
            // {
            //     // Convert string to type (TODO: error handling?)
            //     for (int i = 0; i < result.Mapping.Count; ++i)
            //     {
            //         Type varType = TypeFromString(result.Mapping[i][1]);
            //         model.Mapping.Add(Tuple.Create(result.Mapping[i][0], varType));
            //     }    
            // }
            // else
            // {
            //     // Throw error / device is not registered
            // }
        }

        public async Task Register(string deviceId)
        {
            try 
            {
                // see how to use dictionaries: 
                // https://docs.microsoft.com/en-us/dotnet/standard/collections/thread-safe/how-to-add-and-remove-items

                if (!_virtualDeviceTwins.TryGetValue(deviceId, out IVirtualDevice value))
                {
                    DeviceTwinCredentials deviceTwinCredentials = new DeviceTwinCredentials();

                    deviceTwinCredentials.HubString = "HHub.azure-devices.net"; // TODO: load from key vault
                    deviceTwinCredentials.DeviceId = deviceId;
                    deviceTwinCredentials.DeviceKey = "I7vXwDV6vuBGIRx5ogMvpyGP/lIcY/bala7xe9eEhfc=";

                    // Only adds the virtual twin if it does not exist yet
                    _virtualDeviceTwins.TryAdd(deviceId, VirtualDeviceTwin.Create(deviceTwinCredentials, _log));
                }
            }
            catch (Exception e)
            {
                _log.Error(string.Format("Device id is null {}", e.Message), () => {});
            }
        }

        // public async Task<PropertyApiModel> FetchDownlinkProperties(String deviceId)
        // {
        //     PropertyApiModel propertyApiModel = null;
        //     try 
        //     {
        //         if (_virtualDeviceTwins.TryGetValue(deviceId, out IVirtualDevice virtualDeviceTwin))
        //         {
        //             propertyApiModel = virtualDeviceTwin.FetchDownlinkProperties();
        //         }
        //     }
        //     catch (Exception e)
        //     {
        //         // TODO: invalid argument exception??
        //         _log.Error(string.Format("Device id is null {}", e.Message), () => {});
        //     }
        //     return propertyApiModel;
        // }

        public async Task BridgeDeviceAsync(String deviceId, TelemetryApiModel payload)
        {
            try 
            {
                if (_virtualDeviceTwins.TryGetValue(deviceId, out IVirtualDevice virtualDeviceTwin))
                {
                    await virtualDeviceTwin.SyncDeviceFunctionsAsync(payload);
                }
            }
            catch (Exception e)
            {
                // TODO: invalid argument exception??
                _log.Error(string.Format("Device id is null {}", e.Message), () => {});
            }
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
                    _devices.Add(model.DeviceId, await VirtualDeviceTwin.Create(model, _log)); // TODO own data handler?
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


// }
            // catch (DeviceIdentityException e)
            // {
            //     _log.Error(string.Format("invalid device identity string: {0}", e.Message), () => {});
            // }
            // catch (ArgumentOutOfRangeException e)
            // {
            //     _log.Error(string.Format("invalid device identity string: {0}", e.Message), () => {});
            // }
            // catch (Exception ex)
            // {
            //     _log.Error(string.Format("fatal error: {0}", ex.Message), () => {});
            // }

                        // }
            // catch(FormatException e) // wrong data format
            // {
            //     _log.Error(string.Format("Invalid data format: {0}", e), () => {});
            // }
            // catch(OperationCanceledException e) // send to IoT Hub timeout
            // {
            //     _log.Error(string.Format(
            //         "Timeout error ({0}) while sending message to IoT Hub", e.Message), () => new { DateTime.Now });   
            // }

                        //Random rand = new Random();
            //var payloadStr = System.Text.Encoding.Default.GetString(payload);
            //payload = payload.Remove(0, 4);
            //Console.WriteLine(payload);        
            //double currentTemperature = pl + rand.NextDouble() * 20;
            // double currentPressure = basePressure + rand.NextDouble() * 100;
            // double currentHumidity = baseHumidity + rand.NextDouble() * 20;
            // try
            // {  