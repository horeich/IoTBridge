
// Copyright (c) Horeich GmbH, all rights reserved

using System;
using System.Collections.Generic;
using Horeich.Services.StorageAdapter;

namespace Horeich.Services.VirtualDevice
{
    public class DeviceTwinCredentials
    {
        public string DeviceId { get; set; }
        public string DeviceKey { get; set; }
        public string HubString { get; set; }
    }

    public class DeviceDataModel
    {
        public string DeviceId { get; set; }
        public string HubConnString { get; set; }
        public string DeviceKey { get; set; }
        public int SendInterval { get; set; }
        public List<Tuple<string, Type>> Mapping { get; set; }
        public List<TypeItem> MappingScheme { get; set; }
        public string Type { get; set; }
        public Dictionary<string, string> Properties { get; set; }
    }

    public class TypeItem
    {
        public string Id { get; set; }
        public Type Type { get; set; }
    }

    public class DeviceTelemetry
    {
        public List<string> Data { get; set; }
    }
}