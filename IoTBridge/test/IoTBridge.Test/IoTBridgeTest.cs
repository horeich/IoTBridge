using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Threading.Tasks;
using Xunit;

using Horeich.SensingSolutions.Services.VirtualDevice;
using Horeich.SensingSolutions.Services.Diagnostics;
using Horeich.SensingSolutions.Services.Runtime;
using Horeich.SensingSolutions.Services.StorageAdapter;

namespace Services.Test
{
    [TestClass]
    public class IoTLinkTest
    {
        private readonly IVirtualDeviceManager _deviceManager;
        private readonly Mock<ILogger> _mockLogger;
        private readonly Mock<IDataHandler> _mockDataHandler;
        private readonly Mock<IStorageAdapterClient> _mockStorageClient;
        private readonly Mock<IServicesConfig> _mockConfig;

        public IoTLinkTest()
        {
            _mockLogger = new Mock<ILogger>();
            _mockDataHandler = new Mock<IDataHandler>();
            _mockStorageClient = new Mock<IStorageAdapterClient>();
            _mockConfig = new Mock<IServicesConfig>();
            _deviceManager = new VirtualDeviceManager(_mockStorageClient.Object, _mockDataHandler.Object, _mockConfig.Object, _mockLogger.Object);
        }
        
        [Theory]
        [InlineData("actrbl00101b8","40.50", 0)]
        [InlineData("", "", 5000)]
        [InlineData("actrbl00101b8","40,50", 5000)]
        [InlineData("actrbl00101b8","fourty", 5000)]
        public async Task DeviceLink_LinkDeviceAsync(string deviceIdentity, string payload, uint timeout)
        {
            // Arrange

            // Act    
            //await _deviceLink.LinkDeviceAsync(deviceIdentity, payload, timeout);

            // Assert
            //Assert.AreEqual("")

        }

        [Theory]
        [InlineData("actrbl00101b8","40.50", 0)]
        public async Task VirtualDeviceManager_BridgeDeviceAsync(
            string 
        )
        {
            DeviceApiModel model;


            await _deviceManager.BridgeDeviceAsync()
        }
    }
}
