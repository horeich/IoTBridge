
// Copyright (c) Horeich UG

using System;
using System.Collections.Generic;

namespace Horeich.Services.VirtualDevice
{
    public class BinaryTelemetryMapping
    {
        public string DeviceId { get; set; }
        public string HubString { get; set; }
        public string DeviceKey { get; set; }
        public int SendInterval { get; set; }
        public List<Tuple<string, Type>> Mapping { get; set; }
        public string Type { get; set; }
        public Dictionary<string, string> Properties { get; set; }
    }
}