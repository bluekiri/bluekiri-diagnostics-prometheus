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
        private readonly Counter _requestTimeCounter;

        public HttpHandlerDiagnosticListenerObserver()
        {
            _requestFetcher = new PropertyFetcher("Request");
            _responseFetcher = new PropertyFetcher("Response");

            _requestCounter = Metrics.CreateCounter("http_requests", "HTTP Requests Count", 
                new CounterConfiguration
                {
                    LabelNames = new[] { "host", "method", "endpoint", "status" }
                });
            
            _requestTimeCounter = Metrics.CreateCounter("http_requests_time", "HTTP Requests Count", 
                new CounterConfiguration
                {
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
                    
                    _requestCounter.WithLabels(
                            request.RequestUri.Host, 
                            request.Method.Method, 
                            request.RequestUri.PathAndQuery,
                            statusCode).Inc();                    

                    _requestTimeCounter.WithLabels(
                            request.RequestUri.Host, 
                            request.Method.Method, 
                            request.RequestUri.PathAndQuery,
                            statusCode).Inc(Activity.Current.Duration.TotalMilliseconds);
                    break;
                default:
                    return;
            }            
        }
    }
}
