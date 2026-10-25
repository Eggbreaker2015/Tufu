using System;

namespace Codebase.ABTests
{
	[Serializable]
	public class SpawnerCooldownsData
	{
		public SpawnerCooldownsGroup nonElite = new SpawnerCooldownsGroup { cooldownMinutes = new ValueByLocation { defaultValue = 30f, overridesByLocation = new LocationOverrides { { 1, 4f }, { 2, 6f }, { 3, 8f }, { 4, 10f }, { 5, 12f }, { 6, 14f }, { 7, 16f }, { 8, 18f }, { 9, 20f }, { 10, 22f }, { 11, 24f }, { 12, 26f }, { 13, 28f } } }, charges = new IntValueByLocation { defaultValue = 3 } };

		public SpawnerCooldownsGroup elite = new SpawnerCooldownsGroup { cooldownMinutes = new ValueByLocation { defaultValue = 180f, overridesByLocation = new LocationOverrides { { 1, 60f }, { 2, 90f }, { 3, 120f }, { 4, 150f } } }, charges = new IntValueByLocation { defaultValue = 2 } };
	}
}
