namespace Bluekiri.Diagnostics.Prometheus
{
    public static class SubscribeOptionsExtensions
    {
        /// <summary>
        /// Adds the default observers
        /// </summary>
        /// <param name="o"></param>
        public static void AddDefaultObservers(this SubscribeOptions o)
        {
            o.AddSubscriber("HttpHandlerDiagnosticListener", new HttpHandlerDiagnosticListenerObserver());
            o.AddSubscriber("Microsoft.AspNetCore", new AspNetCoreDiagnosticListenerObserver());
        }
    }
}
