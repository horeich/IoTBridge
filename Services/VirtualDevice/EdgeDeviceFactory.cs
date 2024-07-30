// Copyright (c) HOREICH GmbH. All rights reserved.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Horeich.Services.Diagnostics;
using Horeich.Services.Runtime;

namespace Horeich.Services.VirtualDevice
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