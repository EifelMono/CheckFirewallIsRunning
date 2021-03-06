﻿using System;
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
        static bool IsTest = Argument<bool>("Test");
        static TimeSpan StatusTimeout = IsTest ? TimeSpan.FromSeconds(10) : TimeSpan.FromMinutes(1);
        static TimeSpan StartTimeout = TimeSpan.FromMinutes(2);
        static ServiceController FirewallService = new ServiceController("MpsSvc");
        static CancellationTokenSource Running = new CancellationTokenSource();
        static async Task Main(string[] args)
        {
            Message("program start");
            Console.WindowHeight = 5;
            Console.WindowWidth = 50;
            Console.CancelKeyPress += (sender, e) => Running.Cancel();
            try
            {
                while (!Running.IsCancellationRequested)
                {
                    await StartFirewallService();
                    await WaitForLeaveFirewallStatus(ServiceControllerStatus.Running);
                }
            }
            catch (TaskCanceledException) { }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            Message("program end");
        }

        static void Message(string message)
        {
            Console.Title = $"{nameof(CheckFirewallIsRunning)} {message}".Trim();
            Console.WriteLine($"{DateTime.Now.ToString("yyyy.MM.dd HH:mmm:ss")} {message}");
        }

        static async Task WaitForFirewallServiceStatus(ServiceControllerStatus status, TimeSpan? statusTimeout = default)
        {
            while (!Running.IsCancellationRequested)
            {
                await Task.Delay(statusTimeout ?? StatusTimeout, Running.Token);
                if (FirewallService.Status == status)
                    break;
            }
        }

        static async Task WaitForLeaveFirewallStatus(ServiceControllerStatus status, TimeSpan? statusTimeout = default, int maxRuns = 5)
        {
            int run = 0;
            while (!Running.IsCancellationRequested)
            {
                if (++run > maxRuns)
                    return;
                await Task.Delay(statusTimeout ?? StatusTimeout, Running.Token);
                if (FirewallService.Status != status)
                    break;
            }
            Message($"leave status={status}");
        }

        static async Task StartFirewallService()
        {
            if (FirewallService.StartType != ServiceStartMode.Manual)
                ServiceHelper.ChangeStartMode(FirewallService, ServiceStartMode.Manual);
            if (FirewallService.Status != ServiceControllerStatus.Running)
            {
                Message("starting");
                FirewallService.Start();
                await WaitForFirewallServiceStatus(ServiceControllerStatus.Running, StartTimeout);
                Message(FirewallService.Status == ServiceControllerStatus.Running ? "running" : "not running");
            }
        }

        /// <summary>
        /// Commandline Key=Value
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public static T Argument<T>(string key)
        {
            try
            {
                foreach (var a in Environment.GetCommandLineArgs())
                {
                    var kv = a.Split('=');
                    if (kv.Length == 2 && kv[0] == key)
                        return (T)Convert.ChangeType(kv[1], typeof(T));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return default;
        }
    }
}
