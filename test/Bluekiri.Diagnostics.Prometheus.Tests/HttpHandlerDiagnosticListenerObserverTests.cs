using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Prometheus;
using Prometheus.Advanced;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace Bluekiri.Diagnostics.Prometheus.Tests
{
    [TestClass]
    public class HttpHandlerDiagnosticListenerObserverTests
    {

        public TestContext TestContext { get; set; }

        [TestMethod]
        public void OnNext_RequestObserverd_OutgoingHttpRequestStopEventReceived()
        {
            // Arrange            
            var counterMock = new Mock<Collector<Counter.Child>>(MockBehavior.Loose,
                "the_counter", "counter_help", new string[] { "host", "method", "endpoint", "status" }, true);
            var summaryMock = Metrics.CreateSummary(TestContext.TestName + "summary", "test_summary_help",
                new SummaryConfiguration
                {
                    LabelNames = new string[] { "host", "method", "endpoint", "status" },
                    SuppressInitialValue = true
                });

            var observer = new HttpHandlerDiagnosticListenerObserver(
                new HttpHandlerObserverConfig(),
                counterMock.Object, summaryMock);

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
            var counterMetrics = counterMock.Object.Collect();
            var summaryMetrics = summaryMock.Collect();

            Assert.AreEqual(1, counterMetrics.First().metric.Count);
            Assert.AreEqual(1, counterMetrics.First().metric[0].counter.value);
            Assert.AreEqual(1, summaryMetrics.First().metric.Count);
            Assert.IsNotNull(summaryMetrics.First().metric[0].summary);
        }

        [TestMethod]
        public void OnNext_MetricsHaveLabels_OutgoingHttpRequestStopReceived()
        {
            // Arrange
            var counterMock = new Mock<Collector<Counter.Child>>(MockBehavior.Loose,
                "the_counter", "counter_help", new string[] { "host", "method", "endpoint", "status" }, true);
            var summaryMock = Metrics.CreateSummary(TestContext.TestName + "summary", "test_summary_help",
                new SummaryConfiguration
                {
                    LabelNames = new string[] { "host", "method", "endpoint", "status" },
                    SuppressInitialValue = true
                });
            var observer = new HttpHandlerDiagnosticListenerObserver(
                new HttpHandlerObserverConfig(),
                counterMock.Object, summaryMock);
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
            var counterMetrics = counterMock.Object.Collect();

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
            var counterMock = new Mock<Collector<Counter.Child>>(MockBehavior.Loose,
                "the_counter", "counter_help", new string[] { "host", "method", "endpoint", "status" }, true);

            var summaryMock = Metrics.CreateSummary(TestContext.TestName + "summary", "test_summary_help",
                new SummaryConfiguration
                {
                    LabelNames = new string[] { "host", "method", "endpoint", "status" },
                    SuppressInitialValue = true
                });            

            var observer = new HttpHandlerDiagnosticListenerObserver(
                new HttpHandlerObserverConfig(),
                counterMock.Object, summaryMock);
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
            var counterMetrics = counterMock.Object.Collect();

            Assert.IsTrue(counterMetrics.First().metric.Any(m => m.label[3].value == "Unknown"));
        }

        [TestMethod]
        public void OnNext_RequestNotObserved_FilterSetForSpecificPath()
        {
            // Arrange
            var config = new HttpHandlerObserverConfig();
            config.AddPathFilter("/a/b");

            var counterMock = new Mock<Collector<Counter.Child>>(MockBehavior.Loose,
                "the_counter", "counter_help", new string[] { "host", "method", "endpoint", "status" }, true);

            var summaryMock = Metrics.CreateSummary(TestContext.TestName + "summary", "test_summary_help",
                new SummaryConfiguration
                {
                    LabelNames = new string[] { "host", "method", "endpoint", "status" },
                    SuppressInitialValue = true
                });

            var observer = new HttpHandlerDiagnosticListenerObserver(
                config,
                counterMock.Object, summaryMock);

            var listener = new DiagnosticListener("TestListener");
            listener.Subscribe(observer);

            var request = new HttpRequestMessage
            {
                Method = new HttpMethod("GET"),
                RequestUri = new Uri("http://test.com/c/a/b")
            };

            // Act
            var activity = new Activity("System.Net.Http.HttpRequestOut");
            listener.StartActivity(activity, new { Request = request });
            listener.StopActivity(activity, new { Request = request });

            // Assert
            var counterMetrics = counterMock.Object.Collect();
            Assert.IsFalse(counterMetrics.First().metric.Any());
        }

        [TestMethod]
        public void OnNext_RequestObserved_RequestedPathWithParameter()
        {
            // Arrange
            var config = new HttpHandlerObserverConfig();
            config.AddPathFilter("/a/b/@");

            var counterMock = new Mock<Collector<Counter.Child>>(MockBehavior.Loose,
                "the_counter", "counter_help", new string[] { "host", "method", "endpoint", "status" }, true);
            var summaryMock = Metrics.CreateSummary(TestContext.TestName + "summary", "test_summary_help",
                new SummaryConfiguration
                {
                    LabelNames = new string[] { "host", "method", "endpoint", "status" },
                    SuppressInitialValue = true
                });

            var observer = new HttpHandlerDiagnosticListenerObserver(
                config,
                counterMock.Object, summaryMock);

            var listener = new DiagnosticListener("TestListener");
            listener.Subscribe(observer);

            var request = new HttpRequestMessage
            {
                Method = new HttpMethod("GET"),
                RequestUri = new Uri("http://test.com/a/b/param1")
            };

            // Act
            var activity = new Activity("System.Net.Http.HttpRequestOut");
            listener.StartActivity(activity, new { Request = request });
            listener.StopActivity(activity, new { Request = request });

            // Assert
            var counterMetrics = counterMock.Object.Collect();
            Assert.IsTrue(counterMetrics.First().metric.Any());
        }

        [TestMethod]
        public void OnNext_RequestObserved_RequestedPathWithParameterAndSubpath()
        {
            // Arrange
            var config = new HttpHandlerObserverConfig();
            config.AddPathFilter("/a/b/@/c");

            var counterMock = new Mock<Collector<Counter.Child>>(MockBehavior.Loose,
                "the_counter", "counter_help", new string[] { "host", "method", "endpoint", "status" }, true);
            var summaryMock = Metrics.CreateSummary(TestContext.TestName + "summary", "test_summary_help",
                new SummaryConfiguration
                {
                    LabelNames = new string[] { "host", "method", "endpoint", "status" },
                    SuppressInitialValue = true
                });

            var observer = new HttpHandlerDiagnosticListenerObserver(
                config,
                counterMock.Object, summaryMock);

            var listener = new DiagnosticListener("TestListener");
            listener.Subscribe(observer);

            var request = new HttpRequestMessage
            {
                Method = new HttpMethod("GET"),
                RequestUri = new Uri("http://test.com/a/b/param1/c")
            };

            // Act
            var activity = new Activity("System.Net.Http.HttpRequestOut");
            listener.StartActivity(activity, new { Request = request });
            listener.StopActivity(activity, new { Request = request });

            // Assert
            var counterMetrics = counterMock.Object.Collect();
            Assert.IsTrue(counterMetrics.First().metric.Any());
        }

        [TestMethod]
        public void OnNext_LongestPathMatched_TwoFilters()
        {
            // Arrange
            var config = new HttpHandlerObserverConfig();
            config.AddPathFilter("/a/b");
            config.AddPathFilter("/a/b/@/c");


            var counterMock = new Mock<Collector<Counter.Child>>(MockBehavior.Loose,
                "the_counter", "counter_help", new string[] { "host", "method", "endpoint", "status" }, true);
            var summaryMock = Metrics.CreateSummary(TestContext.TestName + "summary", "test_summary_help",
                new SummaryConfiguration
                {
                    LabelNames = new string[] { "host", "method", "endpoint", "status" },
                    SuppressInitialValue = true
                });

            var observer = new HttpHandlerDiagnosticListenerObserver(
                config,
                counterMock.Object, summaryMock);

            var listener = new DiagnosticListener("TestListener");
            listener.Subscribe(observer);

            var request = new HttpRequestMessage
            {
                Method = new HttpMethod("GET"),
                RequestUri = new Uri("http://test.com/a/b/param1/c")
            };

            // Act
            var activity = new Activity("System.Net.Http.HttpRequestOut");
            listener.StartActivity(activity, new { Request = request });
            listener.StopActivity(activity, new { Request = request });

            // Assert            
            var counterMetrics = counterMock.Object.Collect();
            Assert.AreEqual("/a/b/@/c", counterMetrics.First().metric.First().label[2].value);
            Assert.IsTrue(counterMetrics.First().metric.Any());
        }
    }
}
