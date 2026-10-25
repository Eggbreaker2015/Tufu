using System.Linq;
using Codebase.Utilities;
using Codebase.VIP.Features;
using Codebase.VIP.Commands;
using UniMob;
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

		private readonly List<VipFeatureDescriptionView> spawnedFeatures = new List<VipFeatureDescriptionView>();

		private int lastRenderedPreviewLevel = -1;

		private int maxPreviewLevel;

		private ComputedAtom<int> __VipLevel;

		private ComputedAtom<int> __VipExperience;

		private ComputedAtom<TimeSpan> __ChestCooldown;

		private ComputedAtom<int> __PreviewLevel;

		private int VipLevel
        {
            get
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__VipLevel == null) __VipLevel = CodeGenAtom.CreatePooled(this, null, () => VipLevel, false);
                return __VipLevel.DirectEvaluate() ? vipModel.Level : __VipLevel.Value;
            }
        }

		private int VipExperience
        {
            get
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__VipExperience == null) __VipExperience = CodeGenAtom.CreatePooled(this, null, () => VipExperience, false);
                return __VipExperience.DirectEvaluate() ? vipModel.Experience : __VipExperience.Value;
            }
        }

		private TimeSpan ChestCooldown
        {
            get
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__ChestCooldown == null) __ChestCooldown = CodeGenAtom.CreatePooled(this, null, () => ChestCooldown, false);
                return __ChestCooldown.DirectEvaluate() ? vipModel.DailyChestCooldownRemaining : __ChestCooldown.Value;
            }
        }

		private int PreviewLevel
		{
            get
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__PreviewLevel == null) __PreviewLevel = CodeGenAtom.CreatePooled(this, null, () => PreviewLevel, false);
                return __PreviewLevel.DirectEvaluate() ? __PreviewLevel__BackingField : __PreviewLevel.Value;
            }
            set
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__PreviewLevel != null && !__PreviewLevel.CompareAndInvalidate(value)) return;
                __PreviewLevel__BackingField = value;
            }
		}

		protected override void Awake()
		{
            // Native 0x1605fa4.
            base.Awake();
            closeButton.onClick.AddListener(HandleClose);
            dailyChestButton.onClick.AddListener(HandleOpenDailyChest);
            decreasePreviewLevelButton.onClick.AddListener(DecreasePreviewLevel);
            increasePreviewLevelButton.onClick.AddListener(IncreasePreviewLevel);
		}

		private void HandleClose()
		{
            // Native 0x1606138.
            audioPlayer.PlaySoundOnce("tap");
            Close();
		}

		public override void Initialize()
		{
            // Native 0x1606204.
            base.Initialize();
            using (Atom.NoWatch)
            {
                maxPreviewLevel = vipConfig.GetMaxLevelInFeatures();
                PreviewLevel = Math.Min(maxPreviewLevel, VipLevel);
                PreviewLevel = Math.Max(PreviewLevel, 1);
            }
            lastRenderedPreviewLevel = -1;
            RebuildFeaturesIfNeeded();
		}

		public void SetPreviewLevel(int level)
		{
            // Native 0x1604e9c is B 0x1605f2c, not RET.
            PreviewLevel = level;
		}

		protected override void Render()
		{
            // Native 0x160697c; tail branch is RebuildFeaturesIfNeeded.
            RenderLevels();
            RenderProgress();
            RenderDailyChest();
            RenderPreviewControls();
            RebuildFeaturesIfNeeded();
		}

		private void RenderLevels()
		{
            // Native 0x16069ac.
            int level = VipLevel;
            currentLevelText.text = string.Format("VIP {0}", level);
            nextLevelText.text = string.Format("VIP {0}", Math.Min(vipConfig.MaxLevel, level + 1));
            int needed = vipConfig.GetRequiredExperience(PreviewLevel) - vipModel.Experience;
            string value = LocalizationStrings.Get("VIP_POINTS_NEEDED").Replace("{value}", needed.ToString());
            if (PreviewLevel <= VipLevel) value = LocalizationStrings.Get("VIP_CLAIMED");
            requiredVipPointsTxt.text = value;
		}

		private void RenderProgress()
		{
            // Native 0x1606bec intentionally displays total experience / next cumulative threshold.
            int level = VipLevel;
            int experience = VipExperience;
            int required;
            if (level < vipConfig.MaxLevel)
            {
                if (level > 0) vipConfig.GetRequiredExperience(level);
                required = vipConfig.GetRequiredExperience(level + 1);
                progressSlider.value = required > 0 ? Mathf.Clamp01((float)experience / required) : 0f;
            }
            else { progressSlider.value = 1f; required = experience; }
            progressText.text = string.Format("{0}<color=#FFCCAF><size=77%>/{1}</color></size>", experience, required);
		}

		private void RenderDailyChest()
		{
            // Native 0x1606da4.
            dailyChestLevelText.text = LocalizationStrings.Get("VIP_DAILY_CHEST_LEVEL").Replace("{value}", VipLevel.ToString());
            dailyChestCooldownLevelText.text = dailyChestLevelText.text;
            bool unlocked = vipModel.IsFeatureUnlocked(VipFeatureKeys.DailyChest);
            DailyChestConfig config = vipConfig.Features.OfType<DailyChestConfig>().First();
            DailyChestLevelConfig levelConfig = config.GetActiveLevelConfig(VipLevel);
            bool canClaim = unlocked && CanClaimDailyChestNow();
            bool cooldown = unlocked && levelConfig != null && !canClaim;
            dailyChestButton.interactable = canClaim;
            dailyChestLocked.SetActive(levelConfig == null);
            dailyChestAvailable.SetActive(canClaim);
            dailyChestCooldown.SetActive(cooldown);
            if (canClaim && levelConfig != null) dailyChestIcon.sprite = levelConfig.ChestIcon;
            if (cooldown)
            {
                dailyChestCooldownIcon.sprite = levelConfig.ChestIcon;
                dailyChestCooldownTimerText.text = "<sprite name=atlas_53> " + TimeLocalizer.GetFormatTime((float)ChestCooldown.TotalSeconds);
            }
		}

		private bool CanClaimDailyChestNow()
		{
            // Native 0x1607184.
            return ChestCooldown == TimeSpan.Zero;
		}

		private void RenderPreviewControls()
		{
            // Native 0x1607094.
            previewLevelText.text = string.Format("VIP {0}", PreviewLevel);
            decreasePreviewLevelButton.interactable = PreviewLevel > 1;
            increasePreviewLevelButton.interactable = PreviewLevel < maxPreviewLevel;
		}

		private void HandleOpenDailyChest()
		{
            // Native 0x16071f8: command also checks feature/cooldown/config eligibility.
            if (!CanClaimDailyChestNow()) return;
            audioPlayer.PlaySoundOnce("tap");
            App.ExecuteCommand(new TryClaimVipDailyChestCommand());
		}

		private void DecreasePreviewLevel()
		{
            // Native 0x1607310.
            audioPlayer.PlaySoundOnce("tap");
            using (Atom.NoWatch) PreviewLevel = Math.Max(1, PreviewLevel - 1);
		}

		private void IncreasePreviewLevel()
		{
            // Native 0x160750c.
            audioPlayer.PlaySoundOnce("tap");
            using (Atom.NoWatch) PreviewLevel = Math.Min(maxPreviewLevel, PreviewLevel + 1);
		}

		private void RebuildFeaturesIfNeeded()
		{
            // Native 0x16063d4, predicates 0x1607ab8/0x1607adc/0x1607b50.
            if (lastRenderedPreviewLevel == PreviewLevel) return;
            lastRenderedPreviewLevel = PreviewLevel;
            ClearFeatures();
            var descriptions = vipConfig.FeatureDescriptions.Where(description => description.UnlockLevel <= PreviewLevel)
                .OrderByDescending(GetChangeType).ThenByDescending(description => description.UnlockLevel);
            int index = 0;
            foreach (VipFeatureDescription description in descriptions)
            {
                VipFeatureDescriptionView view = Instantiate(featurePrefab, featuresContainer);
                view.Setup(description.GetLocalizationKey(PreviewLevel), GetChangeType(description), PreviewLevel, description.PreviewItemConfig);
                view.PlayRevealAnimation(index++);
                spawnedFeatures.Add(view);
            }
		}

		private VipFeatureChangeType GetChangeType(VipFeatureDescription description)
		{
            // Native 0x1607968.
            if (description.UnlockLevel == PreviewLevel) return VipFeatureChangeType.New;
            return description.UpgradeLevels != null && description.UpgradeLevels.Contains(PreviewLevel)
                ? VipFeatureChangeType.Up : VipFeatureChangeType.None;
		}

		private void ClearFeatures()
		{
            // Native 0x160770c uses Object.Destroy, not a ViewContainer pool.
            foreach (VipFeatureDescriptionView view in spawnedFeatures) Destroy(view.gameObject);
            spawnedFeatures.Clear();
		}

		public override void Cleanup()
		{
            // Native 0x1607a0c tail branches to ClearFeatures.
            base.Cleanup();
            ClearFeatures();
		}
	}
}
