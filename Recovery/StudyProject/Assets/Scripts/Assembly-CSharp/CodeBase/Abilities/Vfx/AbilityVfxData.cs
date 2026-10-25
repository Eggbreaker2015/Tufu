namespace Codebase.Abilities.Vfx
{
	public readonly struct AbilityVfxData
	{
		public readonly string Key;

		public readonly float Duration;

		public readonly float HeightOffset;

		public readonly float RotationX;

		public readonly float RotationY;

		public readonly float RotationZ;

		public AbilityVfxData(string key, float duration, float heightOffset = 0f, float rotationX = -90f, float rotationY = 0f, float rotationZ = 0f)
		{
			// Native ELF VA 0x15e42a8.
			Key = key;
			Duration = duration;
			HeightOffset = heightOffset;
			RotationX = rotationX;
			RotationY = rotationY;
			RotationZ = rotationZ;
		}
	}
}
