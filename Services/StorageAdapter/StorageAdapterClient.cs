// Copyright (c) HOREICH GmbH, all rights reserved

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Horeich.Services.Diagnostics;
using Horeich.Services.Exceptions;
using Horeich.Services.Http;
using Horeich.Services.Models;
using Horeich.Services.Runtime;
using Newtonsoft.Json;

namespace Horeich.Services.StorageAdapter
{
    public interface IStorageAdapterClient
    {
        Task<DeviceDataSerivceModel> GetAsync(string collectionId, string key);
        Task<DeviceDataSerivceModel> GetDevicePropertiesAsync(string deviceId);
        Task<MappingServiceModel> GetDeviceMappingAsync(string deviceType, string version);
    }

    public sealed class StorageAdapterClient : IStorageAdapterClient
    {
        private const bool ALLOW_INSECURE_SSL_SERVER = true; // TODO: make it configurable, default to false
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly string _serviceUri;
        private readonly int _timeout;

        public StorageAdapterClient(
            IHttpClient httpClient,
            IServicesConfig config,
            ILogger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _serviceUri = config.StorageAdapterApiUrl;
            _timeout = config.StorageAdapterApiTimeout;
        }

        public async Task<DeviceDataSerivceModel> GetAsync(string collectionId, string key)
        {
            var response = await this._httpClient.GetAsync(
                PrepareRequest($"collections/{collectionId}/values/{key}"));

            ThrowIfError(response, collectionId, key);

            // Deserialize Http message into value API model (throws)
            return JsonConvert.DeserializeObject<DeviceDataSerivceModel>(response.Content,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        }

        public async Task<DeviceDataSerivceModel> GetDevicePropertiesAsync(string deviceId)
        {
            var response = await this._httpClient.GetAsync(
                PrepareRequest($"devices/type/device/id/{deviceId}")); // v1 is added

            ThrowIfError(response, "devices", deviceId);

            // Deserialize Http message into value API model (throws) // TODO:
            return JsonConvert.DeserializeObject<DeviceDataSerivceModel>(response.Content,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        }

        public async Task<MappingServiceModel> GetDeviceMappingAsync(string deviceType, string version)
        {
            var response = await _httpClient.GetAsync(
                PrepareRequest($"mappings/type/{deviceType}/version/{version}")); // v1 is added

            ThrowIfError(response, "mappings", String.Format($"{deviceType}.{version}"));

            // Deserialize Http message into value API model (throws)
            return JsonConvert.DeserializeObject<MappingServiceModel>(response.Content,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        }

        private HttpRequest PrepareRequest(string path, DeviceDataSerivceModel content = null)
        {
            var request = new HttpRequest();
            request.AddHeader(HttpRequestHeader.Accept.ToString(), "application/json");
            request.AddHeader(HttpRequestHeader.CacheControl.ToString(), "no-cache");
            request.AddHeader(HttpRequestHeader.UserAgent.ToString(), "Device Simulation " + this.GetType().FullName);
            request.SetUriFromString($"{this._serviceUri}/{path}");
            request.Options.EnsureSuccess = false;
            request.Options.Timeout = this._timeout;
            if (this._serviceUri.ToLowerInvariant().StartsWith("https:"))
            {
                request.Options.AllowInsecureSSLServer = ALLOW_INSECURE_SSL_SERVER;
            }

            if (content != null)
            {
                request.SetContent(content);
            }

            return request;
        }

        private void ThrowIfError(IHttpResponse response, string containerId, string key)
        {
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new ResourceNotFoundException($"Resource {containerId}/{key} not found.");
            }

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                throw new ConflictingResourceException(
                    $"Resource {containerId}/{key} out of date. Reload the resource and retry.");
            }

            if (response.IsError)
            {
                throw new ExternalDependencyException(
                    new HttpRequestException($"Storage request error: status code {response.StatusCode}"));
            }
        }
    }
}