using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using Prometheus;
using Prometheus.Advanced;

namespace Bluekiri.Diagnostics.Prometheus
{
    class HttpHandlerDiagnosticListenerObserver : IObserver<KeyValuePair<string, object>>
    {
        private readonly HttpHandlerObserverConfig _config;
        private readonly PropertyFetcher _requestFetcher;
        private readonly PropertyFetcher _responseFetcher;
        private readonly Collector<Counter.Child> _requestCounter;
        private readonly Collector<Summary.Child> _requestSummary;

        public HttpHandlerDiagnosticListenerObserver(
            HttpHandlerObserverConfig config,
            Collector<Counter.Child> counter,
            Collector<Summary.Child> summary
            )
        {
            _config = config;
            _requestFetcher = new PropertyFetcher("Request");
            _responseFetcher = new PropertyFetcher("Response");

            _requestCounter = counter;
            _requestSummary = summary;
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
                    request = (HttpRequestMessage)_requestFetcher.Fetch(kvp.Value);
                    response = (HttpResponseMessage)_responseFetcher.Fetch(kvp.Value);

                    if (!ProcessRequest(request, out var filteredPath))
                        break;

                    var statusCode = response != null ? response.StatusCode.ToString() : "Unknown";

                    _requestCounter
                        .WithLabels(request.RequestUri.Host, request.Method.Method, filteredPath, statusCode)
                        .Inc();

                    _requestSummary
                        .WithLabels(request.RequestUri.Host, request.Method.Method, filteredPath, statusCode)
                        .Observe(Activity.Current.Duration.TotalMilliseconds);
                    break;
                default:
                    return;
            }
        }

        private bool ProcessRequest(HttpRequestMessage request, out string filteredPath)
        {
            filteredPath = request.RequestUri.AbsolutePath;

            if (_config.PathFilters.Count == 0) return true;

            var pathFilter = _config.PathFilters
                .FirstOrDefault(f => f.Match(request.RequestUri.AbsolutePath));

            if (pathFilter is null) return false;

            filteredPath = pathFilter.Path;

            return true;
        }
    }
}
