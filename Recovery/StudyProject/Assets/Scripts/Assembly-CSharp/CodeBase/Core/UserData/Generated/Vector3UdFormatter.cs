using MessagePack;
using MessagePack.Formatters;

namespace Codebase.Core.UserData.Generated
{
    public class Vector3UdFormatter : IMessagePackFormatter<Vector3Ud>, IMessagePackFormatter
    {
        public void Serialize(ref MessagePackWriter writer, Vector3Ud value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(3);
            writer.Write(value.X);
            writer.Write(value.Y);
            writer.Write(value.Z);
        }
        public Vector3Ud Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil()) return default;
            var count = reader.ReadArrayHeader();
            Vector3Ud value = default;
            for (var i = 0; i < count; ++i)
            {
                switch (i)
                {
                    case 0: value.X = reader.ReadSingle(); break;
                    case 1: value.Y = reader.ReadSingle(); break;
                    case 2: value.Z = reader.ReadSingle(); break;
                    default: reader.Skip(); break;
                }
            }
            return value;
        }
    }
}
