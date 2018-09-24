using System;
using System.Diagnostics;

namespace Bluekiri.Diagnostics.Prometheus
{
    public static class DiagnosticListenerObservableExtensions
    {

        public static void SubscribeDiagnosticListener(this IObservable<DiagnosticListener> observable)
        {
            observable.Subscribe(delegate (DiagnosticListener listener)
            {
                if (listener.Name == "HttpHandlerDiagnosticListener")
                {
                    listener.Subscribe(new HttpHandlerDiagnosticListenerObserver());
                }

                if (listener.Name == "Microsoft.AspNetCore")
                {
                    listener.Subscribe(new AspNetCoreDiagnosticListenerObserver());
                }
            });
        }
    }
}
