// Copyright (c) Horeich UG (andreas.reichle@horeich.de)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;

using Microsoft.Extensions.FileProviders;

using Autofac;
using Autofac.Extensions.DependencyInjection; // Inject services in Autofac

using Horeich.SensingSolutions.Services.Diagnostics;
using Horeich.SensingSolutions.Services.Runtime;
using Horeich.SensingSolutions.Services.VirtualDevice;
using Horeich.SensingSolutions.IoTBridge.Runtime;
using Horeich.SensingSolutions.IoTBridge.Middleware;
using Horeich.SensingSolutions.Services.StorageAdapter;
using Horeich.SensingSolutions.Services.Http;

using System.Reflection;

namespace Horeich.SensingSolutions.IoTBridge
{

    //IConfig config = new Config(new ConfigData(new Logger(Uptime.ProcessId, LogLevel.Info)));
    public class Startup
    {
        public Startup(IWebHostEnvironment env)
        {
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true); //
            configBuilder.Build();
        }
        public Startup(IConfiguration configuration)
        {
        //     Configuration = configuration;
        }

        public IConfigurationRoot Configuration { get; }

        public IContainer ApplicationContainer { set; private get; }

        /// <summary>
        /// This method is called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            //services.AddCors();
            //services.AddMvc().AddControllersAsServices();
            //services.AddControllers();
            // Add controller as services so they'll be resolved
            services.AddMvc().AddControllersAsServices();
            var builder = new ContainerBuilder();

            // Add already set up services (DI container) to autofac container automatically (e.g. controller)
            builder.Populate(services);

            // Register device bridge controller
            builder.RegisterType<Controllers.DeviceBridgeController>().PropertiesAutowired();

            // Register data handler and configuration as single instance
            builder.Register(c => new DataHandler(new LocalLogger(Uptime.ProcessId, LogLevel.Info))).As<IDataHandler>().SingleInstance();
            builder.Register(c => new Config(c.Resolve<IDataHandler>())).As<IConfig>().SingleInstance();

            // Logger
            builder.Register(c => new Logger(Uptime.ProcessId, c.Resolve<IConfig>().LogConfig)).As<ILogger>().SingleInstance();

            // Http client (Instance per dependency)
            builder.Register(c => new HttpClient(c.Resolve<ILogger>())).As<IHttpClient>();

            // Storage adapter client
            builder.Register(c => new StorageAdapterClient(
                c.Resolve<IHttpClient>(),
                  c.Resolve<IConfig>().ServicesConfig,
                c.Resolve<ILogger>())).As<IStorageAdapterClient>().SingleInstance();

            // Virtual device manager
            builder.Register(c => new VirtualDeviceManager(
                c.Resolve<IStorageAdapterClient>(),
                c.Resolve<IDataHandler>(),
                c.Resolve<IConfig>().ServicesConfig,
                c.Resolve<ILogger>())).As<IVirtualDeviceManager>().SingleInstance();
            // services.AddSingleton<IDeviceLink, DeviceLink>(); // deprecated

            // Build container
            ApplicationContainer = builder.Build();

            // Create singletons (config and logger) -> already created in Program.cs
            // IConfig has its own logger
            //ApplicationContainer.Resolve<IConfig>();
            ApplicationContainer.Resolve<ILogger>();

            // Add the device bridge service
            return new AutofacServiceProvider(ApplicationContainer);
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        /// <param name="loggerFactory"></param>
        public void Configure(
            IApplicationBuilder app,
            IWebHostEnvironment env)//,
           // ILoggerFactory loggerFactory)
        {

            // Show exception page during development
            if (env.IsDevelopment())
            {
                // Note: Initialize after UseDeveloperExceptionPage
                app.UseDeveloperExceptionPage();
                app.ConfigureCustomExceptionMiddleware();
            }
            else
            {
                // Note: Initialize before UseExceptionHandler
                app.ConfigureCustomExceptionMiddleware();
                //app.UseExceptionHandler("/Error");
            }

            //app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();
            //app.UseMvc();

            app.UseEndpoints(endpoints =>
            {
                // Use attribute routing
                endpoints.MapControllers();
            });
        }
    }
}