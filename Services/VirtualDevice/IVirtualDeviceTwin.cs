using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

using System.Text;
using Newtonsoft.Json;
using System.Threading;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;

using Horeich.SensingSolutions.Services.Runtime;
using Horeich.SensingSolutions.Services.Exceptions;
using Horeich.SensingSolutions.Services.Http;
using Horeich.SensingSolutions.Services.Diagnostics;

using Horeich.Services.Models;


using Microsoft.Azure.DigitalTwins.Parser;

namespace Horeich.SensingSolutions.Services.VirtualDevice
{
    // TODO: own file
    public interface IVirtualDevice
    {
        Task SendDeviceTelemetryAsync(List<string> telemetryDataPoints, int timeout);

        Task SendDeviceTelemetryAsync(string binaryTelemetry, int timeout);

        Task<bool> IsActive();

        void Dispose();

        Task ConnectDevice(DeviceTwinCredentials credentials);

        Task CloseDevice();

        Task SyncDeviceFunctionsAsync(TelemetryApiModel payload);

        PropertyServiceModel GetDownlinkProperties();

        Task RetrieveDeviceTwin();
    }
}