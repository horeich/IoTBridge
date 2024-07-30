
// using System.Reflection;
// using Autofac;
// using Autofac.Extensions.DependencyInjection;
// using Microsoft.Extensions.DependencyInjection;

// using Horeich.Services.Diagnostics;
// using Horeich.Services.Runtime;
// using Horeich.CoAPEndpoint.Runtime;
// using Horeich.Services.IoTBridge;
// using Horeich.Services.Http;
// using Horeich.CoAPEndpoint.Resources;

// using Com.AugustCellars.CoAP.Server;
// using Com.AugustCellars.CoAP;
// using Com.AugustCellars.CoAP.Server.Resources;

// namespace Horeich.CoAPEndpoint
// {
//     public class DependencyResolution
//     {
//         public static IContainer Setup()
//         {
//             var builder = new ContainerBuilder();

//             SetupCustomRules(builder);

//             var container = builder.Build();
//             Factory.RegisterContainer(container);

//             return container;
//         }

//         // Autowire interfaces to classes from all the assemblies
//         private static void AutowireAssemblies(ContainerBuilder builder)
//         {
//             var assembly = Assembly.GetEntryAssembly();
//             builder.RegisterAssemblyTypes(assembly).AsImplementedInterfaces();

//             // Auto-wire additional assemblies
//             assembly = typeof(IServicesConfig).GetTypeInfo().Assembly;
//             builder.RegisterAssemblyTypes(assembly).AsImplementedInterfaces();
//         }

//         private static void SetupCustomRules(ContainerBuilder builder)
//         {
//             //services.AddCors();
//             //services.AddMvc().AddControllersAsServices();
//             //services.AddControllers();
//             // Add controller as services so they'll be resolved
//             services.AddMvc().AddControllersAsServices();
//             var builder = new ContainerBuilder();
          
//             // Add already set up services (DI container) to autofac container automatically (e.g. controller)
//             builder.Populate(services);

//             // Register device bridge controller
//             builder.RegisterType<Controllers.DeviceBridgeController>().PropertiesAutowired();

//             // Register data handler and configuration as single instance
//             builder.Register(c => new DataHandler(new LocalLogger(Uptime.ProcessId, LogLevel.Info))).As<IDataHandler>().SingleInstance();
//             builder.Register(c => new Config(c.Resolve<IDataHandler>())).As<IConfig>().SingleInstance();
            
//             // Logger
//             builder.Register(c => new Logger(Uptime.ProcessId, c.Resolve<IConfig>().LogConfig)).As<ILogger>().SingleInstance();

//             // Http client (Instance per dependency)
//             builder.Register(c => new HttpClient(c.Resolve<ILogger>())).As<IHttpClient>();

//             // Storage adapter client
//             builder.Register(c => new StorageAdapterClient(
//                 c.Resolve<IHttpClient>(),
//                   c.Resolve<IConfig>().ServicesConfig,
//                 c.Resolve<ILogger>())).As<IStorageAdapterClient>().SingleInstance();

//             // Virtual device manager
//             builder.Register(c => new VirtualDeviceManager(
//                 c.Resolve<IStorageAdapterClient>(),
//                 c.Resolve<IDataHandler>(),
//                 c.Resolve<IConfig>().ServicesConfig,
//                 c.Resolve<ILogger>())).As<IVirtualDeviceManager>().SingleInstance();
//             // services.AddSingleton<IDeviceLink, DeviceLink>(); // deprecated

//             // Build container
//             ApplicationContainer = builder.Build();

//             // Create singletons (config and logger) -> already created in Program.cs
//             // IConfig has its own logger
//             //ApplicationContainer.Resolve<IConfig>();
//             ApplicationContainer.Resolve<ILogger>();

//             // Add the device bridge service
//             return new AutofacServiceProvider(ApplicationContainer);
//     }

//     public interface IFactory
//     {
//         T Resolve<T>();
//     }

//     public class Factory : IFactory
//     {
//         private static IContainer container;

//         public static void RegisterContainer(IContainer c)
//         {
//             container = c;
//         }

//         public T Resolve<T>()
//         {
//             return container.Resolve<T>();
//         }
//     }
// }