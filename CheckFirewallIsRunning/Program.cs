using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CheckFirewallIsRunning
{
    class Program
    {
        static bool IsTest = false;
        static TimeSpan StatusWait = IsTest ? TimeSpan.FromSeconds(10) : TimeSpan.FromMinutes(1);
        static ServiceController FirewallService = new ServiceController("MpsSvc");
        static CancellationTokenSource Running = new CancellationTokenSource();
        static async Task Main(string[] args)
        {
            Console.CancelKeyPress += (sender, e) => Running.Cancel();
            try
            {
                while (!Running.IsCancellationRequested)
                {
                    await StartFireWallService();
                    await WaitForLeaveFireWallStatus(ServiceControllerStatus.Running);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        static async Task WaitForFirewallServiceStatus(ServiceControllerStatus status, TimeSpan? statusWait = default)
        {
            while (!Running.IsCancellationRequested)
            {
                await Task.Delay(statusWait ?? StatusWait, Running.Token).ConfigureAwait(false);
                if (FirewallService.Status == status)
                    break;
            }
        }

        static async Task WaitForLeaveFireWallStatus(ServiceControllerStatus status, TimeSpan? statusWait = default)
        {
            while (!Running.IsCancellationRequested)
            {
                await Task.Delay(statusWait ?? StatusWait, Running.Token).ConfigureAwait(false);
                if (FirewallService.Status != status)
                    break;
            }
        }

        static async Task StartFireWallService()
        {
            if (FirewallService.StartType != ServiceStartMode.Manual)
                ServiceHelper.ChangeStartMode(FirewallService, ServiceStartMode.Manual);
            if (FirewallService.Status != ServiceControllerStatus.Running)
            {
                FirewallService.Start();
                await WaitForFirewallServiceStatus(ServiceControllerStatus.Running);
            }
        }
    }
}
