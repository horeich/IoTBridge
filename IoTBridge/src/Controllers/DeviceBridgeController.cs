using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

using System.Text.Encodings.Web;
using System.Web;
using System.Net;
using Microsoft.Azure.Devices.Client;

using Horeich.SensingSolutions.Services.Diagnostics;
using Horeich.SensingSolutions.Services.VirtualDevice;

namespace Horeich.SensingSolutions.IoTBridge.Controllers
{
    [ApiController]
    public class DeviceBridgeController : ControllerBase
    {
        private readonly ILogger _log;
        private readonly IVirtualDeviceManager _deviceManager;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger"></param>
        public DeviceBridgeController(
            IVirtualDeviceManager deviceManager,
            ILogger logger)
        {
            _deviceManager = deviceManager;
            _log = logger;     
        }

        /// <summary>
        /// Generic IoT Device message attribute routing
        /// DEPRECATED
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        // [HttpPut("[action]")]
        // public async Task<IActionResult> IoTDevice([FromQuery]DeviceTelemetry telemetry)
        // {
        //     // Send data asynchronously
        //     await _deviceManager.BridgeDeviceAsync(telemetry);

        //     // the data has reached its destination -> return Ok
        //     return Ok(); 
        // }

        [HttpPut("{deviceId}/telemetry")]
        public async Task<IActionResult> PutAsync(string deviceId, [FromBody]DeviceTelemetry telemetry)
        {
            // Send data asynchronously
            await _deviceManager.BridgeDeviceAsync(deviceId, telemetry);

            // the data has reached its destination -> return Created
            return new CreatedResult("IoTBridge", deviceId);
        }
    }
}















//         // public IEnumerable<WeatherForecast> Get()
//         // {
//         //     var rng = new Random();
//         //     return Enumerable.Range(1, 5).Select(index => new WeatherForecast
//         //     {
//         //         Date = DateTime.Now.AddDays(index),
//         //         TemperatureC = rng.Next(-20, 55),
//         //         Summary = Summaries[rng.Next(Summaries.Length)]
//         //     })
//         //     .ToArray();
//         // }
//         // [HttpGet("[controller]/[action]")]
//         // public string SendDeviceData()
//         // {
//         //     string name = "Rick";
//         //     var numTimes = 2;
//         //     Console.WriteLine("Processing HttpGet request...");
//         //     return HtmlEncoder.Default.Encode($"Hello {name}, NumTimes is: {numTimes}");
//         // }

//         // /// <summary>
//         // /// 
//         // /// </summary>
//         // /// <param name="str"></param>
//         // /// <returns></returns>
// /// 
//         // [HttpPost]
//         // public async Task<DeviceLink> AsyncSendData()
//         // {
//         //     string scopeID ="2", deviceID = "3", primaryKey = "4";
//         //     Task<DeviceLink> task = DeviceLink.CreateAsync(scopeID, deviceID, primaryKey);
//         //     DeviceLink link = await task;
//         //     // DeviceLink link = task.Result;
//         //     return link;
//         // }

//         [HttpGet("[controller]/[action]")]
//         public string TestConnection()
//         {
//             string name = "Andy";
//             return HtmlEncoder.Default.Encode($"Hello {name}, sent from IoT link");
//         }

//         // [HttpPut("[controller]/[action]")]
//         // public void AsyncSendDataPut(string id, string sId, string pK)
//         // {
//         //     //await _deviceLink.AuthenticateDeviceAsync(id, sId, pK);

//         //     //await _deviceLink.SendDeviceTelemetryAsync(client, "test");
//         //    // return client;
//         // }

//         // [HttpGet("[action]")]
//         // public string GetList([ModelBinder]List<string> id)
//         // {
//         //     return string.Join(",", id);
//         // }