// Copyright (c) HOREICH GmbH. All rights reserved.

using Horeich.Services.Diagnostics;
using Horeich.Services.VirtualDevice;
using Moq;

namespace Services.Test.helpers
{
    public class MockFactory<T> : IEdgeDeviceFactory<T> where T : class
    {
        private readonly Mock<T> mock;

        public MockFactory(Mock<T> mock)
        {
            this.mock = mock;
        }

        public T Create(DeviceDataModel model, Func<object, EventArgs, Task> onTimeout, ILogger logger)
        {
            return this.mock.Object;
        }

    }
}
