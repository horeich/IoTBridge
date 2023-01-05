// Copyright (c) Horeich UG. All rights reserved.

using System;
using Horeich.Services.Runtime;
using Horeich.Services.Diagnostics;

namespace Horeich.IoTBridge.Runtime
{
    public interface IConfig
    {
        // Web service listening port
        int Port { get; }
        ILogConfig LogConfig { get; }

        // Service layer configuration
        IServicesConfig ServicesConfig { get; }

        // Client authentication and authorization configuration
        //IClientAuthConfig ClientAuthConfig { get; }
    }

    /// <summary>Web service configuration</summary>
    public class Config : IConfig
    {
        private const string APPLICATION_KEY = "IoTBridge:";
        private const string APPLICATION_NAME = APPLICATION_KEY + "Name";

        // service port
        private const string PORT_KEY = APPLICATION_KEY + "webservicePort";

        // Update interval
        private const string DEVICE_UPDATE_INTERVAL = APPLICATION_KEY + "DeviceUpdateInterval";

        // DTDL config
        private const string DIGITAL_TWINS_KEY = "DigitalTwins:";
        private const string DIGITAL_TWINS_HOST_NAME = DIGITAL_TWINS_KEY + "HostName";

        //private const string IOTHUB_CONNSTRING_KEY = APPLICATION_KEY + "iotHubConnectionString";
        private const string DEVICE_PROPERTIES_KEY = APPLICATION_KEY + "DevicePropertiesCache:"; 
        private const string DEVICE_PROPERTIES_WHITELIST_KEY = DEVICE_PROPERTIES_KEY + "whitelist";
        private const string DEVICE_PROPERTIES_TTL_KEY = DEVICE_PROPERTIES_KEY + "TTL";
        private const string DEVICE_PROPERTIES_REBUILD_TIMEOUT_KEY = DEVICE_PROPERTIES_KEY + "rebuildTimeout";

        //private const string EXTERNAL_DEPENDENCIES = "ExternalDependencies:";
        //private const string STORAGE_ADAPTER_URL_KEY = EXTERNAL_DEPENDENCIES + "storageAdapterWebServiceUrl";
        //private const string USER_MANAGEMENT_URL_KEY = EXTERNAL_DEPENDENCIES + "authWebServiceUrl";

        private const string CLIENT_AUTH_KEY = APPLICATION_KEY + "ClientAuth:";
        private const string CORS_WHITELIST_KEY = CLIENT_AUTH_KEY + "corsWhitelist";
        private const string AUTH_TYPE_KEY = CLIENT_AUTH_KEY + "authType";
        private const string AUTH_REQUIRED_KEY = CLIENT_AUTH_KEY + "authRequired";

        private const string JWT_KEY = APPLICATION_KEY + "ClientAuth:JWT:";
        private const string JWT_ALGOS_KEY = JWT_KEY + "allowedAlgorithms";
        private const string JWT_ISSUER_KEY = JWT_KEY + "authIssuer";
        private const string JWT_AUDIENCE_KEY = JWT_KEY + "aadAppId";
        private const string JWT_CLOCK_SKEW_KEY = JWT_KEY + "clockSkewSeconds";
        
        // Storage Adapter
        private const string STORAGE_KEY = "StorageAdapter:";
        private const string STORAGE_DOCUMENT_KEY = STORAGE_KEY + "DocumentId";
        private const string STORAGE_DEVICE_COLLECTION_KEY = STORAGE_KEY + "DeviceCollectionId";
        private const string STORAGE_MAPPING_COLLECTION_KEY = STORAGE_KEY + "MappingCollectionId";
        private const string STORAGE_URL_KEY = STORAGE_KEY + "WebServiceUrl";
        private const string STORAGE_URL_TIMEOUT = STORAGE_KEY + "WebServiceTimeout";



        private const string IOT_HUB_KEY = "IoTHub:";
        private const string IOT_HUB_TIMEOUT = IOT_HUB_KEY + "TelemetryTimeout";

        //private const string COSMOSDB_CONNSTRING_KEY = COSMOSDB_KEY + "documentDBConnectionString";
        //private const string COSMOSDB_RUS_KEY = COSMOSDB_KEY + "RUs";
        
        private const string LOGGING_LEVEL_DEFAULT = "Logging:LogLevel";
        private const string LOGGING_LEVEL_REMOTE = "Logging:ApplicationInsights:LogLevel";
        private const string LOGGING_INSTRUMENTATION_KEY = "Logging:ApplicationInsights:Instrumentationkey";

        public int Port { get; }
        public IServicesConfig ServicesConfig { get; }

        public ILogConfig LogConfig { get; }
        //public IClientAuthConfig ClientAuthConfig { get; }

        public Config(IDataHandler dataHandler)
        {
            this.Port = dataHandler.GetInt(PORT_KEY);

            //var connstring = dataHandler.GetString(IOTHUB_CONNSTRING_KEY);

            // if (connstring.ToLowerInvariant().Contains("your azure iot hub"))
            // {
            //     // In order to connect to Azure IoT Hub, the service requires a connection
            //     // string. The value can be found in the Azure Portal. For more information see
            //     // https://docs.microsoft.com/azure/iot-hub/iot-hub-csharp-csharp-getstarted
            //     // to find the connection string value.
            //     // The connection string can be stored in the 'appsettings.ini' configuration
            //     // file, or in the PCS_IOTHUB_CONNSTRING environment variable. When
            //     // working with VisualStudio, the environment variable can be set in the
            //     // WebService project settings, under the "Debug" tab.
            //     throw new Exception("The service configuration is incomplete. " +
            //                         "Please provide your Azure IoT Hub connection string. " +
            //                         "For more information, see the environment variables " +
            //                         "used in project properties and the 'iothub_connstring' " +
            //                         "value in the 'appsettings.ini' configuration file.");
            // }

            this.ServicesConfig = new ServicesConfig
            {
                ApplicationName = dataHandler.GetString(APPLICATION_NAME),
                StorageAdapterDocumentKey = dataHandler.GetString(STORAGE_DOCUMENT_KEY),
                StorageAdapterDeviceCollectionKey = dataHandler.GetString(STORAGE_DEVICE_COLLECTION_KEY),
                StorageAdapterMappingCollection = dataHandler.GetString(STORAGE_MAPPING_COLLECTION_KEY),
                // IoTHubConnString = configData.GetString(IOTHUB_CONNSTRING_KEY),
                // DevicePropertiesWhiteList = configData.GetString(DEVICE_PROPERTIES_WHITELIST_KEY),
                // DevicePropertiesTTL = configData.GetInt(DEVICE_PROPERTIES_TTL_KEY),
                // DevicePropertiesRebuildTimeout = configData.GetInt(DEVICE_PROPERTIES_REBUILD_TIMEOUT_KEY),
                StorageAdapterApiUrl = dataHandler.GetString(STORAGE_URL_KEY),
                StorageAdapterApiTimeout = dataHandler.GetInt(STORAGE_URL_TIMEOUT),
                IoTHubTimeout = dataHandler.GetInt(IOT_HUB_TIMEOUT),
                DeviceUpdateInterval = dataHandler.GetInt(DEVICE_UPDATE_INTERVAL),
                DigitalTwinHostName = dataHandler.GetString(DIGITAL_TWINS_HOST_NAME),
                //UserManagementApiUrl = configData.GetString(USER_MANAGEMENT_URL_KEY)
            };

            // Initialize log config
            // Parse log config enum
            LogLevel logLevel;
            string enumString = dataHandler.GetString(LOGGING_LEVEL_DEFAULT);
            logLevel = Enum.TryParse<LogLevel>(enumString, true, out logLevel) ? logLevel : LogLevel.Debug;

            LogConfig = new LogConfig
            {
                LogLevel = logLevel,
            };
        }
    }
}
