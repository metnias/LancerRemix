using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LancerRemix
{
    internal class JsonBuilder
    {
        private readonly List<object> _items = new List<object>();

        public JsonBuilder Value(string name, object value)
        {
            _items.Add(new KeyValuePair<string, object>(name, value));
            return this;
        }

        public JsonBuilder Object(string name, Action<JsonBuilder> action = null)
        {
            var builder = new JsonBuilder();
            action?.Invoke(builder);
            _items.Add(new KeyValuePair<string, object>(name, builder._items));
            return this;
        }

        public JsonBuilder Array(string name, Action<JsonBuilder> action = null)
        {
            var builder = new JsonBuilder();
            action?.Invoke(builder);
            _items.Add(new KeyValuePair<string, object>(name, builder._items));
            return this;
        }

        private string EscapeString(string input)
        {
            return new StringBuilder(input)
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\b", "\\b")
                .Replace("\f", "\\f")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t")
                .ToString();
        }

        public string Build()
        {
            var jsonItems = _items.Select(item =>
            {
                if (item is KeyValuePair<string, object> kvp)
                {
                    var key = EscapeString(kvp.Key);
                    var value = kvp.Value is JsonBuilder builder ? builder.Build() : kvp.Value;
                    var jsonValue = value == null
                        ? "null"
                        : value is string s
                            ? $"\"{EscapeString(s)}\""
                            : value.ToString();
                    return $"\"{key}\":{jsonValue}";
                }
                return null;
            });

            return $"{{{string.Join(",", jsonItems.Where(item => item != null))}}}";
        }
    }
}