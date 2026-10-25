using System;
using System.Collections.Generic;
using MessagePack;
using MessagePack.Formatters;

namespace Codebase.Core.UserData.Generated
{
    public class AotCollectionResolver : IFormatterResolver
    {
        private static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T> Formatter = (IMessagePackFormatter<T>)GetFormatter(typeof(T));
        }
        public static readonly AotCollectionResolver Instance = new AotCollectionResolver();
        public IMessagePackFormatter<T> GetFormatter<T>() => FormatterCache<T>.Formatter;
        private static object GetFormatter(Type t)
        {
            if (t == typeof(Dictionary<string, int>)) return new DictionaryFormatter<string, int>();
            if (t == typeof(Dictionary<string, string>)) return new DictionaryFormatter<string, string>();
            if (t == typeof(Dictionary<string, bool>)) return new DictionaryFormatter<string, bool>();
            if (t == typeof(Dictionary<string, float>)) return new DictionaryFormatter<string, float>();
            if (t == typeof(Dictionary<string, double>)) return new DictionaryFormatter<string, double>();
            if (t == typeof(List<string>)) return new ListFormatter<string>();
            return null;
        }
    }
}
