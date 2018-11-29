using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Prometheus;
using Prometheus.Advanced;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Bluekiri.Diagnostics.Prometheus.Tests
{
    [TestClass]
    public class AspNetCoreDiagnosticListenerObserverTests
    {
        [TestMethod]
        public void OnNext_RequestObserved_RequestStopEventReceived()
        {
            // Arrange            
            var observer = new AspNetCoreDiagnosticListenerObserver();
            var listener = new DiagnosticListener("TestListener");
            listener.Subscribe(observer);

            var context = new DefaultHttpContext();
            context.Request.Method = "GET";
            context.Request.Path = new PathString("/api/test");
            context.Response.StatusCode = 200;

            var httpConnectionFeature = new HttpConnectionFeature
            {
                ConnectionId = "1234"
            };
            context.Features.Set<IHttpConnectionFeature>(httpConnectionFeature);

            var routeData = new RouteData();
            routeData.Values.Add("controller", "test");
            routeData.Values.Add("action", "get");
            // Act                                    

            // Create the activity, start it and immediately stop it. This will
            // cause the OnNext method in the observer to be called with the events
            // Microsoft.AspNetCore.Hosting.HttpRequestIn.Start and
            // Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop. In addition, 
            // Activity.Current will be managed by the listener.
            var activity = new Activity("Microsoft.AspNetCore.Hosting.HttpRequestIn");
            listener.StartActivity(activity, new { HttpContext = context });
            listener.Write("Microsoft.AspNetCore.Mvc.AfterAction", new { httpContext = context, routeData });
            listener.StopActivity(activity, new { HttpContext = context });


            // Assert     
            var requestCounter = (Counter)observer.GetType().GetField("_requestCounter",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(observer);
            var requestSummary = (Summary)observer.GetType().GetField("_requestSummary",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(observer);

            var counterMetrics = requestCounter.Collect();
            var summaryMetrics = requestSummary.Collect();

            Assert.AreEqual(1, counterMetrics.First().metric.Count);
            Assert.AreEqual(1, counterMetrics.First().metric[0].counter.value);
            Assert.AreEqual(1, summaryMetrics.First().metric.Count);
            Assert.IsNotNull(summaryMetrics.First().metric[0].summary);

            
        }


        [TestMethod]
        public void OnNext_RequestObserver_With_Null_Action_Value_And_Null_Controller_Value()
        {
            // Arrange
            var observer = new AspNetCoreDiagnosticListenerObserver();

            var registry = new DefaultCollectorRegistry();
            var factory = Metrics.WithCustomRegistry(registry);

            var listener = new DiagnosticListener("TestListener2");
            listener.Subscribe(observer);
    
            

            var context = new DefaultHttpContext();
            context.Request.Method = "GET";
            context.Request.Path = new PathString("/api/test2");
            context.Response.StatusCode = 200;

            var httpConnectionFeature = new HttpConnectionFeature
            {
                ConnectionId = "12345"
            };
            context.Features.Set<IHttpConnectionFeature>(httpConnectionFeature);

            var routeData = new RouteData();
            routeData.Values.Add("controller",null);
            routeData.Values.Add("action", null);

            // Act
            var activity = new Activity("Microsoft.AspNetCore.Hosting.HttpRequestIn");
            listener.StartActivity(activity, new { HttpContext = context });
            listener.Write("Microsoft.AspNetCore.Mvc.AfterAction", new { httpContext = context, routeData });
            listener.StopActivity(activity, new { HttpContext = context });


            // Assert     
            var requestCounter = (Counter)observer.GetType().GetField("_requestCounter",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(observer);
            var requestSummary = (Summary)observer.GetType().GetField("_requestSummary",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(observer);

            var counterMetrics = requestCounter.Collect();
            var summaryMetrics = requestSummary.Collect();
            
            

            Assert.IsTrue(counterMetrics.First().metric.Any(p => p.label[1].value == "/api/test2"));
            Assert.IsNotNull(summaryMetrics.First().metric[0].summary);

        }
    }
}
