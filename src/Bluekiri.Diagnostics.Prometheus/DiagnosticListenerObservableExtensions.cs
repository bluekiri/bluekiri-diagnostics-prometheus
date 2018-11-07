using System;
using System.Diagnostics;

namespace Bluekiri.Diagnostics.Prometheus
{
    public static class DiagnosticListenerObservableExtensions
    {
        /// <summary>
        /// Subscribes the default observers for HttHandlerDiagnosticListener and Microsoft.AspNetCore
        /// </summary>
        /// <param name="observable"></param>
        public static void SubscribeDiagnosticListener(this IObservable<DiagnosticListener> observable)
        {
            observable.SubscribeDiagnosticListener(null);
        }

        /// <summary>
        /// Subscribes a custom set of observers to specific Diagnostic Listeners
        /// </summary>
        /// <param name="observable"></param>
        /// <param name="configure">The action that adds the custom observers</param>
        public static void SubscribeDiagnosticListener(this IObservable<DiagnosticListener> observable, Action<SubscribeOptions> configure)
        {
            var options = new SubscribeOptions();
            options.AddDefaultObservers();

            if(!(configure is null))
            {
                configure(options);
            }            

            observable.Subscribe(delegate (DiagnosticListener listener)
            {
                if (options.Observers.ContainsKey(listener.Name))
                {
                    listener.Subscribe(options.Observers[listener.Name]);
                }
            });
        }
    }
}