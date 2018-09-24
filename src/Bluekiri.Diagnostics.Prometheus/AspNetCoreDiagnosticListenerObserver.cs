using Microsoft.AspNetCore.Http;
using Prometheus;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Bluekiri.Diagnostics.Prometheus
{
    internal class AspNetCoreDiagnosticListenerObserver : IObserver<KeyValuePair<string, object>>
    {
        private readonly PropertyFetcher _contextFetcher;
        private readonly Counter _requestCounter;
        private readonly Summary _requestSummary;

        public AspNetCoreDiagnosticListenerObserver()
        {
            _contextFetcher = new PropertyFetcher("HttpContext");
            _requestCounter = Metrics.CreateCounter("http_requests_mvc", "Requests Count", new CounterConfiguration
            {
                SuppressInitialValue = true,
                LabelNames = new[] { "method", "endpoint", "status" }
            });
            _requestSummary = Metrics.CreateSummary("http_requests_mvc_time", "Response times in milliseconds", new SummaryConfiguration
            {
                SuppressInitialValue = true,
                LabelNames = new[] { "method", "endpoint", "status" }
            });
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(KeyValuePair<string, object> kvp)
        {
            HttpContext context;
            switch (kvp.Key)
            {                
                case "Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop":
                    context = (HttpContext)_contextFetcher.Fetch(kvp.Value);

                    if (context != null)
                    {
                        _requestCounter
                            .WithLabels(context.Request.Method, context.Request.Path.Value, context.Response.StatusCode.ToString())
                            .Inc();

                        _requestSummary
                            .WithLabels(context.Request.Method, context.Request.Path.Value, context.Response.StatusCode.ToString())
                            .Observe(Activity.Current.Duration.TotalMilliseconds);
                    }

                    break;
                default:
                    break;
            }
        }
    }
}