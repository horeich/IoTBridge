// Copyright (c) HOREICH GmbH. All rights reserved.

using System;
using System.Threading.Tasks;
using Horeich.Services.Diagnostics;
using Horeich.Services.Models;

namespace Horeich.Services.EdgeDevice
{
    public interface IEdgeDeviceFactory<out T>
    {
        T Create(DeviceDataModel model, Func<object, EventArgs, Task> onTimeout, ILogger logger);
    }

    public class EdgeDeviceFactory : IEdgeDeviceFactory<EdgeDevice>
    {
        public EdgeDeviceFactory()
        {

        }

        public EdgeDevice Create(DeviceDataModel model, Func<object, EventArgs, Task> onTimeout,ILogger logger)
        {
            return new EdgeDevice(model, onTimeout, logger);
        }
    }
}