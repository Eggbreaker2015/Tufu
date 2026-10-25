using System;
using MessagePack;
using MessagePack.Formatters;

namespace Codebase.Core.UserData.Generated
{
    public sealed class EnumAsIntFormatter<T> : IMessagePackFormatter<T>, IMessagePackFormatter
    {
        public void Serialize(ref MessagePackWriter writer, T value, MessagePackSerializerOptions options) => writer.Write(Convert.ToInt32(value));
        public T Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) => (T)(object)reader.ReadInt32();
    }
}
