// Copyright (c) Horeich GmbH, all rights reserved

using Horeich.Services.EdgeDevice;
using Moq;

namespace Services.Test
{
    public class EdgeDeviceManagerITest
    {
        EdgeDeviceManager _edgeDeviceManager;

        [Theory]
        [InlineData("LEY3")]
        public async void SendTelemetryAsyncITest(string deviceId)
        {
            // Arrange
            // Make sure storage adapter is up and running and device credentials are up to date in the database

            // EdgeDeviceManager edgeDeviceManager = new EdgeDeviceManager() // TODO:
            
            DeviceTelemetry deviceTelemetry = new DeviceTelemetry();

            // Act
            await _edgeDeviceManager.SendTelemetryAsync(deviceId, deviceTelemetry);
        }
    }
}

