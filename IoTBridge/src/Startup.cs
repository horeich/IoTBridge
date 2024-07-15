// Copyright (c) Horeich GmbH. All rights reserved

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Autofac;
using Autofac.Extensions.DependencyInjection;

using Horeich.Services.Diagnostics;
using Horeich.Services.Runtime;
using Horeich.Services.VirtualDevice;
using Horeich.IoTBridge.Runtime;
using Horeich.IoTBridge.Middleware;
using Horeich.Services.StorageAdapter;
using Horeich.Services.Http;  

namespace Horeich.IoTBridge
{
    public class Startup
    {
        public Startup(IWebHostEnvironment env)
        {
            // The appsettings.json file will be copied to the binary folder
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true); //
            configBuilder.Build();
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
            // services.AddCors();
            // services.AddMvc().AddControllersAsServices();
            // services.AddControllers();
            // Add controller as services so they'll be resolved
            services.AddMvc().AddControllersAsServices();
            ContainerBuilder builder = new ContainerBuilder();
          
            // Add already set up services (DI container) to autofac container automatically (e.g. controller)
            builder.Populate(services);

            // Register device bridge controller
            builder.RegisterType<Controllers.DeviceBridgeController>().PropertiesAutowired();

            // Register logger first which is injected in all other instances
            builder.Register(c => new Logger(
                Uptime.ProcessId, 
                "NLog", 
                LogLevel.Trace))
                .As<ILogger>().SingleInstance();

            // Register data handler which reads configuration
            builder.Register(c => new DataHandler(
                c.Resolve<ILogger>()))
                .As<IDataHandler>().SingleInstance();

            // Configuration read only once
            builder.Register(c => new Config(
                c.Resolve<IDataHandler>()))
                .As<IConfig>().SingleInstance();

            // Http client (Instance per dependency)
            builder.Register(c => new HttpClient(
                c.Resolve<ILogger>()))
                .As<IHttpClient>(); // Instance per dependency

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

            // Build container
            ApplicationContainer = builder.Build();

            IConfig config = ApplicationContainer.Resolve<IConfig>();
            ILogger logger = ApplicationContainer.Resolve<ILogger>();

            // Set logging config
            logger.LogLevel = config.LogConfig.LogLevel;
            logger.ApplicationName = config.ServicesConfig.ApplicationNameKey;

            // Add the device bridge service
            return new AutofacServiceProvider(ApplicationContainer);
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline and add middleware.
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