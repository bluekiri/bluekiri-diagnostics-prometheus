using System;
using System.Collections.Generic;

namespace Bluekiri.Diagnostics.Prometheus
{
    public class HttpHandlerObserverConfig
    {
        private readonly List<PathFilter> _pathFilters;

        public HttpHandlerObserverConfig()
        {
            _pathFilters = new List<PathFilter>();
        }

        internal IReadOnlyCollection<PathFilter> PathFilters => _pathFilters.AsReadOnly();

        public void AddPathFilter(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            _pathFilters.Add(new PathFilter(path));
        }
    }
}