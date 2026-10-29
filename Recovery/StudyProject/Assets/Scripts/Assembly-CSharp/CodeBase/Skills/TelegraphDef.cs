using System;

namespace Codebase.Skills
{
	[Serializable]
	public class TelegraphDef
	{
		public TelegraphShape Shape;

		public float Radius;

		public float Angle;

		// Centered on the picked target (locked at cast start) instead of the caster.
		public bool AnchorOnTarget;
	}
}
