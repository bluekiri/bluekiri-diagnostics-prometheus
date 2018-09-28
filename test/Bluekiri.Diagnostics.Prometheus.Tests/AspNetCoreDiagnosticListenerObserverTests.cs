using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Prometheus;
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

            // Act                                    

            // Create the activity, start it and immediately stop it. This will
            // cause the OnNext method in the observer to be called with the events
            // Microsoft.AspNetCore.Hosting.HttpRequestIn.Start and
            // Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop. In addition, 
            // Activity.Current will be managed by the listener.
            var activity = new Activity("Microsoft.AspNetCore.Hosting.HttpRequestIn");
            listener.StartActivity(activity, new { HttpContext = context });
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
    }
}
