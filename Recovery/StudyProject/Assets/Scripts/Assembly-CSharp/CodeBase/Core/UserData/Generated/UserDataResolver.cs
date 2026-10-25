using System;
using System.Collections.Generic;
using MessagePack;
using MessagePack.Formatters;

namespace Codebase.Core.UserData.Generated
{
    public class UserDataResolver : IFormatterResolver
    {
        private static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T> Formatter;
            static FormatterCache()
            {
                if (FormatterMap.TryGetValue(typeof(T), out var formatter))
                    Formatter = (IMessagePackFormatter<T>)formatter;
                else
                    Formatter = AotCollectionResolver.Instance.GetFormatter<T>() ?? EnumAsIntResolver.Instance.GetFormatter<T>();
            }
        }
        public static readonly UserDataResolver Instance = new UserDataResolver();
        private static readonly Dictionary<Type, object> FormatterMap = new Dictionary<Type, object>
        {
            { typeof(Vector3Ud), new Vector3UdFormatter() }
        };
        private UserDataResolver() { }
        public IMessagePackFormatter<T> GetFormatter<T>() => FormatterCache<T>.Formatter;
    }
}
