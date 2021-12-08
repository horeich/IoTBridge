
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Horeich.SensingSolutions.Services.Diagnostics;
using Horeich.SensingSolutions.Services.VirtualDevice;
using Horeich.IoTBridge.v1.Models;
using Horeich.Services.Models;

namespace Horeich.IoTBridge.v1.Controllers
{
    [Route(Version.PATH)] // TODO: ExceptionFilterAttribute

    public class PropertyController : ControllerBase
    {
        private readonly ILogger _log;
        private readonly IVirtualDeviceManager _deviceManager;

        public PropertyController(
            IVirtualDeviceManager deviceManager,
            ILogger logger
        )
        {
            _deviceManager = deviceManager;
            _log = logger;
        }

        // example request: http://localhost:9021/v1/{deviceId}/properties
        [HttpGet("{deviceId}/properties")]
        public async Task<PropertyApiModel> GetAsync(string deviceId)
        {
            var properties = _deviceManager.GetDownlinkProperties(deviceId);
            var result = new PropertyApiModel(properties);
            return result;
        }
    }
}