// Copyright (c) HOREICH GmbH. All rights reserved.

namespace Horeich.Services.Runtime
{   
    public interface IServicesConfig
    {
        string ApplicationNameKey { get; }
        string StorageAdapterDocumentKey { get; }
        string StorageAdapterDeviceCollectionKey { get; }
        string StorageAdapterMappingCollection { get; }
        string StorageAdapterApiUrl { get; }
        int StorageAdapterApiTimeout { get; }
        int IoTHubTimeout { get; }
        string DbPartitionKey { get; }
        int DeviceUpdateInterval { get; }

        // string UserManagementApiUrl { get; }
        // StorageConfig MessagesConfig { get; set; }
        // AlarmsConfig AlarmsConfig { get; set; }
        // string StorageType { get; set; }
        // Uri CosmosDbUri { get; }
        // string CosmosDbKey { get; }
        // int CosmosDbThroughput { get; set; }
        // string TimeSeriesFqdn { get; }
        // string TimeSeriesAuthority { get; }
        // string TimeSeriesAudience { get; }
        // string TimeSeriesExplorerUrl { get; }
        // string TimeSertiesApiVersion { get; }
        // string TimeSeriesTimeout { get; }
        // string ActiveDirectoryTenant { get; }
        // string ActiveDirectoryAppId { get; }
        // string ActiveDirectoryAppSecret { get; }
        // string DiagnosticsApiUrl { get; }
        // int DiagnosticsMaxLogRetries { get; }
        // string ActionsEventHubConnectionString { get; }
        // string ActionsEventHubName { get; }
        // string BlobStorageConnectionString { get; }
        // string ActionsBlobStorageContainer { get; }
        // string LogicAppEndpointUrl { get; }
        // string SolutionUrl { get; }
        // string TemplateFolder { get; }
    }
    public class ServicesConfig : IServicesConfig
    {
        public string ApplicationNameKey { get; set; }
        public string StorageAdapterDocumentKey { get; set; }
        public string StorageAdapterDeviceCollectionKey { get; set; }
        public string StorageAdapterMappingCollection { get; set; }
        public string StorageAdapterApiUrl { get; set; }
        public int StorageAdapterApiTimeout { get; set; }
        public int IoTHubTimeout { get; set; }
        public string DbPartitionKey { get; set; }
        public int DeviceUpdateInterval { get; set; }
        // string UserManagementApiUrl { get; }
        // StorageConfig MessagesConfig { get; set; }
        // AlarmsConfig AlarmsConfig { get; set; }
        // string StorageType { get; set; }
        // Uri CosmosDbUri { get; }
        // string CosmosDbKey { get; }
        // int CosmosDbThroughput { get; set; }
        // string TimeSeriesFqdn { get; }
        // string TimeSeriesAuthority { get; }
        // string TimeSeriesAudience { get; }
        // string TimeSeriesExplorerUrl { get; }
        // string TimeSertiesApiVersion { get; }
        // string TimeSeriesTimeout { get; }
        // string ActiveDirectoryTenant { get; }
        // string ActiveDirectoryAppId { get; }
        // string ActiveDirectoryAppSecret { get; }
        // string DiagnosticsApiUrl { get; }
        // int DiagnosticsMaxLogRetries { get; }
        // string ActionsEventHubConnectionString { get; }
        // string ActionsEventHubName { get; }
        // string BlobStorageConnectionString { get; }
        // string ActionsBlobStorageContainer { get; }
        // string LogicAppEndpointUrl { get; }
        // string SolutionUrl { get; }
        // string TemplateFolder { get; }
    }
}
