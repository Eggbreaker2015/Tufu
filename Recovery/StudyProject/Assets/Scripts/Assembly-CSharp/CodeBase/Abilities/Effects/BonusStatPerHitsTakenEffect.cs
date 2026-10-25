using System;
using Codebase.Abilities.Core;
using Codebase.Unit;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class BonusStatPerHitsTakenEffect : AbilityEffectBase
	{
		public StatType StatType;

		public ModifierType ModifierType;

		public ModifierSource Source;

		private string cachedKey;

		public override void UpdatePassive(AbilityContext context, float deltaTime)
		{
            // Original ELF VA 0x15ee77c; nonpositive stack limits leave actual hits taken uncapped.
            int limit = context.State.GetStackLimit();
            int count = context.PlayerModel.HitsTakenCount;
            if (limit > 0) count = System.Math.Min(count, limit);
            float value = context.State.GetValue() * count;
            if (cachedKey == null) cachedKey = AddStatPassiveEffect.BuildKey(context.Def.Key, StatType, ModifierType, Source);
            AddStatPassiveEffect.PublishDesired(cachedKey, StatType, ModifierType, Source, value);
        }
	}
}
