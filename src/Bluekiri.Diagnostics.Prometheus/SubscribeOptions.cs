using System;
using System.Collections.Generic;

namespace Bluekiri.Diagnostics.Prometheus
{
    public class SubscribeOptions
    {         
        internal IDictionary<string, IObserver<KeyValuePair<string, object>>> Observers { get; set; }

        public SubscribeOptions()
        {
            Observers = new Dictionary<string, IObserver<KeyValuePair<string, object>>>();
        }

        public void AddSubscriber(string listenerName, IObserver<KeyValuePair<string, object>> listenerObserver)
        {
            Observers.Add(listenerName, listenerObserver);
        }

        public void Clear()
        {
            Observers.Clear();
        }
    }
}