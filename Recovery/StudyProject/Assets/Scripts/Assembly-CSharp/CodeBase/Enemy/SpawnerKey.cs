using System;

namespace Codebase.Enemy
{
    public static class SpawnerKey
    {
        private const int UdGuidSuffixLength = 6;
        public static string Build(string spawnerName) => spawnerName + "_" + Guid.NewGuid().ToString("N").Substring(0, UdGuidSuffixLength);
        public static string ToUdKey(string configKey)
        {
            var index = configKey.LastIndexOf('_');
            if (index >= 0)
            {
                var prefix = configKey.Substring(0, index + 1);
                var suffix = configKey.Substring(index + 1);
                if (suffix.Length > UdGuidSuffixLength) return prefix + suffix.Substring(0, UdGuidSuffixLength);
            }
            return configKey;
        }
    }
}
