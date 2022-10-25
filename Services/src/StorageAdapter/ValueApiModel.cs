// Copyright (c) Microsoft/ Horeich UG. All rights reserved.

using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;

namespace Horeich.SensingSolutions.Services.StorageAdapter
{
    public class ValueApiModel
    {
        [JsonProperty("Data")]
        public string Data { get; set; }

        [JsonProperty("Mapping")]
        public List<List<string>> Mapping { get; set; }

        [JsonProperty("HubId")]
        public string HubId { get; set; }

        [JsonProperty("Type")]
        public string Type { get; set; }

        [JsonProperty("SendInterval")]
        public int SendInterval { get; set; }

        [JsonProperty("Properties")]
        public Dictionary<string, string> Properties { get; set; }

        [JsonProperty("ETag")]
        public string ETag { get; set; }

        public ValueApiModel()
        {

        }
    }
}
