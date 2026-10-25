using System.Collections.Generic;
using Codebase.Core;
using Codebase.Player;
using Codebase.Player.Models;
using Codebase.Unit;
using Scellecs.Morpeh;

namespace Codebase.Amulet.Upgrades
{
	public class UpgradeStatApplySystem : SystemBase
	{
		[Inject]
		private AmuletUpgradesModel upgradesModel;

		[Inject]
		private PlayerModel playerModel;

		private readonly List<(Stat stat, StatModifier modifier)> appliedModifiers = new List<(Stat stat, StatModifier modifier)>();

		private int lastVersion = -1;

		private PlayerUnit lastUnit;

		public override void OnAwake()
		{
// Native ELF VA 0x15bb7d0 is a single RET.
		}

		public override void OnUpdate(float deltaTime)
		{
// Original ELF VA 0x15bb7d4; bounded ARM64 confirms unit-change HP reset only.
            PlayerUnit unit = playerModel.Unit;
            if (unit == null)
            {
                ClearApplied();
                lastUnit = null;
                return;
            }
            int version = upgradesModel.Version;
            bool unitChanged = unit != lastUnit;
            if (version == lastVersion && !unitChanged) return;
            lastVersion = version;
            lastUnit = unit;
            ApplyAll(unit);
            if (unitChanged) unit.CurrentHealth = unit.GetStatValue(StatType.Health,true);
		}

		private void ApplyAll(PlayerUnit unit)
		{
// Native ELF VA 0x15bbb28.
            ClearApplied();
            if (upgradesModel.Upgrades == null) return;
            foreach (StatModifier modifier in upgradesModel.Upgrades.CollectStatModifiers())
            {
                if (!unit.TryGetStat(modifier.StatType,out Stat stat)) continue;
                stat.AddModifier(modifier);
                appliedModifiers.Add((stat,modifier));
            }
		}

		private void ClearApplied()
		{
// Native ELF VA 0x15bb904.
            foreach (var applied in appliedModifiers) applied.stat.RemoveModifier(applied.modifier);
            appliedModifiers.Clear();
		}
	}
}
