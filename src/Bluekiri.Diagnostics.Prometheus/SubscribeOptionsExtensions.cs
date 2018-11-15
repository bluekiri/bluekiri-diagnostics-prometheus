using Prometheus;
using System;

namespace Bluekiri.Diagnostics.Prometheus
{
    public static class SubscribeOptionsExtensions
    {

        public static void AddHttpHandlerObserver(this SubscribeOptions o)
        {
            o.AddHttpHandlerObserver(null);
        }

        /// <summary>
        /// Adds the HttpHandler obsever for the HttpHandlerDiagnosticListener
        /// </summary>
        /// <param name="o"></param>
        /// <param name="configure">Configuration options for the observer</param>
        public static void AddHttpHandlerObserver(this SubscribeOptions o, Action<HttpHandlerObserverConfig> configure)
        {
            var config = new HttpHandlerObserverConfig();
            configure?.Invoke(config);

            var counter = Metrics.CreateCounter("outgoing_http_requests", "Outgoing HTTP Requests Count",
                new CounterConfiguration
                {
                    SuppressInitialValue = true,
                    LabelNames = new[] { "host", "method", "endpoint", "status" }
                });

            var summary = Metrics.CreateSummary("outgoing_http_requests_time", "Response times in milliseconds",
                new SummaryConfiguration
                {
                    SuppressInitialValue = true,
                    LabelNames = new[] { "host", "method", "endpoint", "status" }
                });

            o.AddSubscriber("HttpHandlerDiagnosticListener",
                new HttpHandlerDiagnosticListenerObserver(config, counter, summary));
        }

        /// <summary>
        /// Adds the AspNetCore observer for the Microsoft.AspNetCore listener
        /// </summary>
        /// <param name="o"></param>
        public static void AddAspNetCoreObserver(this SubscribeOptions o)
        {
            o.AddSubscriber("Microsoft.AspNetCore", new AspNetCoreDiagnosticListenerObserver());
        }
    }
}
