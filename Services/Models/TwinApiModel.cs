
// Copyright (c) Horeich UG

using System;
using System.Collections.Generic;

using Microsoft.Azure.DigitalTwins.Parser;
using Newtonsoft.Json;

namespace Horeich.Services.Models
{
    public class TwinApiModel
    {
        [JsonProperty("DeviceId")]
        //[Required]
        public string DeviceId { get; set; }

        [JsonProperty("TelemetryMapping")]
        //[Required]
        public List<LwM2MAdapterModel> TelemetryMapping { get; set; }

        public TwinApiModel(TwinServiceModel model)
        {
            this.DeviceId = model.DeviceId;

            this.TelemetryMapping = model.TelemetryMapping;


            // TODO: Metadata
        }
    }

    public class TwinServiceModel
    {
        public string DeviceId { get; set; }
        public string HubString { get; set; }
        public string DeviceKey { get; set; }
        public int SendInterval { get; set; }

        // [JsonProperty(PropertyName = "@TelemetryMapping")]
        // public Dictionary<string, DTEntityKind> TelemetryMapping { get; set; }
        public List<LwM2MAdapterModel> TelemetryMapping { get; set; }
        public string Type { get; set; }
        public Dictionary<string, string> Properties { get; set; }
    }
}