// Copyright (c) HOREICH GmbH, all rights reserved

using Horeich.Services.Runtime;
using Horeich.Services.StorageAdapter;
using Horeich.Services.VirtualDevice;
using Horeich.Services.Diagnostics;
using Microsoft.Identity.Client.Extensions.Msal;
using Moq;
using Xunit.Sdk;

namespace Services.Test
{
    public class EdgeDeviceManagerTest
    {
        EdgeDeviceManager _edgeDeviceManager;

        // DeviceDataModel model = new DeviceDataModel();
        // model.DeviceId = "LEY3";
        // model.DeviceKey = "ZqNKe0NRtGREbvjhF+Lbe+6Jq2PDOMZDaM7sEPgX5sc=";
        // model.HubConnString = "iotc-2fee92c0-e1fe-4a11-b9ac-0e826cac1889.azure-devices.net";
        // model.SendInterval = 10;
        // model.Type = "levelsense";

        [Theory]
        [InlineData("LEY3")]
        public async void SendTelemetryAsyncTest(string deviceId)
        {
            // Arrange
            // Make sure storage adapter is up and running and device credentials are up to date in the database
            DevicePropertiesServiceModel devicePropertiesModel = new DevicePropertiesServiceModel
            {
                Id = "LEY3",
                Type = "device",
                Category = "oilsense",
                MappingVersion = "v3",
                SendInterval = 100,
            };

            MappingServiceModel mappingServiceModel = new MappingServiceModel
            {
                Mapping = new List<MappingItem> {
                    new MappingItem { Id = "RSSI", TypeString = "System.Int32"},
                    new MappingItem { Id = "RAT", TypeString = "System.Int32"}
                }
            };

            ServicesConfig servicesConfig = new ServicesConfig();

            Mock<StorageAdapterClient> _storageAdapterClientMock = new Mock<StorageAdapterClient>();
            _storageAdapterClientMock.Setup(x => x.GetDevicePropertiesAsync(
                It.IsAny<string>())).ReturnsAsync(devicePropertiesModel);

            _storageAdapterClientMock.Setup(x => x.GetDeviceMappingAsync(
                It.IsAny<string>(),
                It.IsAny<string>())).ReturnsAsync(mappingServiceModel);
            
            Mock<EdgeDevice> _edgeDeviceMock = new Mock<EdgeDevice>();
            _edgeDeviceMock.Setup(x => x.SetOnlineStatusAsync(
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()));

            _edgeDeviceMock.Setup(x => x.SendDeviceTelemetryAsync(
                It.IsAny<List<string>>(),
                default
            )).Returns

            string deviceKey = "testkey";

            Mock<DataHandler> _dataHandlerMock = new Mock<DataHandler>();
            _dataHandlerMock.Setup(x => x.GetString(
                It.IsAny<string>(),
                It.IsAny<string>()
            )).Returns(deviceKey);

            EdgeDeviceManager edgeDeviceManager = new EdgeDeviceManager(
                _storageAdapterClientMock.Object,
                _dataHandlerMock.Object,
                new ServicesConfig(),
                new Logger("UnitTest", "NLog", LogLevel.Debug));
            

            DeviceTelemetry deviceTelemetry = new DeviceTelemetry();

            // Act
            await _edgeDeviceManager.SendTelemetryAsync(deviceId, deviceTelemetry);

            // Assert

        }
    }
}

