using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Bluekiri.Diagnostics.Prometheus
{
    internal class PathFilter : IComparable<PathFilter>
    {
        public string Path { get; private set; }

        private readonly Regex _pathRegex;

        public PathFilter(string path)
        {
            Path = path;
            _pathRegex = CreateRegexFromPathPattern(path);
        }

        public int CompareTo(PathFilter other)
        {
            if (other is null) return 1;

            return Path.Length.CompareTo(other.Path.Length);
        }

        internal bool Match(string path)
        {
            return _pathRegex.IsMatch(path);
        }

        private static Regex CreateRegexFromPathPattern(string path)
        {
            var regexExpression = new StringBuilder("^");
            foreach (var c in path)
            {
                switch(c)
                {
                    case '/':
                        regexExpression.Append(@"\").Append(c);
                        break;
                    case '@':
                        regexExpression.Append(@"[A-Za-z0-9]+");
                        break;
                    default:
                        regexExpression.Append(c);
                        break;
                }
            }
            regexExpression.Append("$");

            return new Regex(regexExpression.ToString(), 
                RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }
    }
}