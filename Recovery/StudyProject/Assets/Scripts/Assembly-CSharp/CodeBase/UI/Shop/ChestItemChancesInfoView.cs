using System.Runtime.CompilerServices;
using Codebase.Core.UI;
using TMPro;
using UniMob.Core;
using UniMob;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Shop
{
	public class ChestItemChancesInfoView : View
	{
		[SerializeField]
		private Image chestIcon;

		[SerializeField]
		private TMP_Text chestNameText;

		[SerializeField]
		private Transform raritiesContainer;

		[SerializeField]
		private ChestRarityChanceItemView rarityItemPrefab;

		private ViewContainer<ChestRarityChanceItemView, ChestRarityChanceData> raritiesViewContainer;

		[CompilerGenerated]
		private ChestChancesData __Data__BackingField;

		private ComputedAtom<ChestChancesData> __Data;

		private ChestChancesData Data
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

		public void Setup(ChestChancesData data)
		{
			Data = data;
			Initialize();
			raritiesViewContainer.ForceRebuild();
		}

		public override void Initialize()
		{
			base.Initialize();
			if (raritiesViewContainer != null) return;
			raritiesViewContainer = new ViewContainer<ChestRarityChanceItemView, ChestRarityChanceData>(raritiesContainer, rarityItemPrefab)
				.BindTo(Lifetime, () => Data.Rarities).Setup((view, data) => view.Setup(data));
		}

		protected override void Render()
		{
			chestIcon.sprite = Data.Icon;
			chestNameText.text = Data.Name;
		}
	}
}
