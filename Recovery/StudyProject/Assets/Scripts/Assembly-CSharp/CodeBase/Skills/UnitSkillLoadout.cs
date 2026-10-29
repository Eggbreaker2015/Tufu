using System;
using System.Collections.Generic;

namespace Codebase.Skills
{
	[Serializable]
	public class SkillSlotDef
	{
		public string SkillKey;

		public int Level = 1;
	}

	// Enemy: prefab name (pooled instance name without "(Clone)"); Boss: boss key (EnemySpawnerConfig.Key of boss fight units).
	[Serializable]
	public class UnitSkillLoadout
	{
		public SkillOwnerKind OwnerKind;

		public string Key;

		public List<SkillSlotDef> Skills = new List<SkillSlotDef>();
	}
}
