using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using Prometheus;

namespace Bluekiri.Diagnostics.Prometheus
{
    class HttpHandlerDiagnosticListenerObserver : IObserver<KeyValuePair<string, object>>
    {
        private readonly PropertyFetcher _requestFetcher;
        private readonly PropertyFetcher _responseFetcher;
        private readonly Counter _requestCounter;
        private readonly Summary _requestSummary;

        public HttpHandlerDiagnosticListenerObserver()
        {
            _requestFetcher = new PropertyFetcher("Request");
            _responseFetcher = new PropertyFetcher("Response");

            _requestCounter = Metrics.CreateCounter("outgoing_http_requests", "Outgoing HTTP Requests Count", 
                new CounterConfiguration
                {
                    SuppressInitialValue = true,
                    LabelNames = new[] { "host", "method", "endpoint", "status" }
                });

            _requestSummary = Metrics.CreateSummary("outgoing_http_requests_time", "Response times in milliseconds", new SummaryConfiguration
            {
                SuppressInitialValue = true,
                LabelNames = new[] { "host", "method", "endpoint", "status" }
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
            HttpRequestMessage request;
            HttpResponseMessage response;            
            switch (kvp.Key)
            {
                case "System.Net.Http.HttpRequestOut.Stop":
                    request = (HttpRequestMessage) _requestFetcher.Fetch(kvp.Value);
                    response = (HttpResponseMessage) _responseFetcher.Fetch(kvp.Value);
                    
                    var statusCode = response != null ? response.StatusCode.ToString() : "Unknown";
                    
                    _requestCounter
                        .WithLabels(request.RequestUri.Host, request.Method.Method, request.RequestUri.PathAndQuery, statusCode)
                        .Inc();                    

                    _requestSummary
                        .WithLabels(request.RequestUri.Host, request.Method.Method, request.RequestUri.PathAndQuery, statusCode)
                        .Observe(Activity.Current.Duration.TotalMilliseconds);
                    break;
                default:
                    return;
            }            
        }
    }
}
