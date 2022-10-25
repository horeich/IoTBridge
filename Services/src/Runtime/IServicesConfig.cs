namespace Horeich.SensingSolutions.Services.Runtime
{
    public interface IServicesConfig
    {
        string StorageAdapterDocumentKey { get; }
        string StorageAdapterDeviceCollectionKey { get; }
        string StorageAdapterMappingCollection { get; }
        string StorageAdapterApiUrl { get; }
        int StorageAdapterApiTimeout { get; }
        int IoTHubTimeout { get; }
        int DeviceUpdateInterval { get; }
        string DigitalTwinHostName { get; }

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