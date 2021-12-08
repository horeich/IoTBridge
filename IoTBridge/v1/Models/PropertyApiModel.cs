
// Copyright (c) Horeich UG

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Horeich.Services.Models;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json.Linq;

namespace Horeich.IoTBridge.v1.Models
{
    public class LwM2MValue
    {
        public object value { get; set; }
        public string type { get; set; }

        public LwM2MValue(object value, string type)
        {
            this.value = value;
            this.type = type;
        }
    }
    public sealed class PropertyApiModel
    {
        /// Note: keys in uri must have the same name as dictionary name
        [JsonProperty(PropertyName = "Properties")]
        public Dictionary<String, LwM2MValue> Properties { get; set; }

        public PropertyApiModel(PropertyServiceModel model)
        {
            this.Properties = new Dictionary<string, LwM2MValue>();
            // Deserialize in
            foreach (KeyValuePair<string, object> property in model.Properties)
            {

                JToken value;
                var valueJson = JObject.Parse(property.Value.ToString());
                if (valueJson.TryGetValue("value", out value))
                {
                    // TODO: LWM2M Formatting class
                    LwM2MValue rawValue = new LwM2MValue(value.ToString(), "INTEGER");
                    this.Properties.Add(property.Key, rawValue);
                }
                // JToken value;
                // json.TryGetValue("value", out value);
                // this.Properties = model.Properties;
            }
        }

       //public List<string> Telemetry { get; set; }

        // [JsonProperty(PropertyName = "Telemetry")]
        // public String Telemetry{ get; set; }
    }
}