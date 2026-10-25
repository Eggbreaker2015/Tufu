using System.Runtime.CompilerServices;
using System.Globalization;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Player.Inventory;
using Codebase.Utilities;
using TMPro;
using UniMob.Core;
using UniMob;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Shop
{
	public class CrystalChestRarityItemView : View
	{
		[SerializeField]
		private Image rarityIcon;

		[SerializeField]
		private TMP_Text rarityNameText;

		[SerializeField]
		private TMP_Text rarityChanceText;

		[Inject]
		private ItemsProvider itemsProvider;

		[CompilerGenerated]
		private ChestRarityChanceData __Data__BackingField;

		private ComputedAtom<ChestRarityChanceData> __Data;

		private ChestRarityChanceData Data
		{
			[CompilerGenerated]
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__Data == null) __Data = CodeGenAtom.CreatePooled(this, null, () => Data, false);
				return __Data.DirectEvaluate() ? __Data__BackingField : __Data.Value;
			}
			[CompilerGenerated]
			set
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__Data != null && !__Data.CompareAndInvalidate(value)) return;
				__Data__BackingField = value;
			}
		}

		private string ChancePercent => (Data.Chance * 100f).ToString("0.##", CultureInfo.InvariantCulture);

		public void Setup(ChestRarityChanceData data)
		{
			Data = data;
			Initialize();
		}

		protected override void Render()
		{
			Color color = itemsProvider.GetRarityColor(Data.Rarity);
			rarityIcon.color = color;
			rarityNameText.color = color;
			rarityNameText.text = LocalizationStrings.Get(string.Format("ITEM_RARITY_{0}", Data.Rarity));
			rarityChanceText.text = ChancePercent + "%";
		}
	}
}
