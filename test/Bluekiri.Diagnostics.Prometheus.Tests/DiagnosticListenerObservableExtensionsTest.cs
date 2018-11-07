using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Bluekiri.Diagnostics.Prometheus.Tests
{
    [TestClass]
    public class DiagnosticListenerObservableExtensionsTest
    {
        class FakeDiagnosticListenerObservable : IObservable<DiagnosticListener>
        {
            private readonly List<IObserver<DiagnosticListener>> _observers;
            private readonly List<DiagnosticListener> _listeners;

            public FakeDiagnosticListenerObservable()
            {
                _observers = new List<IObserver<DiagnosticListener>>();
                _listeners = new List<DiagnosticListener>();
            }

            private class Unsubscriber : IDisposable
            {
                private List<IObserver<DiagnosticListener>> _observers;
                private IObserver<DiagnosticListener> _observer;

                public Unsubscriber(List<IObserver<DiagnosticListener>> observers, IObserver<DiagnosticListener> observer)
                {
                    _observers = observers;
                    _observer = observer;
                }
                public void Dispose()
                {
                    if (!(_observer is null)) _observers.Remove(_observer);
                }
            }

            public IDisposable Subscribe(IObserver<DiagnosticListener> observer)
            {
                if (!_observers.Contains(observer))
                    _observers.Add(observer);

                return new Unsubscriber(_observers, observer);
            }

            public void AddListener(DiagnosticListener listener)
            {
                _listeners.Add(listener);
            }

            public void CommitData()
            {
                foreach (var listener in _listeners)
                {
                    foreach (var observer in _observers)
                    {
                        observer.OnNext(listener);
                    }
                }
            }
        }

        class TestObserverForTestDiagnosticListener : IObserver<KeyValuePair<string, object>>
        {
            public void OnCompleted()
            {                
            }

            public void OnError(Exception error)
            {                
            }

            public void OnNext(KeyValuePair<string, object> value)
            {                
            }
        }

        [TestMethod]
        public void SubscribeDiagnosticListener_DefaultListenersSubscribed()
        {
            // Arrange
            var diagnosticListenerObservable = new FakeDiagnosticListenerObservable();
            var httpClientDiagnosticListener = new DiagnosticListener("HttpHandlerDiagnosticListener");
            var aspNetCoreDiagnosticListener = new DiagnosticListener("Microsoft.AspNetCore");

            diagnosticListenerObservable.AddListener(httpClientDiagnosticListener);
            diagnosticListenerObservable.AddListener(aspNetCoreDiagnosticListener);
            diagnosticListenerObservable.SubscribeDiagnosticListener();

            // Act

            // This notifies the diagnostic listeners observers about the different diagnostic listeners registered.
            // We register one observer using SubscribeDiagnosticListener. 
            // At the same time, when notified, this observer registers the adequate
            // observers into each one of the Diagnostic Listeners.
            diagnosticListenerObservable.CommitData();

            // Assert     
            var subscriptionsField = typeof(DiagnosticListener).GetField("_subscriptions",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var httpClientDiagnosticListenerSubscriptions = subscriptionsField.GetValue(httpClientDiagnosticListener);

            var httpClientDiagnosticListenerObserver = httpClientDiagnosticListenerSubscriptions.GetType()
                .GetField("Observer",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .GetValue(httpClientDiagnosticListenerSubscriptions);

            var aspNetCoreDiagnosticListenerSubscriptions = subscriptionsField.GetValue(aspNetCoreDiagnosticListener);
            var aspNetCoreDiagnosticListenerObserver = aspNetCoreDiagnosticListenerSubscriptions.GetType()
                .GetField("Observer",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .GetValue(aspNetCoreDiagnosticListenerSubscriptions);

            Assert.IsInstanceOfType(httpClientDiagnosticListenerObserver, typeof(HttpHandlerDiagnosticListenerObserver));
            Assert.IsInstanceOfType(aspNetCoreDiagnosticListenerObserver, typeof(AspNetCoreDiagnosticListenerObserver));
        }

        [TestMethod]
        public void SubscribeDiagnosticListener_CustomDiagnosticListenerObserverSubscribed()
        {
            // Arrange
            var diagnosticListenerObservable = new FakeDiagnosticListenerObservable();
            var testDiagnosticListener = new DiagnosticListener("TestDiagnosticListener");

            diagnosticListenerObservable.AddListener(testDiagnosticListener);
            diagnosticListenerObservable.SubscribeDiagnosticListener(o =>
            {
                o.AddSubscriber("TestDiagnosticListener", new TestObserverForTestDiagnosticListener());
            });

            // Act
            diagnosticListenerObservable.CommitData();

            // Assert
            var subscriptionsField = typeof(DiagnosticListener).GetField("_subscriptions",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var testDiagnosticListenerSubscriptions = subscriptionsField.GetValue(testDiagnosticListener);

            var testDiagnosticListenerObserver = testDiagnosticListenerSubscriptions.GetType()
                .GetField("Observer",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .GetValue(testDiagnosticListenerSubscriptions);

            Assert.IsInstanceOfType(testDiagnosticListenerObserver, typeof(TestObserverForTestDiagnosticListener));
        }
    }
}
