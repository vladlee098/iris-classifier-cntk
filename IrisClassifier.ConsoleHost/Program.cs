using System;
using log4net;
using Microsoft.Owin.Hosting;

namespace IrisClassifier.ConsoleHost
{
    class Program
    {
        private static readonly ILog log = LogManager.GetLogger("main");

        static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();

            log.Info("Starting up the service, listening on port 9000");

            string baseAddress = "http://localhost:9000/";
            using (WebApp.Start<Startup>(url: baseAddress))
            {
                Console.WriteLine("The host is started.");
                Console.WriteLine("Press 'enter' to quit.");
                Console.ReadLine();
            }
        }
    }
}
