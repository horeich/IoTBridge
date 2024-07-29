
// using Quartz;
// using Quartz.Impl;
using Horeich.Services.Runtime;
using Horeich.Services.Diagnostics;
using System.Threading.Tasks;
// using FluentScheduler;
using Hangfire;

namespace Horeich.Services.VirtualDevice
{

    
    // public class DemoJobFactory : IJobFactory
    // {
    //     private readonly IServiceProvider _serviceProvider;

    //     public DemoJobFactory(IServiceProvider serviceProvider)
    //     {
    //         _serviceProvider = serviceProvider;
    //     }

    //     public FluentScheduler.IJob GetJobInstance<T>() where T : FluentScheduler.IJob
    //     {
    //         throw new System.NotImplementedException();
    //     }

    //     public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
    //     {
    //         return _serviceProvider.GetService<DemoJob>();
    //     }

    //     public void ReturnJob(IJob job)
    //     {
    //         var disposable = job as IDisposable;
    //         disposable?.Dispose();
    //     }
    // }
    public class EdgeDeviceTimeoutJob
    {
        private readonly ILogger _logger;
        private readonly IEdgeDeviceManager _edgeDeviceManager;
        public EdgeDeviceTimeoutJob(EdgeDeviceManager edgeDeviceManager, ILogger logger)
        {
            _logger = logger;
            _edgeDeviceManager = edgeDeviceManager;
        }

        public async Task Execute(ILogger logger)
        {
            int i = 0;
            i++;
            // This is executed in EdgeDevice's context
            // await _semaphore.WaitAsync();
            // try
            // {
            //     EdgeDevice edgeDevice = (EdgeDevice)sender;
            //     EdgeDevice foundDevice = _devices[edgeDevice.Id];
            //     if (foundDevice == null)
            //     {
            //         // We do not expect this
            //         throw new NullReferenceException(); // TODO: needed?
            //     }

            //     CancellationTokenSource cts = new CancellationTokenSource();
            //     cts.CancelAfter(TimeSpan.FromSeconds(10));
            //     await foundDevice.UpdatePropertyAsync("Status", "offline", cts.Token);

            //     foundDevice.ResetDeviceTimeout();
            //     _devices[edgeDevice.Id] = null;
            // }
            // catch (Exception e)
            // {

            // }
            // finally
            // {
            //     _semaphore.Release();
            // }
            // _logger.Debug("Test output job");

        }
    }
}