using System;
using System.Reflection;

namespace Bluekiri.Diagnostics.Prometheus
{
    /// <summary>
    /// This is an utility class to fetch properties from anonymous objects in an efficient way
    /// </summary>
    class PropertyFetcher
    {
        private readonly string _name;
        private PropertyInfo _fetchForExpectedType;
        private Type _expectedType;

        public PropertyFetcher(string name)
        {
            _name = name;
        }

        public object Fetch(object o)
        {
            var objType = o.GetType();
            if(objType != _expectedType)
            {
                _fetchForExpectedType = objType.GetProperty(_name);
                _expectedType = objType;
            }

            return _fetchForExpectedType?.GetValue(o);
        }
    }
}