using Codebase.Utilities;
using System.Linq;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Pet;
using Codebase.Player;
using Codebase.Player.Inventory;
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

		private float petRefreshTime;

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

		private void LateUpdate()
		{
			// Pet mode: the pets' attack uses player stats the render reaction does not track, so the DPS is polled.
			if (!PetConfig.Enabled || statType != StatType.Damage || !IsInitialized || Time.unscaledTime < petRefreshTime) return;
			petRefreshTime = Time.unscaledTime + global::Codebase.UI.PetStatsText.RefreshInterval;
			Render();
		}

		protected override void Render()
		{
			// Native ELF VA 0x16504d4 and original predicates 0x1650e84..0x1650f20.
			if (statType == StatType.Damage) {
			    double total = 0.0;
			    // Pet mode: each weapon counts as its pet, and the player's own claw is added with the original terms.
			    if (PetConfig.Enabled) total += RenderPetDps();
			    foreach (var item in playerModel.EquippedItems)
			    {
			        if (PetConfig.Enabled && PetStats.TryGet(item, out _)) continue;
			        total += playerModel.GetStatValue(item.Config.DamageStatTypePercent) *
			            (playerModel.GetStatValue(item.Config.DamageStatTypeFlat) + item.Damage + (double)item.AbilityDamageFlat + (double)playerModel.AbilityDamageFlat) *
			            (double)item.AbilityDamageMultiplier * (double)item.AbilityAttackSpeedMultiplier * (double)playerModel.AbilityDamageMultiplier / (double)item.Config.AttackCooldown;
			    }
			    valueField.text = total.Format(); return;
			}
			var stats = playerModel.Unit.Stats;
			double value = playerModel.Unit.GetStatValue(statType);
			double bonus = playerModel.EquippedWeapons.Where(item=>item.Config.DamageStatTypeFlat==statType).Sum(item=>WeaponBonus(item));
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

		// Pet mode: a weapon adds its pet's own attack to its damage type instead of the weapon damage.
		private static double WeaponBonus(ItemModel item)
		{
			return PetStats.TryGet(item, out PetStatsInfo pet) ? pet.OwnAttack : item.Damage;
		}

		private double RenderPetDps()
		{
			double total = 0.0;
			foreach (var item in playerModel.EquippedItems)
			    if (PetStats.TryGet(item, out PetStatsInfo pet)) total += global::Codebase.UI.PetStatsText.Dps(pet, playerModel);
			// The claw hits with the player's Damage stat (PetConfig.PlayerBaseAttack + kill-earned) as DamageCalculator's base attack.
			ItemModel claw = playerModel.PlayerAttackItem;
			if (claw != null)
			    total += playerModel.GetStatValue(claw.Config.DamageStatTypePercent) *
			        (playerModel.GetStatValue(StatType.Damage) + playerModel.GetStatValue(claw.Config.DamageStatTypeFlat) + claw.Damage + (double)claw.AbilityDamageFlat + (double)playerModel.AbilityDamageFlat) *
			        (double)claw.AbilityDamageMultiplier * (double)claw.AbilityAttackSpeedMultiplier * (double)playerModel.AbilityDamageMultiplier / (double)claw.Config.AttackCooldown;
			return total;
		}
	}
}
