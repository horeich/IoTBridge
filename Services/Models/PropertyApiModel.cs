
// Copyright (c) Horeich UG

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Horeich.Services.Models
{
    public class PropertyApiModel
    {
        /// Note: keys in uri must have the same name as dictionary name
        [JsonProperty(PropertyName = "Properties")]
        //public String telemetry { get; set; }
        //public String telemetry { get; set; }
        public Dictionary<String, Object> Telemetry { get; set; }


       //public List<string> Telemetry { get; set; }

        // [JsonProperty(PropertyName = "Telemetry")]
        // public String Telemetry{ get; set; }
    }
}