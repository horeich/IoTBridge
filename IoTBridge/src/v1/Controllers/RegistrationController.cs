using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

using Horeich.SensingSolutions.Services.Exceptions;

using System.Text.Encodings.Web;
using System.Web;
using System.Net;
using Microsoft.Azure.Devices.Client;

using Horeich.SensingSolutions.Services.Diagnostics;
using Horeich.SensingSolutions.Services.VirtualDevice;
using Horeich.Services.Models;

namespace Horeich.IoTBridge.v1.Controllers
{

   

    [ApiController]
    [Route(Version.PATH)] // ExceptionsFilter
    public class RegistrationController : Controller
    {
        private readonly ILogger _log;
        private readonly IVirtualDeviceManager _deviceManager;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger"></param>
        public RegistrationController(
            IVirtualDeviceManager deviceManager,
            ILogger logger)
        {
            _deviceManager = deviceManager;
            _log = logger;     
        }

        ///
        /// example call: 
        [HttpPost("{deviceId}/resources")]
        // [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<ActionResult> CreateAsync(string deviceId, [FromQuery]LwM2MResources resources)
        {
            // if (deviceId == "FAT1") // only ncessary if taken from body
            // {
            //     throw new InvalidInputException(); // Throws internal server error 500 
            // }

            TwinServiceModel result = await _deviceManager.Register(deviceId, resources);
            //return new TwinApiModel(result); // 204 no content if null // 200 OK on success
            return new CreatedResult(nameof(CreateAsync), new TwinApiModel(result));
            
            //return new CreatedResult("value", new TwinApiModel(result));
        }

        [HttpPost("{deviceId}")]
        // [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<ActionResult> CreateAsync(string deviceId)
        {
            await _deviceManager.Register(deviceId);
            return new CreatedResult(nameof(CreateAsync), "");
        }

        [HttpDelete("{deviceId}")]
        public async Task<IActionResult> DeleteAsync(string deviceId)
        {
            await _deviceManager.Unregister(deviceId);

            // the data has reached its destination -> return Created
            return new AcceptedResult(); // see OMA specification
        }
    }
}








