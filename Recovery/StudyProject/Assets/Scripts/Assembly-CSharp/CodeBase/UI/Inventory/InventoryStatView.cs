using Codebase.Utilities;
using System.Linq;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Player;
using Codebase.Player.Models;
using Codebase.Unit;
using TMPro;
using UnityEngine;

namespace Codebase.UI.Inventory
{
	public class InventoryStatView : View
	{
		[SerializeField]
		private StatType statType;

		[SerializeField]
		private StatType secondStatType;

		[SerializeField]
		private bool useBothStats;

		[SerializeField]
		private bool isArmor;

		[SerializeField]
		private TMP_Text valueField;

		[SerializeField]
		private bool addSprite;

		[Inject]
		private IconStyleConfig iconStyleConfig;

		private PlayerModel playerModel;

		protected override void Awake()
		{
			// Native ELF VA 0x1650444.
			base.Awake(); playerModel = GameContext.Get<PlayerModel>();
		}

		private void OnEnable()
		{
			// Native ELF VA 0x16504c8.
			Initialize();
		}

		protected override void Render()
		{
			// Native ELF VA 0x16504d4 and original predicates 0x1650e84..0x1650f20.
			if (statType == StatType.Damage) {
			    double total = 0.0;
			    foreach (var item in playerModel.EquippedItems)
			        total += playerModel.GetStatValue(item.Config.DamageStatTypePercent) *
			            (playerModel.GetStatValue(item.Config.DamageStatTypeFlat) + item.Damage + (double)item.AbilityDamageFlat + (double)playerModel.AbilityDamageFlat) *
			            (double)item.AbilityDamageMultiplier * (double)item.AbilityAttackSpeedMultiplier * (double)playerModel.AbilityDamageMultiplier / (double)item.Config.AttackCooldown;
			    valueField.text = total.Format(); return;
			}
			var stats = playerModel.Unit.Stats;
			double value = playerModel.Unit.GetStatValue(statType);
			double bonus = playerModel.EquippedWeapons.Where(item=>item.Config.DamageStatTypeFlat==statType).Sum(item=>item.Damage);
			if (statType == StatType.HealthRegen) bonus += value * (double)(playerModel.AbilityHealthRegenMultiplier - 1f);
			if (isArmor) bonus = playerModel.EquippedArmors.Where(item=>item.Config.DamageStatTypeFlat==StatType.UniversalDefenceFlat||item.Config.DamageStatTypeFlat==statType).Sum(item=>item.Armor);
			string prefix = addSprite ? "<sprite name=" + iconStyleConfig.Styles.First(style=>style.StatType==statType).Sprite.name + ">" : string.Empty;
			valueField.text = prefix + value.Format();
			if (bonus > 0.0) valueField.text += (statType==StatType.HealthRegen?"<br>":"")+"<color=#42FF8E><size=70%> <voffset=6>+"+bonus.Format();
			if (useBothStats) {
			    double second = stats.First(stat=>stat.Type==secondStatType).Value;
			    if(second>1.0) valueField.text += " + " + ((second-1.0)*100.0).Format() + "%";
			}
		}
	}
}
