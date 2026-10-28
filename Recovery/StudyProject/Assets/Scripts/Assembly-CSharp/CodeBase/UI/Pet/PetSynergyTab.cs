using System.Collections.Generic;
using System.Text;
using Codebase.Enemy;
using Codebase.Pet;
using Codebase.Player.Inventory;
using Codebase.Player.Models;
using Codebase.Utilities;
using TMPro;
using UnityEngine;

namespace Codebase.UI.Pets
{
	// 羁绊 tab: per damage type the equipped pets and the synergy tier table, the active tier highlighted.
	public class PetSynergyTab : MonoBehaviour
	{
		public static readonly WeaponType[] Types = { WeaponType.Stabbing, WeaponType.Cutting, WeaponType.Buckshot };

		[SerializeField]
		private TMP_Text hint;

		[SerializeField]
		private List<TMP_Text> blocks = new List<TMP_Text>();

		private readonly StringBuilder builder = new StringBuilder();

		private readonly List<string> names = new List<string>();

		internal void Setup(TMP_Text hintText, List<TMP_Text> blockTexts)
		{
			hint = hintText;
			blocks = blockTexts;
		}

		public void Render()
		{
			PetStatsText.SetText(hint, "同类型的宠物同时出战可激活羁绊，该类型的每只出战宠物都获得加成（只取达到的最高一档）。");
			PetConfig config = PetConfig.Instance;
			PlayerModel playerModel = PetUiBuilder.Find<PlayerModel>();
			for (int i = 0; i < Types.Length && i < blocks.Count; i++)
				PetStatsText.SetText(blocks[i], BuildBlock(Types[i], config, playerModel));
		}

		private string BuildBlock(WeaponType type, PetConfig config, PlayerModel playerModel)
		{
			names.Clear();
			if (config != null && playerModel != null && playerModel.ItemSlots != null)
				foreach (ItemSlotModel slot in playerModel.ItemSlots)
				{
					if (slot.SlotType != ItemSlotType.Weapon || !slot.IsEquipped || slot.ItemModel == null) continue;
					if (PetCollection.GetDamageType(slot.ItemModel, config) != type) continue;
					PetDefinition definition = config.Find(slot.ItemModel.Config);
					names.Add(definition != null && !string.IsNullOrEmpty(definition.DisplayName) ? definition.DisplayName : LocalizationStrings.Get("ITEM_NAME_" + slot.ItemModel.Config.Key));
				}
			int count = PetCollection.CountEquippedOfType(type);
			PetSynergyTier active = PetCollection.GetSynergyTier(count);
			builder.Clear();
			builder.Append("<size=125%><color=#FFD54A>").Append(PetStatsText.DamageTypeName(type)).Append("</color></size>  出战 ").Append(count).Append(" 只\n");
			builder.Append("<size=80%><color=#C8C8C8>").Append(names.Count > 0 ? string.Join("、", names) : "暂无出战宠物").Append("</color></size>");
			if (config != null && config.SynergyTiers != null)
				foreach (PetSynergyTier tier in config.SynergyTiers)
				{
					if (tier == null) continue;
					builder.Append('\n');
					bool isActive = tier == active;
					builder.Append(isActive ? "<color=#7CFF7C>" : "<color=#8A8A8A>").Append(tier.SameTypeCount).Append("只  ").Append(PetStatsText.FormatModifiers(tier.Modifiers));
					if (isActive) builder.Append("  (生效中)");
					builder.Append("</color>");
				}
			return builder.ToString();
		}
	}
}
