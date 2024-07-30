// Copyright (c) Horeich GmbH. All rights reserved.

using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;

namespace Horeich.Services.StorageAdapter
{
    public class DeviceDataSerivceModel
    {
        [JsonProperty(propertyName: "id")]
        public string Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("hubId")]
        public string HubId { get; set; }

        [JsonProperty("data")]
        public string Data { get; set; }

        [JsonProperty("mappingVersion")]
        public string MappingVersion { get; set; }

        [JsonProperty("mapping")]
        public List<List<string>> Mapping { get; set; }

        // public List<List<string>> Mapping { get; set; }
        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("sendInterval")]
        public int SendInterval { get; set; }

        [JsonProperty("properties")]
        public Dictionary<string, string> Properties { get; set; }

        [JsonProperty("ETag")]
        public string ETag { get; set; }

        public DeviceDataSerivceModel()
        {

        }
    }
}
