using System.Threading;
using UniMob;

namespace Codebase.Utilities
{
    public static class LifetimeExtensions
    {
        public static CancellationToken ToCancellationToken(this Lifetime lifetime)
        {
            var source = new CancellationTokenSource();
            lifetime.Register(source);
            lifetime.Register(() => source.Cancel());
            return source.Token;
        }
    }
}
