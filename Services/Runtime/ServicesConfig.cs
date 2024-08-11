// Copyright (c) HOREICH GmbH. All rights reserved.

namespace Horeich.Services.Runtime
{   
    public interface IServicesConfig
    {
        string ApplicationName { get; }
        string StorageAdapterDeviceContainer { get; }
        string StorageAdapterMMappingContainer { get; }
        string StorageAdapterApiUrl { get; }
        int StorageAdapterApiTimeout { get; }
        int DeviceClientTimeout { get; }
        string DbPartitionKey { get; }
        // int DeviceUpdateInterval { get; }

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
        public string ApplicationName { get; set; }
        public string StorageAdapterDeviceContainer { get; set; }
        public string StorageAdapterMMappingContainer { get; set; }
        public string StorageAdapterApiUrl { get; set; }
        public int StorageAdapterApiTimeout { get; set; }
        public int DeviceClientTimeout { get; set; }
        public string DbPartitionKey { get; set; }
        // public int DeviceUpdateInterval { get; set; }
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
