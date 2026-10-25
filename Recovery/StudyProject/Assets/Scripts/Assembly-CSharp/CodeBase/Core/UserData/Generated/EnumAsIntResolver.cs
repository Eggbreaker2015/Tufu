using MessagePack;
using MessagePack.Formatters;

namespace Codebase.Core.UserData.Generated
{
    public class EnumAsIntResolver : IFormatterResolver
    {
        private static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T> Formatter = typeof(T).IsEnum ? new EnumAsIntFormatter<T>() : null;
        }
        public static readonly EnumAsIntResolver Instance = new EnumAsIntResolver();
        public IMessagePackFormatter<T> GetFormatter<T>() => FormatterCache<T>.Formatter;
    }
}
