using System;
using System.Collections.Generic;
using Codebase.Abilities.Core;
using Codebase.Unit;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class GrantRandomStatPerDistanceEffect : AbilityEffectBase
	{
		public float Distance = 400f; // Native ctor 0x15f0490.

		public int DailyLimit = 100;

		public ModifierType ModifierType;

		public ModifierSource Source = ModifierSource.AbilityPermanent;

		public List<StatType> StatPool = new List<StatType>();

		public override bool HasPassiveTick => true;

		public override void UpdatePassive(AbilityContext context, float deltaTime)
		{
            // Original ELF VA 0x15f019c; subtract each threshold before daily-limit consumption, discard remainder on denial.
            var distance = Codebase.Core.GameContext.Get<Codebase.Player.Distance.DistanceTraveledModel>();
            var daily = Codebase.Core.GameContext.Get<Codebase.Player.DailyProcs.DailyProcsModel>();
            if (distance.LastFrameDelta <= 0f || Distance <= 0f || StatPool.Count == 0) return;
            context.State.MovedDistance = distance.LastFrameDelta + context.State.MovedDistance;
            while (context.State.MovedDistance >= Distance)
            {
                context.State.MovedDistance -= Distance;
                if (!daily.TryConsumeProc(context.Def.Key, DailyLimit)) { context.State.MovedDistance = 0f; return; }
                GrantRandomStat(context);
            }
        }

		private void GrantRandomStat(AbilityContext context)
		{
            // Original ELF VA 0x15f02f4. The already-consumed daily proc is not refunded when permanent gains are disabled.
            if (context.PlayerModel.DisablePermanentStatsGain) return;
            StatType stat = StatPool[UnityEngine.Random.Range(0, StatPool.Count)];
            var modifier = new StatModifier { StatType = stat, ModifierType = ModifierType, Name = context.Def.Key, Value = context.State.GetValue(), Source = Source };
            Codebase.Core.GameContext.Get<Codebase.Player.Systems.PlayerRewardSystem>().GrantStatReward(modifier, -1.0, 1);
        }
	}
}
