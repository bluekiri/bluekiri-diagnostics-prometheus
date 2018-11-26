using System.Diagnostics;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Bluekiri.Diagnostics.Prometheus;
using Prometheus;

namespace SampleDiagnosedApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Creating the kestrel metrics server listening
            // into another port
            var metricsServer = new KestrelMetricServer(9303);
            metricsServer.Start();

            // Subscribe the diagnostic listeners that will
            // export Prometheus metrics
            DiagnosticListener.AllListeners
                .SubscribeDiagnosticListener(o =>
                {
                    o.AddAspNetCoreObserver();
                    o.AddHttpHandlerObserver();
                });

            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
