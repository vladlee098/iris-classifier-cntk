using log4net;
using System;
using Topshelf;

namespace IrisCliassifierService.Host
{
    static class Program
    {
        private static readonly ILog log = LogManager.GetLogger("main");

        static int Main()
        {
            log4net.Config.XmlConfigurator.Configure();

            log.Info("Starting up the service...");

            var rc = HostFactory.Run(x =>                                
            {
                x.Service<HostingConfiguration>(sc =>                                
                {
                    sc.ConstructUsing(name => new HostingConfiguration());           
                    sc.WhenStarted((s, hostControl) => s.Start(hostControl));                     
                    sc.WhenStopped((s, hostControl) => s.Stop(hostControl));                      
                });
                x.UseLog4Net();
                x.RunAsLocalSystem();
                x.StartManually();

                x.SetServiceName(HostingConfiguration.EventSource);
                x.SetDisplayName(HostingConfiguration.EventSource);
                x.SetDescription(HostingConfiguration.EventSource + " using WinApi, Owin and Topshelf");
            });

            var exitCode = (int)Convert.ChangeType(rc, rc.GetTypeCode());
            //Environment.ExitCode = exitCode;


            //var exitCode = HostFactory.Run(x =>
            //{
            //    log4net.Config.XmlConfigurator.Configure();
            //    x.UseLog4Net();

            //    x.SetStartTimeout(TimeSpan.FromSeconds(60));
            //    x.SetStopTimeout(TimeSpan.FromSeconds(60));
            //    x.Service<HostingConfiguration>();
            //    x.RunAsLocalSystem();
            //    x.StartManually();
            //    x.SetServiceName(HostingConfiguration.EventSource);
            //    x.SetDisplayName(HostingConfiguration.EventSource);
            //    x.SetDescription(HostingConfiguration.EventSource + " using WinApi, Owin and Topshelf");

            //    log.Info("Setting up the service...");
            //});
            return (int)exitCode;
        }
    }
}
