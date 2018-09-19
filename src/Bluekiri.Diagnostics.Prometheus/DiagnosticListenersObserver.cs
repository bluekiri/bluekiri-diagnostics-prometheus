using System;
using System.Diagnostics;
using System.Threading;

namespace Bluekiri.Diagnostics.Prometheus
{
    public class DiagnosticListenersObserver : IObserver<DiagnosticListener>
    {
        private IDisposable _subscription;
        private static Lazy<DiagnosticListenersObserver> _instance 
            = new Lazy<DiagnosticListenersObserver>(
                () => new DiagnosticListenersObserver(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static DiagnosticListenersObserver Instance => _instance.Value;

        private DiagnosticListenersObserver(){}

        public void OnCompleted()
        {
            _subscription?.Dispose();            
        }

        public void OnError(Exception error)
        {            
        }

        public void OnNext(DiagnosticListener value)
        {
            if(value.Name == "HttpHandlerDiagnosticListener")
            {              
                _subscription = value.Subscribe(new HttpHandlerDiagnosticListenerObserver());
            } 
        }
    }
}
