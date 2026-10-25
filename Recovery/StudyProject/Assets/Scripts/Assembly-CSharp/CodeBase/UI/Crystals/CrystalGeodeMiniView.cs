using Codebase.Core;
using Codebase.Utilities;
using Codebase.Core.UI;
using Codebase.Crystals;
using Codebase.Player;
using TMPro;
using UnityEngine;

namespace Codebase.UI.Crystals
{
	public class CrystalGeodeMiniView : View
	{
		[SerializeField]
		private AmuletCrystalItemView itemView;

		[SerializeField]
		private TMP_Text baseStatsText;

		[SerializeField]
		private TMP_Text specialStatText;

		[SerializeField]
		private TMP_Text legendaryAbilityText;

		[SerializeField]
		private TMP_Text sellPriceText;

		[Inject]
		private CrystalModel crystalModel;

		[Inject]
		private IconStyleConfig iconStyleConfig;

		[Inject]
		private CrystalStyleConfig crystalStyleConfig;

		private CrystalItemModel model;

		public void Setup(CrystalItemModel crystal)
		{
            // Native ELF0x13b3f58.
            model = crystal; Injector.InjectInto(itemView); itemView.Setup(crystal); Initialize(); Render();
        }

		public override void Cleanup()
		{
            // Native ELF0x13b400c.
            base.Cleanup(); model = null;
        }

		protected override void Render()
		{
            // Native ELF0x13b4030.
            if (model == null) return;
            var parts = CrystalStatTextFormatter.BuildCrystalStatsParts(crystalModel, iconStyleConfig, model, false, true);
            if (model.EnchantLevel == 0) parts.Special = CrystalStatTextFormatter.BuildEnchantToUnlockText(crystalStyleConfig, model.Rarity);
            CrystalStatTextFormatter.ApplyStatsParts(parts, baseStatsText, specialStatText, legendaryAbilityText);
            sellPriceText.text = CrystalStatTextFormatter.FormatSellPrice(crystalModel.CalculateSellPrice(model));
        }
	}
}
