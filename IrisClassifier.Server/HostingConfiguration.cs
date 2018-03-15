using System;
using System.Diagnostics;
using Topshelf;
using Microsoft.Owin.Hosting;
using System.Configuration;

namespace IrisCliassifierService.Host
{

    public class HostingConfiguration : ServiceControl
    {
        public const string EventSource = "IrisClassifier.Server";

        private IDisposable _webApp;
        private int _port = 9000;

        public HostingConfiguration()
        {
            var portText = ConfigurationManager.AppSettings["host_port"];
            if (!string.IsNullOrEmpty(portText))
            {
                int port;
                if (int.TryParse(portText, out port))
                {
                    _port = port;
                }
            }
        }

        public bool Start(HostControl hostControl)
        {
            Trace.WriteLine("Starting service...");
            _webApp = WebApp.Start<OwinConfiguration>("http://localhost:" + _port.ToString() );
            return true;
        }

        public bool Stop(HostControl hostControl)
        {
            _webApp?.Dispose();
            return true;
        }
    }
}
