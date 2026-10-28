using Codebase.BossTower.Components;
using CodeBase.Audio;
using Codebase.Meta;
using Codebase.Core;
using Codebase.Abilities.Vfx;
using Codebase.Abilities.Systems;
using Codebase.Abilities.Events;
using UnityEngine;
using System;
using Codebase.Abilities.Core;
using Codebase.Enemy;
using Codebase.Player.Models;
using Codebase.Unit.Components;
using Scellecs.Morpeh;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class ApplyMarkEffect : AbilityEffectBase
	{
		public const float PiercingWeaponHitChance = 0.1f;

		public bool AppliesToAllPiercingWeapons;

		public override void Execute(AbilityContext context)
		{
            // Native 0x15ed9b8: the all-piercing mode is consumed by PiercingWeaponMarkSystem.
            if (AppliesToAllPiercingWeapons) return;
            ApplyMarkInternal(context.World.GetStash<MarkComponent>(), context.World, context.Target, context.PlayerModel, 1, false, true);
        }

		public static void ApplyMark(Stash<MarkComponent> markStash, World world, EnemyUnit target, PlayerModel playerModel, int stacksToAdd = 1, bool allowToStayAboveMax = false)
		{
            // Native 0x15ec3d4: two-instruction tail wrapper sets rollExtraMarkChance=true.
            ApplyMarkInternal(markStash, world, target, playerModel, stacksToAdd, allowToStayAboveMax, true);
        }

		private static void ApplyMarkInternal(Stash<MarkComponent> markStash, World world, EnemyUnit target, PlayerModel playerModel, int stacksToAdd, bool allowToStayAboveMax, bool rollExtraMarkChance)
		{
            // Native 0x15eda38. Existing over-cap marks retain their old stack count unless the next total falls within max.
            if (target == null || stacksToAdd <= 0) return;
            Entity entity = target.EntityProvider.Entity;
            bool allowAbove = world.GetStash<HydraBossComponent>().Has(entity) || allowToStayAboveMax;
            MarkComponent mark = markStash.Get(entity, out bool exists);
            int previous = exists ? mark.Stacks : 0;
            int max = CalculateMaxMarks(world, playerModel, target);
            GameContext.Get<AbilityVfxService>().Spawn(AbilityVfxConstants.ApplyMark, target.VisualRoot != null ? target.VisualRoot : target.transform);
            GameContext.Get<IAudioPlayer>().PlaySoundOnce("mark_apply");
            if (!exists) mark = new MarkComponent {Stacks = allowAbove ? stacksToAdd : Math.Min(max, stacksToAdd), MaxStacks = max, DecayTimer = 12f, AllowToStayAboveMax = allowAbove};
            else {
                mark.MaxStacks = max;
                mark.DecayTimer = 12f;
                if (allowAbove) { mark.Stacks = unchecked(mark.Stacks + stacksToAdd); mark.AllowToStayAboveMax = true; }
                else if (!mark.AllowToStayAboveMax) mark.Stacks = Math.Min(max, unchecked(mark.Stacks + stacksToAdd));
                else if (unchecked(mark.Stacks + stacksToAdd) <= max) mark.Stacks = unchecked(mark.Stacks + stacksToAdd);
            }
            markStash.Set(entity, mark);
            ProcessMarkAppliedEvents(target, markStash.Get(entity).Stacks - previous);
            if (rollExtraMarkChance) {
                double chance = GameContext.Get<MetaStatsModel>().Get("extra_mark").Value;
                if (chance > 0.0 && UnityEngine.Random.value < chance) ApplyMarkInternal(markStash, world, target, playerModel, 1, allowAbove, false);
            }
        }

		private static void ProcessMarkAppliedEvents(EnemyUnit target, int appliedMarks)
		{
            // Native 0x15ee214: dispatch exactly once for each newly gained stack, not each attempted application.
            if (appliedMarks <= 0) return;
            AbilityExecuteEventSystem system = GameContext.Get<AbilityExecuteEventSystem>();
            for (int i = 0; i < appliedMarks; i++) system.ProcessEventImmediate(new AbilityTriggerEvent {Trigger = AbilityTrigger.OnMarkApplied, Target = target});
        }

		private static int CalculateMaxMarks(World world, PlayerModel playerModel, EnemyUnit target)
		{
            // Native 0x15edf3c: all equipped effects contribute, without a condition/trigger gate.
            int result = unchecked(1 + playerModel.BonusMaxMarks);
            foreach (var item in playerModel.EquippedItems)
                foreach (var state in item.AbilityStates)
                    foreach (var effect in state.Def.Effects)
                        if (effect is IncreaseMaxMarksEffect increase) {
                            AbilityContext context = AbilityContext.Rent();
                            context.World = world; context.Owner = item; context.State = state; context.PlayerModel = playerModel; context.Target = target;
                            context.Attacker = AbilityTargetResolver.GetOwnerPetUnit(item); // Pet mode: the owner's pet; null with pet mode off.
                            float scaled = ValueScaler.Resolve(increase.Scaling, context, increase.ScalingStat) * increase.GetCount(state);
                            // Native FCVTZS saturates except the compiler's explicit +infinity-to-MinValue case.
                            int count = float.IsNaN(scaled) ? 0 : float.IsPositiveInfinity(scaled) ? int.MinValue : scaled >= 2147483648f ? int.MaxValue : scaled <= -2147483648f ? int.MinValue : (int)scaled;
                            result = unchecked(result + count);
                            AbilityContext.Release(context);
                        }
            return result;
        }
	}
}
