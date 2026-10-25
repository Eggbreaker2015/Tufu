using Codebase.Core;
using UniMob;

namespace Codebase.Player.Distance
{
	public class DistanceTraveledModel : Model
	{
		public float LastFrameDelta { get; private set; }

		public DistanceTraveledModel(Lifetime lifetime)
			: base(lifetime)
		{
		}

		public void SetLastFrameDelta(float delta)
		{
            // Original ELF VA 0x1467b2c; preserve the raw producer value without clamping.
            LastFrameDelta = delta;
        }
	}
}
