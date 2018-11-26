using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Prometheus;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace Bluekiri.Diagnostics.Prometheus
{
    internal class AspNetCoreDiagnosticListenerObserver : IObserver<KeyValuePair<string, object>>
    {
        private readonly PropertyFetcher _contextFetcherAfterAction;
        private readonly PropertyFetcher _contextFetcherRequestStop;
        private readonly PropertyFetcher _routeFetcher;
        private readonly Counter _requestCounter;
        private readonly Summary _requestSummary;
        private ConcurrentDictionary<string, string> _requestPaths;

        public AspNetCoreDiagnosticListenerObserver()
        {
            _contextFetcherAfterAction = new PropertyFetcher("httpContext");
            _contextFetcherRequestStop = new PropertyFetcher("HttpContext");
            _routeFetcher = new PropertyFetcher("routeData");

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
            _requestPaths = new ConcurrentDictionary<string, string>();
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
            RouteData routeData;

            string pathValue;
            string connectionId;

            switch (kvp.Key)
            {

                case "Microsoft.AspNetCore.Mvc.AfterAction":

                    context = (HttpContext)_contextFetcherAfterAction.Fetch(kvp.Value);
                    routeData = (RouteData)_routeFetcher.Fetch(kvp.Value);
                    if (context == null)
                    {
                        break;
                    }

                    pathValue = context.Request.Path.Value;

                    if (routeData != null)
                    {
                        var controllerValue = string.Empty;
                        var actionValue = string.Empty;
                        if (routeData.Values.ContainsKey("controller"))
                        {
                            controllerValue = routeData.Values["controller"]?.ToString();
                        }
                        if (routeData.Values.ContainsKey("action"))
                        {
                            actionValue = routeData.Values["action"]?.ToString();
                        }
                        pathValue = $"{controllerValue}/{actionValue}";
                    }

                    _requestCounter
                        .WithLabels(context.Request.Method, pathValue, context.Response.StatusCode.ToString())
                        .Inc();

                    connectionId = context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpConnectionFeature>()?.ConnectionId;
                    if (!string.IsNullOrEmpty(connectionId))
                    {
                        _requestPaths.TryAdd(connectionId, pathValue);
                    }
                    break;

                case "Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop":
                    context = (HttpContext)_contextFetcherRequestStop.Fetch(kvp.Value);

                    connectionId = context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpConnectionFeature>()?.ConnectionId;

                    if (string.IsNullOrEmpty(connectionId) || !_requestPaths.TryRemove(connectionId, out pathValue))
                    {
                        break;
                    }

                    _requestSummary
                        .WithLabels(context.Request.Method, pathValue, context.Response.StatusCode.ToString())
                        .Observe(Activity.Current.Duration.TotalMilliseconds);

                    break;
                default:
                    break;
            }
        }

    }
}