using Microsoft.VisualStudio.TestTools.UnitTesting;
using Prometheus;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;

namespace Bluekiri.Diagnostics.Prometheus.Tests
{
    [TestClass]
    public class HttpHandlerDiagnosticListenerObserverTests
    {

        [TestMethod]
        public void OnNext_RequestObserverd_OutgoingHttpRequestStopEventReceived()
        {
            // Arrange
            var observer = new HttpHandlerDiagnosticListenerObserver();
            var listener = new DiagnosticListener("TestListener");
            listener.Subscribe(observer);

            var request = new HttpRequestMessage
            {
                Method = new HttpMethod("GET"),
                RequestUri = new Uri("http://test/api/elements")
            };

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = request
            };


            // Act
            var activity = new Activity("System.Net.Http.HttpRequestOut");
            listener.StartActivity(activity, new
            {
                Request = request
            });
            listener.StopActivity(activity, new
            {
                Request = request,
                Response = response
            });

            // Assert
            var requestCounter = (Counter) observer.GetType().GetField("_requestCounter",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(observer);
            var requestSummary = (Summary) observer.GetType().GetField("_requestSummary",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(observer);

            var counterMetrics = requestCounter.Collect();
            var summaryMetrics = requestSummary.Collect();

            Assert.AreEqual(1, counterMetrics.First().metric.Count);
            Assert.AreEqual(1, counterMetrics.First().metric[0].counter.value);
            Assert.AreEqual(1, summaryMetrics.First().metric.Count);
            Assert.IsNotNull(summaryMetrics.First().metric[0].summary);
        }

        [TestMethod]
        public void OnNext_MetricsHaveLabels_OutgoingHttpRequestStopReceived()
        {
            // Arrange
            var observer = new HttpHandlerDiagnosticListenerObserver();
            var listener = new DiagnosticListener("TestListener");
            listener.Subscribe(observer);

            var request = new HttpRequestMessage
            {
                Method = new HttpMethod("GET"),
                RequestUri = new Uri("http://test/api/elements")
            };

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = request
            };


            // Act
            var activity = new Activity("System.Net.Http.HttpRequestOut");
            listener.StartActivity(activity, new
            {
                Request = request
            });
            listener.StopActivity(activity, new
            {
                Request = request,
                Response = response
            });

            // Assert
            var requestCounter = (Counter)observer.GetType().GetField("_requestCounter",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(observer);            

            var counterMetrics = requestCounter.Collect();            

            Assert.AreEqual(4, counterMetrics.First().metric[0].label.Count);
            Assert.AreEqual("host", counterMetrics.First().metric[0].label[0].name);
            Assert.AreEqual("method", counterMetrics.First().metric[0].label[1].name);
            Assert.AreEqual("endpoint", counterMetrics.First().metric[0].label[2].name);
            Assert.AreEqual("status", counterMetrics.First().metric[0].label[3].name);
        }

        [TestMethod]
        public void OnNext_UnknownStatusCodObserved_OutgoingHttpRequestStopReceived()
        {
            // Arrange
            var observer = new HttpHandlerDiagnosticListenerObserver();
            var listener = new DiagnosticListener("TestListener");
            listener.Subscribe(observer);

            var request = new HttpRequestMessage
            {
                Method = new HttpMethod("GET"),
                RequestUri = new Uri("http://test/api/elements")
            };

            // Act
            var activity = new Activity("System.Net.Http.HttpRequestOut");
            listener.StartActivity(activity, new
            {
                Request = request
            });
            listener.StopActivity(activity, new
            {
                Request = request
            });
            
            // Assert
            var requestCounter = (Counter)observer.GetType().GetField("_requestCounter",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(observer);            

            var counterMetrics = requestCounter.Collect();            
            
            Assert.IsTrue(counterMetrics.First().metric.Any(m => m.label[3].value == "Unknown"));
        }
    }
}
