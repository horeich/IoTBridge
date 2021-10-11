// Copyright (c) Horeich UG. All rights reserved

using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

using Horeich.SensingSolutions.Services.Runtime;
using Horeich.SensingSolutions.Services.Diagnostics;
using Horeich.SensingSolutions.IoTBridge.Runtime;

namespace Horeich.SensingSolutions.IoTBridge
{
    /// <summary>
    /// Make the Program class static to exclude it from code coverage (pass through class)
    /// Instead make the startup class fully injected and unit-testable
    /// </summary>
    public static class Program
    {
        
        public static void Main(string[] args)
        {

            IConfig config = new Config(new DataHandler(new LocalLogger(Uptime.ProcessId, LogLevel.Info)));

            var host = new WebHostBuilder()
                .UseUrls("http://*:" + config.Port) // the port to listen to
                .UseKestrel(options => 
                { 
                    options.AllowSynchronousIO = false;
                })
                .UseStartup<Startup>()
                .ConfigureKestrel(serverOptions =>
                {
                    serverOptions.ConfigureEndpointDefaults(listenOptions =>
                    {
                        //listenOptions.
                    });
                })
                .UseIISIntegration() // use internal IIS reverse proxy for externally hosted application
                .Build();

            // Start Kestrel web server
            host.Run();
        }
    }
}
