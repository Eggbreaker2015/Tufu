using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Crystals;
using Codebase.Meta.Upgrades;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Crystals
{
	public class AmuletCrystalUpgradesView : PopupView
	{
		[SerializeField]
		private Transform itemsContainer;

		[SerializeField]
		private AmuletCrystalUpgradeItemView itemPrefab;

		[SerializeField]
		private Button closeButton;

		[Inject]
		private CrystalModel crystalModel;

		private ViewContainer<AmuletCrystalUpgradeItemView, UpgradeDef> upgradesContainer;

		protected override void Awake()
		{
			// Original ELF 0x13b2c80.
			base.Awake();
			closeButton.onClick.AddListener(Close);
		}

		public override void Initialize()
		{
			// Original ELF 0x13b2d1c; original binding/setup closures 0x13b34d8/0x13b356c.
			base.Initialize();
			if (upgradesContainer != null) return;
			upgradesContainer = new ViewContainer<AmuletCrystalUpgradeItemView, UpgradeDef>(itemsContainer, itemPrefab)
				.BindTo(Lifetime, () => crystalModel.Upgrades.Defs)
				.Setup((view, upgrade) => view.Setup(upgrade));
		}

		public void HighlightSlotUnlock(string slotKey)
		{
			// Original ELF 0x13aafcc.
			if (string.IsNullOrEmpty(slotKey) || upgradesContainer == null) return;
			string key = FindUpgradeKeyForSlot(slotKey);
			if (string.IsNullOrEmpty(key)) return;
			foreach (var view in upgradesContainer.Active)
				if (view.UpgradeKey == key) { view.PlayHighlight(); return; }
		}

		private string FindUpgradeKeyForSlot(string slotKey)
		{
			// Original ELF 0x13b2f2c: first matching feature modifier in authored definition order.
			foreach (var upgrade in crystalModel.Upgrades.Defs)
				foreach (var modifier in upgrade.Modifiers)
					if (modifier.StatKey == "feature_unlock" && modifier.StringParam == slotKey) return upgrade.Key;
			return null;
		}
	}
}
