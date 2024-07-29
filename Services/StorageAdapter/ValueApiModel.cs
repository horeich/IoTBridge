// Copyright (c) Microsoft/ Horeich UG. All rights reserved.

using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;

namespace Horeich.Services.StorageAdapter
{
    public class DevicePropertiesServiceModel
    {
        [JsonProperty("Data")]
        public string Data { get; set; }

        [JsonProperty("mappingVersion")]
        public List<List<string>> Mapping { get; set; }

        [JsonProperty("hubId")]
        public string HubId { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("sendInterval")]
        public int SendInterval { get; set; }

        // [JsonProperty("Properties")]
        // public Dictionary<string, string> Properties { get; set; }

        [JsonProperty("ETag")]
        public string ETag { get; set; }

        public DevicePropertiesServiceModel()
        {

        }
    }
}
