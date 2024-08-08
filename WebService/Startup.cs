// Copyright (c) HOREICH GmbH. All rights reserved.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration.Json;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Horeich.Services.Diagnostics;
using Horeich.Services.Runtime;
using Horeich.Services.EdgeDevice;
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

        // BackgroundJobServer _backgroundServer;
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
            services.AddMvc().AddControllersAsServices(); // use routing

            // New container
            ContainerBuilder builder = new ContainerBuilder();

            // Add already set up services (DI container) to autofac container automatically (e.g. controller)
            builder.Populate(services);

            // Register device bridge controller
            builder.RegisterType<Controllers.EdgeDeviceController>().PropertiesAutowired();

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

            // Device factory (for unit testing)
            builder.Register(c => new EdgeDeviceFactory())
                .As<IEdgeDeviceFactory<IEdgeDevice>>().SingleInstance();

            // Virtual device manager
            builder.Register(c => new EdgeDeviceManager(
                c.Resolve<IEdgeDeviceFactory<IEdgeDevice>>(),
                c.Resolve<IStorageAdapterClient>(),
                c.Resolve<IDataHandler>(),
                c.Resolve<IConfig>().ServicesConfig,
                c.Resolve<ILogger>())).As<IEdgeDeviceManager>().SingleInstance();

            // Build container
            ApplicationContainer = builder.Build();

            IConfig config = ApplicationContainer.Resolve<IConfig>();
            ILogger logger = ApplicationContainer.Resolve<ILogger>();

            // Set logging config
            logger.LogLevel = config.LogConfig.LogLevel;
            logger.ApplicationName = config.ServicesConfig.ApplicationName;

            // Add the device bridge service
            return new AutofacServiceProvider(ApplicationContainer);
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline and add middleware.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        public void Configure(
            IApplicationBuilder app,
            IWebHostEnvironment env)
        {
            // Show exception page during development
            if (env.IsDevelopment()) // TODO:!!!!
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



//  services.Configure<QuartzOptions>(options =>
//             {
//                 options.Scheduling.IgnoreDuplicates = true; // default: false
//                 options.Scheduling.OverWriteExistingData = true; // default: true
//             });

//             services.AddQuartz(q =>
//             {
//                 // handy when part of cluster or you want to otherwise identify multiple schedulers
//                 q.SchedulerId = "Scheduler-Core";

//                 // q.UseMicrosoftDependencyInjectionJobFactory();

//                 // we take this from appsettings.json, just show it's possible
//                 // q.SchedulerName = "Quartz ASP.NET Core Sample Scheduler";

//                 // these are the defaults
//                 q.UseSimpleTypeLoader();
//                 q.UseInMemoryStore();
//                 q.UseDefaultThreadPool(tp =>
//                 {
//                     tp.MaxConcurrency = 10;
//                 });

//                 var jobKey = new JobKey("HelloWorldJob");

//                 // Register the job with the DI container
//                 q.A<EdgeDeviceTimeoutJob>(opts => opts.WithIdentity(jobKey));

//                 // q.ScheduleJob<EdgeDeviceTimeoutJob>(trigger => trigger
//                 //     .WithIdentity("Combined Configuration Trigger")
//                 //     .StartAt(DateBuilder.EvenSecondDate(DateTimeOffset.UtcNow.AddSeconds(7)))
//                 //     .WithSimpleSchedule(x => x.WithIntervalInSeconds(20).RepeatForever())
//                 //     .WithDescription("my awesome trigger configured for a job with single call")
//                 // );

//                 // add some listeners
//                 // q.AddSchedulerListener<SampleSchedulerListener>();
//                 // q.AddJobListener<SampleJobListener>(GroupMatcher<JobKey>.GroupEquals(jobKey.Group));
//                 // q.AddTriggerListener<SampleTriggerListener>();

//             });

//             // services.AddQuartzHostedService(
//             //         q => q.WaitForJobsToComplete = true);

//             // services.AddTransient<ExampleJob>();

//             // services.Configure<SampleOptions>(Configuration.GetSection("Sample"));
//             // services.AddOptions<QuartzOptions>()
//             // .Configure<IOptions<SampleOptions>>((options, dep) =>
//             // {
//             // if (!string.IsNullOrWhiteSpace(dep.Value.CronSchedule))
//             // {
//             //     var jobKey = new JobKey("options-custom-job", "custom");
//             //     options.AddJob<ExampleJob>(j => j.WithIdentity(jobKey));
//             //     options.AddTrigger(trigger => trigger
//             //     .WithIdentity("options-custom-trigger", "custom")
//             //     .ForJob(jobKey)
//             //     .WithCronSchedule(dep.Value.CronSchedule));
//             // }
//             // });

//             services.AddQuartzHostedService(options =>
//             {
//                 // when shutting down we want jobs to complete gracefully
//                 options.WaitForJobsToComplete = true;
//             });




            // edgeDeviceManager.StartRecurringJobAsync();

            // Hangfire configuration

            // services.AddHangfire(c => c
            //     .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            //     .UseSimpleAssemblyNameTypeSerializer()
            //     .UseRecommendedSerializerSettings()
            //     .UseMemoryStorage()
            // );

            // builder.RegisterType<CurrencyRatesJob>().InstancePerBackgroundJob();


            // ApplicationContainer = builder.Build();


            // GlobalConfiguration.Configuration
            //     // .UseActivator(new ContainerJobActivator(ApplicationContainer))
            //     .UseAutofacActivator(ApplicationContainer)
            //     .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            //     .UseSimpleAssemblyNameTypeSerializer()
            //     .UseRecommendedSerializerSettings()
            //     .UseMemoryStorage();

            // JobActivator.Current = new AutofacJobActivator(ApplicationContainer);


            // MemoryStorageOptions memoryStorageOptions = new()
            // {
            //     FetchNextJobTimeout = TimeSpan.FromHours(10)
            // };

            // // MemoryStorageOptions memoryStorageOptions = new()
            // // {
            // //     FetchNextJobTimeout = TimeSpan.FromHours(10)
            // // };

            // services.AddHangfire(options => options.UseMemoryStorage(memoryStorageOptions));
            // services.AddHangfireServer();

            // // ...

            // services.AddTransient<MyJobService>();




            // ...

            // Use IRecurringJobManager for recurring jobs
            // ApplicationContainer.Resolve<IRecurringJobManager>().AddOrUpdate<CurrencyRatesJob>("test-job", job => job.Execute(), Cron.Minutely);

            // Use IBackgroundJobClient interface for regular jobs
            //ApplicationContainer.Resolve<IBackgroundJobClient>().Enqueue(() => Console.WriteLine("Hello, world"));

            // RecurringJob.AddOrUpdate( "process2", () => Console.WriteLine("Test output"), Cron.Minutely);

            // IEdgeDeviceManager edgeDeviceManager = ApplicationContainer.Resolve<IEdgeDeviceManager>();