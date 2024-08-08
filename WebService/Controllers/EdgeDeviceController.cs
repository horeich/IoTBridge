// Copyright (c) HOREICH GmbH. All rights reserved.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Horeich.Services.Diagnostics;
using Horeich.Services.EdgeDevice;
using Horeich.Services.Models;

namespace Horeich.IoTBridge.Controllers
{
    [ApiController]
    public class EdgeDeviceController : ControllerBase
    {
        private readonly ILogger _log;
        private readonly IEdgeDeviceManager _deviceManager;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger"></param>
        public EdgeDeviceController(
            IEdgeDeviceManager deviceManager,
            ILogger logger)
        {
            _deviceManager = deviceManager;
            _log = logger;     
        }

        [HttpPut("/{deviceId}/telemetry")]
        public async Task<IActionResult> PutAsync(string deviceId, [FromQuery]DeviceTelemetry telemetry)
        {
            // Send data asynchronously
            await _deviceManager.SendTelemetryAsync(deviceId, telemetry);

            // the data has reached its destination -> return Created
            return new CreatedResult("IoTBridge", deviceId);
        }
    }
}