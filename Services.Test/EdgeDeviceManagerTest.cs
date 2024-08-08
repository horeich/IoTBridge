// Copyright (c) HOREICH GmbH, all rights reserved

using Horeich.Services.Runtime;
using Horeich.Services.StorageAdapter;
using Horeich.Services.EdgeDevice;
using Horeich.Services.Diagnostics;
using Microsoft.Identity.Client.Extensions.Msal;
using Moq;
using Xunit.Sdk;
using Services.Test.helpers;
using Horeich.Services.Exceptions;

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
            DeviceDataSerivceModel devicePropertiesModel = new DeviceDataSerivceModel
            {
                Id = "LEY3",
                Type = "device",
                Category = "oilsense",
                MappingVersion = "v3",
                SendInterval = 100,
            };

            MappingServiceModel mappingServiceModel = new MappingServiceModel
            {
                // Id = "oilsense.v3",
                Mapping = new List<MappingItem> {
                    new MappingItem { Id = "RSSI", TypeString = "System.Int32"},
                    new MappingItem { Id = "RAT", TypeString = "System.Int32"},
                    new MappingItem { Id = "Temperature", TypeString = "System.Single"},
                    new MappingItem { Id = "Time", TypeString = "System.String"}
                }
            };

            DeviceTelemetry deviceTelemetry = new DeviceTelemetry
            {
                Data = new List<string> {
                    "2.4",
                    "4.5"
                }
            };

            Mock<IStorageAdapterClient> _storageAdapterClientMock = new Mock<IStorageAdapterClient>();
            _storageAdapterClientMock.Setup(x => x.GetDevicePropertiesAsync(
                It.IsAny<string>())).ReturnsAsync(devicePropertiesModel);

            _storageAdapterClientMock.Setup(x => x.GetDeviceMappingAsync(
                It.IsAny<string>(),
                It.IsAny<string>())).ReturnsAsync(mappingServiceModel);
            
            Mock<IEdgeDevice> _edgeDeviceMock = new Mock<IEdgeDevice>();
            _edgeDeviceMock.Setup(x => x.SetOnlineStatusAsync(
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()));

            _edgeDeviceMock.Setup(x => x.SendDeviceTelemetryAsync(
                It.IsAny<List<string>>(),
                default
            ));

            string deviceKey = "38fhvheh3hchfgo=df=23123kn34ndjwq";

            Mock<IDataHandler> _dataHandlerMock = new Mock<IDataHandler>();
            _dataHandlerMock.Setup(x => x.GetString(
                It.IsAny<string>(),
                It.IsAny<string>()
            )).Returns(deviceKey);

            EdgeDeviceManager edgeDeviceManager = new EdgeDeviceManager(
                new MockFactory<IEdgeDevice>(_edgeDeviceMock),
                _storageAdapterClientMock.Object,
                _dataHandlerMock.Object,
                new ServicesConfig(),
                new Logger("UnitTest", "NLog", LogLevel.Debug));
            
            // Act
            var ex = await Record.ExceptionAsync(async() => await edgeDeviceManager.SendTelemetryAsync(deviceId, deviceTelemetry));
            
            // Assert
            // var ex = Assert.ThrowsAsync<System.Exception>(act);
            Assert.Null(ex);
        }

        [Theory]
        [InlineData("LEY3")]
        public async void SendTelemetryAsyncNoDataTest(string deviceId)
        {
            // Arrange
            DeviceDataSerivceModel devicePropertiesModel = new DeviceDataSerivceModel
            {
                Id = "LEY3",
                Type = "device",
                Category = "oilsense",
                MappingVersion = "v3",
                SendInterval = 100,
            };

            MappingServiceModel mappingServiceModel = new MappingServiceModel
            {
                // Id = "oilsense.v3",
                Mapping = new List<MappingItem> {
                    new MappingItem { Id = "RSSI", TypeString = "System.Int32"},
                    new MappingItem { Id = "RAT", TypeString = "System.Int32"},
                    new MappingItem { Id = "Temperature", TypeString = "System.Single"},
                    new MappingItem { Id = "Time", TypeString = "System.String"}
                }
            };

            Mock<IStorageAdapterClient> _storageAdapterClientMock = new Mock<IStorageAdapterClient>();
            _storageAdapterClientMock.Setup(x => x.GetDevicePropertiesAsync(
                It.IsAny<string>())).ReturnsAsync(devicePropertiesModel);

            _storageAdapterClientMock.Setup(x => x.GetDeviceMappingAsync(
                It.IsAny<string>(),
                It.IsAny<string>())).ReturnsAsync(mappingServiceModel);
            
            Mock<IEdgeDevice> _edgeDeviceMock = new Mock<IEdgeDevice>();
            _edgeDeviceMock.Setup(x => x.SetOnlineStatusAsync(
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()));

            _edgeDeviceMock.Setup(x => x.SendDeviceTelemetryAsync(
                It.IsAny<List<string>>(),
                default
            ));

            string deviceKey = "38fhvheh3hchfgo=df=23123kn34ndjwq";

            Mock<IDataHandler> _dataHandlerMock = new Mock<IDataHandler>();
            _dataHandlerMock.Setup(x => x.GetString(
                It.IsAny<string>(),
                It.IsAny<string>()
            )).Returns(deviceKey);

            EdgeDeviceManager edgeDeviceManager = new EdgeDeviceManager(
                new MockFactory<IEdgeDevice>(_edgeDeviceMock),
                _storageAdapterClientMock.Object,
                _dataHandlerMock.Object,
                new ServicesConfig(),
                new Logger("UnitTest", "NLog", LogLevel.Debug));

            DeviceTelemetry deviceTelemetry = new DeviceTelemetry();

            // Act
            Func<Task> act = async () => await edgeDeviceManager.SendTelemetryAsync(deviceId, deviceTelemetry);

            // Assert
            var ex = await Assert.ThrowsAsync<System.NullReferenceException>(act);
        }

        [Theory]
        [InlineData("LEY3")]
        public async void SendTelemetryAsyncStorageFailureTest(string deviceId)
        {
            // Arrange
            DeviceDataSerivceModel devicePropertiesModel = new DeviceDataSerivceModel
            {
                Id = "LEY3",
                Type = "device",
                Category = "oilsense",
                MappingVersion = "v3",
                SendInterval = 100,
            };

            MappingServiceModel mappingServiceModel = new MappingServiceModel
            {
                // Id = "oilsense.v3",
                Mapping = new List<MappingItem> {
                    new MappingItem { Id = "RSSI", TypeString = "System.Int32"},
                    new MappingItem { Id = "RAT", TypeString = "System.Int32"},
                    new MappingItem { Id = "Temperature", TypeString = "System.Single"},
                    new MappingItem { Id = "Time", TypeString = "System.String"}
                }
            };

            DeviceTelemetry deviceTelemetry = new DeviceTelemetry
            {
                Data = new List<string> {
                    "2.4",
                    "4.5"
                }
            };

            ResourceNotFoundException resourceException = new ResourceNotFoundException();
            Mock<IStorageAdapterClient> _storageAdapterClientMock = new Mock<IStorageAdapterClient>();
            _storageAdapterClientMock.Setup(x => x.GetDevicePropertiesAsync(
                It.IsAny<string>())).ThrowsAsync(resourceException);

            _storageAdapterClientMock.Setup(x => x.GetDeviceMappingAsync(
                It.IsAny<string>(),
                It.IsAny<string>())).ReturnsAsync(mappingServiceModel);
            
            Mock<IEdgeDevice> _edgeDeviceMock = new Mock<IEdgeDevice>();
            _edgeDeviceMock.Setup(x => x.SetOnlineStatusAsync(
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()));

            _edgeDeviceMock.Setup(x => x.SendDeviceTelemetryAsync(
                It.IsAny<List<string>>(),
                default
            ));

            string deviceKey = "38fhvheh3hchfgo=df=23123kn34ndjwq";

            Mock<IDataHandler> _dataHandlerMock = new Mock<IDataHandler>();
            _dataHandlerMock.Setup(x => x.GetString(
                It.IsAny<string>(),
                It.IsAny<string>()
            )).Returns(deviceKey);

            EdgeDeviceManager edgeDeviceManager = new EdgeDeviceManager(
                new MockFactory<IEdgeDevice>(_edgeDeviceMock),
                _storageAdapterClientMock.Object,
                _dataHandlerMock.Object,
                new ServicesConfig(),
                new Logger("UnitTest", "NLog", LogLevel.Debug));

            // Act
            Task result() => edgeDeviceManager.SendTelemetryAsync(deviceId, deviceTelemetry);
            // var ex = await Record.ExceptionAsync(async() => await edgeDeviceManager.SendTelemetryAsync(deviceId, deviceTelemetry));

            // Assert

            await Assert.ThrowsAsync<ResourceNotFoundException>(result);
        }

        [Theory]
        [InlineData("LEY3")]
        public async void SendTelemetryAsyncStorageFailureTest(string deviceId)
        {
            // Arrange
            DeviceDataSerivceModel devicePropertiesModel = new DeviceDataSerivceModel
            {
                Id = "LEY3",
                Type = "device",
                Category = "oilsense",
                MappingVersion = "v3",
                SendInterval = 100,
            };

            MappingServiceModel mappingServiceModel = new MappingServiceModel
            {
                // Id = "oilsense.v3",
                Mapping = new List<MappingItem> {
                    new MappingItem { Id = "RSSI", TypeString = "System.Int32"},
                    new MappingItem { Id = "RAT", TypeString = "System.Int32"},
                    new MappingItem { Id = "Temperature", TypeString = "System.Single"},
                    new MappingItem { Id = "Time", TypeString = "System.String"}
                }
            };

            DeviceTelemetry deviceTelemetry = new DeviceTelemetry
            {
                Data = new List<string> {
                    "2.4",
                    "4.5"
                }
            };

            ResourceNotFoundException resourceException = new ResourceNotFoundException();
            Mock<IStorageAdapterClient> _storageAdapterClientMock = new Mock<IStorageAdapterClient>();
            _storageAdapterClientMock.Setup(x => x.GetDevicePropertiesAsync(
                It.IsAny<string>())).ThrowsAsync(resourceException);

            _storageAdapterClientMock.Setup(x => x.GetDeviceMappingAsync(
                It.IsAny<string>(),
                It.IsAny<string>())).ReturnsAsync(mappingServiceModel);
            
            Mock<IEdgeDevice> _edgeDeviceMock = new Mock<IEdgeDevice>();
            _edgeDeviceMock.Setup(x => x.SetOnlineStatusAsync(
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()));

            _edgeDeviceMock.Setup(x => x.SendDeviceTelemetryAsync(
                It.IsAny<List<string>>(),
                default
            ));

            string deviceKey = "38fhvheh3hchfgo=df=23123kn34ndjwq";

            Mock<IDataHandler> _dataHandlerMock = new Mock<IDataHandler>();
            _dataHandlerMock.Setup(x => x.GetString(
                It.IsAny<string>(),
                It.IsAny<string>()
            )).Returns(deviceKey);

            EdgeDeviceManager edgeDeviceManager = new EdgeDeviceManager(
                new MockFactory<IEdgeDevice>(_edgeDeviceMock),
                _storageAdapterClientMock.Object,
                _dataHandlerMock.Object,
                new ServicesConfig(),
                new Logger("UnitTest", "NLog", LogLevel.Debug));

            // Act
            Task result() => edgeDeviceManager.SendTelemetryAsync(deviceId, deviceTelemetry);
            // var ex = await Record.ExceptionAsync(async() => await edgeDeviceManager.SendTelemetryAsync(deviceId, deviceTelemetry));

            // Assert

            await Assert.ThrowsAsync<ResourceNotFoundException>(result);
        }
    }
}

