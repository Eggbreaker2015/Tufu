using System;
using Codebase.Abilities.Core;
using Codebase.Core;
using Codebase.Rage;
using Codebase.Player.Systems;
using Codebase.Abilities.Systems;
using Codebase.Abilities.Events;
using Codebase.Abilities.Vfx;
using CodeBase.Audio;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class AddRageEffect : AbilityEffectBase
	{
		public int Amount = 1;
		public override AbilityTrigger? IndependentTrigger
		{
			get
			{
				return AbilityTrigger.OnHit;
			}
		}

		public override void Execute(AbilityContext context)
		{
			RageModel rage = GameContext.Get<RageModel>();
			if (rage.MaxAccessibleLevel <= 0 || !rage.TryAddRage(Amount))
				return;
			GameContext.Get<RageExtraHealthSystem>().ForceApply();
			int level = rage.RageLevel;
			AbilityVfxData[] effects = AbilityVfxConstants.RageLevelUps;
			GameContext.Get<AbilityVfxService>().Spawn(effects[Mathf.Clamp(level - 1, 0, effects.Length - 1)], context.PlayerUnit);
			GameContext.Get<IAudioPlayer>().PlaySoundOnce("rage_level_up");
			GameContext.Get<AbilityExecuteEventSystem>().ProcessEventImmediate(new AbilityTriggerEvent { Trigger = AbilityTrigger.OnRageLevelUp });
			context.World.GetEvent<RageLevelUpEvent>().NextFrame(new RageLevelUpEvent { RageLevel = rage.RageLevel });
		}
	}
}
