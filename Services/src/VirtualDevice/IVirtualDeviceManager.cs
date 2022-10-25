using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Newtonsoft.Json;

using Horeich.SensingSolutions.Services.Runtime;
using Horeich.SensingSolutions.Services.Exceptions;
using Horeich.SensingSolutions.Services.Diagnostics;

using System.Collections.Concurrent;
using System.Collections.Specialized;

using System.IO;

using Horeich.Services.Models;

using Azure.DigitalTwins.Core;
using Azure.Core;
using Azure.Identity;
using Azure;
using Microsoft.Azure.DigitalTwins.Parser;


using System.Web;

using Horeich.SensingSolutions.Services.StorageAdapter;

namespace Horeich.SensingSolutions.Services.VirtualDevice
{   
    public interface IVirtualDeviceManager
    {
        Task BridgeDeviceAsync(string deviceId, DeviceTelemetry model);

        Task Register(string deviceId);

        Task<TwinServiceModel> Register(string deviceId, LwM2MResources resources);

        Task Unregister(string deviceId);

        Task SolveBinaryTelemetry(string deviceId, BinaryTelemetryApiModel telemetry);
    }
}