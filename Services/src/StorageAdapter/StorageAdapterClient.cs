// Copyright (c) Microsoft. All rights reserved.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Horeich.Services.Diagnostics;
using Horeich.Services.Exceptions;
using Horeich.Services.Http;
using Horeich.Services.Runtime;

using Newtonsoft.Json;

namespace Horeich.Services.StorageAdapter
{
    public interface IStorageAdapterClient
    {
        //Task<ValueListApiModel> GetAllAsync(string collectionId);
        Task<ValueApiModel> GetAsync(string collectionId, string key);
        Task<ValueApiModel> CreateAsync(string collectionId, string value);
        Task<ValueApiModel> UpsertAsync(string collectionId, string key, string value, string etag);
        Task DeleteAsync(string collectionId, string key);
    }

    // TODO: handle retriable errors
    public class StorageAdapterClient : IStorageAdapterClient
    {
        // TODO: make it configurable, default to false
        private const bool ALLOW_INSECURE_SSL_SERVER = true;

        private readonly IHttpClient httpClient;
        private readonly ILogger log;
        private readonly string serviceUri;
        private readonly int timeout;

        public StorageAdapterClient(
            IHttpClient httpClient,
            IServicesConfig config,
            ILogger logger)
        {
            this.httpClient = httpClient;
            this.log = logger;
            this.serviceUri = config.StorageAdapterApiUrl;
            this.timeout = config.StorageAdapterApiTimeout;
        }

        // public async Task<ValueListApiModel> GetAllAsync(string collectionId)
        // {
        //     var response = await this.httpClient.GetAsync(
        //         this.PrepareRequest($"collections/{collectionId}/values"));

        //     this.ThrowIfError(response, collectionId, "");

        //     return JsonConvert.DeserializeObject<ValueListApiModel>(response.Content);
        // }

        public async Task<ValueApiModel> GetAsync(string collectionId, string key)
        {
            var response = await this.httpClient.GetAsync(
                this.PrepareRequest($"collections/{collectionId}/values/{key}"));

            this.ThrowIfError(response, collectionId, key);

            // Deserialize Http message into value API model (TODO: Throws JsonSerializationException)
            return JsonConvert.DeserializeObject<ValueApiModel>(response.Content,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        }

        public async Task<ValueApiModel> CreateAsync(string collectionId, string value)
        {
            var response = await this.httpClient.PostAsync(
                this.PrepareRequest($"collections/{collectionId}/values",
                    new ValueApiModel { Data = value }));

            this.ThrowIfError(response, collectionId, "");

            return JsonConvert.DeserializeObject<ValueApiModel>(response.Content);
        }

        public async Task<ValueApiModel> UpsertAsync(string collectionId, string key, string value, string etag)
        {
            var response = await this.httpClient.PutAsync(
                this.PrepareRequest($"collections/{collectionId}/values/{key}",
                    new ValueApiModel { Data = value, ETag = etag }));

            this.ThrowIfError(response, collectionId, key);

            return JsonConvert.DeserializeObject<ValueApiModel>(response.Content);
        }

        public async Task DeleteAsync(string collectionId, string key)
        {
            var response = await this.httpClient.DeleteAsync(
                this.PrepareRequest($"collections/{collectionId}/values/{key}"));

            this.ThrowIfError(response, collectionId, key);
        }

        private HttpRequest PrepareRequest(string path, ValueApiModel content = null)
        {
            var request = new HttpRequest();
            request.AddHeader(HttpRequestHeader.Accept.ToString(), "application/json");
            request.AddHeader(HttpRequestHeader.CacheControl.ToString(), "no-cache");
            request.AddHeader(HttpRequestHeader.UserAgent.ToString(), "Device Simulation " + this.GetType().FullName);
            request.SetUriFromString($"{this.serviceUri}/{path}");
            request.Options.EnsureSuccess = false;
            request.Options.Timeout = this.timeout;
            if (this.serviceUri.ToLowerInvariant().StartsWith("https:"))
            {
                request.Options.AllowInsecureSSLServer = ALLOW_INSECURE_SSL_SERVER;
            }

            if (content != null)
            {
                request.SetContent(content);
            }

            return request;
        }

        private void ThrowIfError(IHttpResponse response, string collectionId, string key)
        {
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new ResourceNotFoundException($"Resource {collectionId}/{key} not found.");
            }

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                throw new ConflictingResourceException(
                    $"Resource {collectionId}/{key} out of date. Reload the resource and retry.");
            }

            if (response.IsError)
            {
                throw new ExternalDependencyException(
                    new HttpRequestException($"Storage request error: status code {response.StatusCode}"));
            }
        }
    }
}
