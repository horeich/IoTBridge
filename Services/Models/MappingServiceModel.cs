// Copyright (c) HOREICH GmbH. All rights reserved.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Horeich.Services.Models
{
    public class MappingServiceModel
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; } // e.g. OIL2

        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; } // e.g. oilsense

        [JsonProperty(PropertyName = "version")]
        public string Version { get; set; } // e.g. oilsense

        [JsonProperty(PropertyName = "mapping")]
        public List<MappingItem> Mapping { get; set; }
        
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class MappingItem
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "type")]        
        public string TypeString { get; set; }
    }
}
