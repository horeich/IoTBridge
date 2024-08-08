// Copyright (c) HOREICH GmbH, all rights reserved

using System;
using Horeich.Services.Runtime;
using Horeich.Services.Diagnostics;

namespace Horeich.IoTBridge.Runtime
{
    public interface IConfig
    {
        int Port { get; } // Web service listening port
        ILogConfig LogConfig { get; }
        IServicesConfig ServicesConfig { get; } // Service layer configuration
    }

    /// <summary>Web service configuration</summary>
    public class Config : IConfig
    {
        private const string APPLICATION_KEY = "IoTBridge:";
        private const string APPLICATION_NAME = APPLICATION_KEY + "Name";

        // service port
        private const string PORT_KEY = APPLICATION_KEY + "WebServicePort";

        // Update interval
        private const string DEVICE_PROPERTIES_KEY = APPLICATION_KEY + "DevicePropertiesCache:"; 
        private const string DEVICE_PROPERTIES_WHITELIST_KEY = DEVICE_PROPERTIES_KEY + "whitelist";
        private const string DEVICE_PROPERTIES_TTL_KEY = DEVICE_PROPERTIES_KEY + "TTL";
        private const string DEVICE_PROPERTIES_REBUILD_TIMEOUT_KEY = DEVICE_PROPERTIES_KEY + "rebuildTimeout";

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
        private const string STORAGE_DEVICE_COLLECTION_KEY = STORAGE_KEY + "DevicesContainerId";
        private const string STORAGE_MAPPING_COLLECTION_KEY = STORAGE_KEY + "MappingsContainerId";
        private const string STORAGE_URL_KEY = STORAGE_KEY + "WebServiceUrl";
        private const string STORAGE_URL_TIMEOUT = STORAGE_KEY + "WebServiceTimeout";

        // Device client
        private const string DEVICE_CLIENT_KEY = "DeviceClient:";
        private const string DEVICE_CLIENT_CONNECTION_TIMEOUT_KEY = DEVICE_CLIENT_KEY + "ConnectionTimeout";

        // Logging
        private const string LOGGING_LEVEL_DEFAULT = "Logging:MinimumLogLevel";

        public int Port { get; }
        public IServicesConfig ServicesConfig { get; }
        public ILogConfig LogConfig { get; }

        public Config(IDataHandler dataHandler)
        {
            this.Port = dataHandler.GetInt(PORT_KEY);

            // Set the configuration for all services
            this.ServicesConfig = new ServicesConfig
            {
                ApplicationName = dataHandler.GetString(APPLICATION_NAME),
                StorageAdapterDeviceContainer = dataHandler.GetString(STORAGE_DEVICE_COLLECTION_KEY),
                StorageAdapterMMappingContainer = dataHandler.GetString(STORAGE_MAPPING_COLLECTION_KEY),
                StorageAdapterApiUrl = dataHandler.GetString(STORAGE_URL_KEY),
                StorageAdapterApiTimeout = dataHandler.GetInt(STORAGE_URL_TIMEOUT),
                DeviceClientTimeout = dataHandler.GetInt(DEVICE_CLIENT_CONNECTION_TIMEOUT_KEY),
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
