using Codebase.Player.Systems;
using Codebase.Player.DailyProcs;
using Codebase.Player.Commands;
using Codebase.Core;
using System;
using System.Collections.Generic;
using Codebase.Abilities.Core;
using Codebase.Unit;

namespace Codebase.Abilities.Effects
{
	[Serializable]
	public class AddStatEffect : AbilityEffectBase
	{
		public StatType StatType;

		public ModifierType ModifierType;

		public ModifierSource Source;

		public float Duration;

		public AbilityValueScaling ValueScaling;

		public StatType ScalingStat;

		public bool UseDailyProcLimit;

		public int DailyProcLimit = 1; // Native ctor 0x15ecd04.

		public override void Execute(AbilityContext context)
		{
			// Native ELF VA 0x15ec76c; keep OnEquip, temporary-command and permanent-reward branches distinct.
			float value = ValueScaler.GetScaledValue(context, ValueScaling, ScalingStat);
			if (context.Trigger == AbilityTrigger.OnEquip) {
			    Stat stat = FindStat(context.PlayerUnit.Stats, StatType);
			    if (stat == null) return;
			    if (!context.ItemUnequipped) stat.AddModifier(new StatModifier {StatType=StatType, ModifierType=ModifierType, Name=context.Def.Key, Source=Source, Value=value, Duration=Duration});
			    else { StatModifier modifier=FindModifier(stat.Modifiers,context.Def.Key,Source); if(modifier!=null) stat.RemoveModifier(modifier); }
			    return;
			}
			if (UseDailyProcLimit && !GameContext.Get<DailyProcsModel>().TryConsumeProc(context.Def.Key,DailyProcLimit)) return;
			if (Duration > 0f) {
			    App.ExecuteCommand(new AddTemporaryModifierCommand {Unit=context.PlayerUnit, Name=context.Def.Key, StatType=StatType, ModifierType=ModifierType, Source=Source, Value=value, Duration=Duration, RemoveDuplicates=true});
			    return;
			}
			if(context.PlayerModel.DisablePermanentStatsGain) return;
			int multiplier=context.KillStatRewardMultiplier;
			GameContext.Get<PlayerRewardSystem>().GrantStatReward(new StatModifier {StatType=StatType, ModifierType=ModifierType, Name=context.Def.Key, Source=Source, Value=value*(float)multiplier}, -1.0, multiplier);
		}

		private static Stat FindStat(List<Stat> stats, StatType type)
		{
			// Native ELF VA 0x15ecb38.
			foreach (Stat stat in stats) if (stat.Type == type) return stat;
			return null;
		}

		private static StatModifier FindModifier(List<StatModifier> modifiers, string name, ModifierSource source)
		{
			// Native ELF VA 0x15ecc00.
			foreach (StatModifier modifier in modifiers) if (modifier.Name == name && modifier.Source == source) return modifier;
			return null;
		}
	}
}
