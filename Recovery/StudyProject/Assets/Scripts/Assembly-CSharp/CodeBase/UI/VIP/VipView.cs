using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CodeBase.Audio;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.VIP;
using TMPro;
using UniMob.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.VIP
{
	public class VipView : PopupView
	{
		[Header("Buttons")]
		[SerializeField]
		private Button closeButton;

		[SerializeField]
		private Button dailyChestButton;

		[SerializeField]
		private Button decreasePreviewLevelButton;

		[SerializeField]
		private Button increasePreviewLevelButton;

		[Header("Texts")]
		[SerializeField]
		private TMP_Text progressText;

		[SerializeField]
		private TMP_Text currentLevelText;

		[SerializeField]
		private TMP_Text nextLevelText;

		[SerializeField]
		private TMP_Text dailyChestLevelText;

		[SerializeField]
		private TMP_Text dailyChestCooldownLevelText;

		[SerializeField]
		private TMP_Text previewLevelText;

		[SerializeField]
		private TMP_Text requiredVipPointsTxt;

		[Header("Progress")]
		[SerializeField]
		private Slider progressSlider;

		[Header("Features")]
		[SerializeField]
		private Transform featuresContainer;

		[SerializeField]
		private VipFeatureDescriptionView featurePrefab;

		[Header("Daily Chest")]
		[SerializeField]
		private Image dailyChestIcon;

		[SerializeField]
		private GameObject dailyChestLocked;

		[SerializeField]
		private GameObject dailyChestAvailable;

		[SerializeField]
		private GameObject dailyChestCooldown;

		[SerializeField]
		private TMP_Text dailyChestCooldownTimerText;

		[SerializeField]
		private Image dailyChestCooldownIcon;

		[Inject]
		private VipModel vipModel;

		[Inject]
		private VipConfig vipConfig;

		[Inject]
		private IAudioPlayer audioPlayer;

		[CompilerGenerated]
		private int __PreviewLevel__BackingField;

		private readonly List<VipFeatureDescriptionView> spawnedFeatures;

		private int lastRenderedPreviewLevel;

		private int maxPreviewLevel;

		private ComputedAtom<int> __VipLevel;

		private ComputedAtom<int> __VipExperience;

		private ComputedAtom<TimeSpan> __ChestCooldown;

		private ComputedAtom<int> __PreviewLevel;

		private int VipLevel => 0;

		private int VipExperience => 0;

		private TimeSpan ChestCooldown => default;

		private int PreviewLevel
		{
			[CompilerGenerated]
			get
			{
				return 0;
			}
			[CompilerGenerated]
			set
			{
			}
		}

		protected override void Awake()
		{
		}

		private void HandleClose()
		{
		}

		public override void Initialize()
		{
		}

		public void SetPreviewLevel(int level)
		{
		}

		protected override void Render()
		{
		}

		private void RenderLevels()
		{
		}

		private void RenderProgress()
		{
		}

		private void RenderDailyChest()
		{
		}

		private bool CanClaimDailyChestNow()
		{
			return false;
		}

		private void RenderPreviewControls()
		{
		}

		private void HandleOpenDailyChest()
		{
		}

		private void DecreasePreviewLevel()
		{
		}

		private void IncreasePreviewLevel()
		{
		}

		private void RebuildFeaturesIfNeeded()
		{
		}

		private VipFeatureChangeType GetChangeType(VipFeatureDescription description)
		{
			return VipFeatureChangeType.None;
		}

		private void ClearFeatures()
		{
		}

		public override void Cleanup()
		{
		}
	}
}
