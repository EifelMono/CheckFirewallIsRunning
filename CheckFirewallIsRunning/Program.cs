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
        static TimeSpan StatusTimeout = IsTest ? TimeSpan.FromSeconds(10) : TimeSpan.FromMinutes(1);
        static TimeSpan StartTimeout = TimeSpan.FromMinutes(2);
        static ServiceController FirewallService = new ServiceController("MpsSvc");
        static CancellationTokenSource Running = new CancellationTokenSource();
        static async Task Main(string[] args)
        {
            Console.Title = "CheckFirewall";
            Console.WindowHeight = 5;
            Console.WindowWidth = 50;
            Console.CancelKeyPress += (sender, e) => Running.Cancel();
            try
            {
                while (!Running.IsCancellationRequested)
                {
                    await StartFirewallService();
                    Console.Title = "CheckFirewall Running";
                    Console.WriteLine($"{DateTime.Now.ToString("yyyy.MM.dd HH:mmm:ss")} {Console.Title}");
                    await WaitForLeaveFirewallStatus(ServiceControllerStatus.Running);
                    Console.Title = "CheckFirewall Starting";
                    Console.WriteLine(Console.Title);
                }
            }
            catch (TaskCanceledException) { }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        static async Task WaitForFirewallServiceStatus(ServiceControllerStatus status, TimeSpan? statusTimeout = default)
        {
            while (!Running.IsCancellationRequested)
            {
                await Task.Delay(statusTimeout ?? StatusTimeout, Running.Token).ConfigureAwait(false);
                if (FirewallService.Status == status)
                    break;
            }
        }

        static async Task WaitForLeaveFirewallStatus(ServiceControllerStatus status, TimeSpan? statusTimeout = default)
        {
            while (!Running.IsCancellationRequested)
            {
                await Task.Delay(statusTimeout ?? StatusTimeout, Running.Token).ConfigureAwait(false);
                if (FirewallService.Status != status)
                    break;
            }
        }

        static async Task StartFirewallService()
        {
            if (FirewallService.StartType != ServiceStartMode.Manual)
                ServiceHelper.ChangeStartMode(FirewallService, ServiceStartMode.Manual);
            if (FirewallService.Status != ServiceControllerStatus.Running)
            {
                FirewallService.Start();
                await WaitForFirewallServiceStatus(ServiceControllerStatus.Running, StartTimeout);
            }
        }
    }
}
