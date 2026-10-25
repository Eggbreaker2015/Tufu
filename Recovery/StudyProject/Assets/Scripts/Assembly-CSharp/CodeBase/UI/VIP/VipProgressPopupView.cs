using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using CodeBase.Audio;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.VIP;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.VIP
{
	public class VipProgressPopupView : PopupView
	{


		[Header("UI Elements")]
		[SerializeField]
		private TMP_Text currentLevelText;

		[SerializeField]
		private TMP_Text nextLevelText;

		[SerializeField]
		private TMP_Text progressText;

		[SerializeField]
		private Slider progressSlider;

		[SerializeField]
		private Transform vipIcon;

		[Header("Animation Settings")]
		[SerializeField]
		private float experienceAnimationDuration = 1.5f;

		[SerializeField]
		private float delayBeforeClose = 0.5f;

		[SerializeField]
		private Ease animationEase = (Ease)9;

		[SerializeField]
		private float punchScale = 0.2f;

		[SerializeField]
		private int punchVibrato = 10;

		[SerializeField]
		private float punchElasticity = 1f;

		[Inject]
		private VipModel vipModel;

		[Inject]
		private VipConfig vipConfig;

		[Inject]
		private IAudioPlayer audioPlayer;

		[Inject]
		private UiModel uiModel;

		private int startExperience;

		private int targetExperience;

		private int startLevel;

		private int lastDisplayedLevel;

		private float currentSliderValue;

		private Coroutine animationCoroutine;

		protected override void Awake()
		{
            // Native 0x1604ecc tail calls PopupView.Awake.
            base.Awake();
		}

		public override void Initialize()
		{
            // Native 0x1604ed4; bounded tail calls UpdateDisplay.
            base.Initialize();
            startExperience = targetExperience = vipModel.Experience;
            startLevel = lastDisplayedLevel = vipModel.Level;
            int required = vipConfig.GetRequiredExperience(startLevel + 1);
            currentSliderValue = required > 0 ? Mathf.Clamp01((float)startExperience / required) : 0f;
            UpdateDisplay(startExperience, startLevel, currentSliderValue);
		}

		public void StartAnimation(int previousExperience)
		{
            // Native 0x16051bc.
            startExperience = previousExperience;
            targetExperience = vipModel.Experience;
            startLevel = lastDisplayedLevel = CalculateLevelForExperience(previousExperience);
            int required = vipConfig.GetRequiredExperience(startLevel + 1);
            currentSliderValue = required > 0 ? Mathf.Clamp01((float)previousExperience / required) : 0f;
            uiModel.IsVipProgressCurrentlyShowing = true;
            if (animationCoroutine != null) StopCoroutine(animationCoroutine);
            animationCoroutine = StartCoroutine(AnimateExperienceGain());
		}


		private IEnumerator AnimateExperienceGain()
		{
            // Native iterator MoveNext 0x16056b8, bounded0x38c. Original yield-frame, then close-delay stages.
            float elapsed = 0f;
            while (elapsed < experienceAnimationDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / experienceAnimationDuration);
                float eased = DOVirtual.EasedValue(0f, 1f, progress, animationEase);
                int experience = Mathf.RoundToInt(Mathf.Lerp(startExperience, targetExperience, eased));
                int level = CalculateLevelForExperience(experience);
                if (level > lastDisplayedLevel)
                {
                    lastDisplayedLevel = level;
                    PlayLevelUpAnimation();
                }
                int required = vipConfig.GetRequiredExperience(level + 1);
                float value = required > 0 ? Mathf.Clamp01((float)experience / required) : 0f;
                currentSliderValue = Mathf.Lerp(currentSliderValue, value, Time.deltaTime * 10f);
                UpdateDisplay(experience, level, currentSliderValue);
                yield return null;
            }
            int finalLevel = vipModel.Level;
            int finalRequired = vipConfig.GetRequiredExperience(finalLevel + 1);
            currentSliderValue = finalRequired > 0 ? Mathf.Clamp01((float)targetExperience / finalRequired) : 0f;
            UpdateDisplay(targetExperience, finalLevel, currentSliderValue);
            yield return new WaitForSeconds(delayBeforeClose);
            uiModel.IsVipProgressCurrentlyShowing = false;
            Close();
		}

		private void UpdateDisplay(int experience, int level, float sliderValue)
		{
            // Native 0x1604f78 uses cumulative experience and threshold, including max-level experience/experience.
            currentLevelText.text = string.Format("VIP {0}", level);
            nextLevelText.text = string.Format("VIP {0}", Math.Min(vipConfig.MaxLevel, level + 1));
            int required;
            if (level < vipConfig.MaxLevel)
            {
                required = vipConfig.GetRequiredExperience(level + 1);
                progressSlider.value = sliderValue;
            }
            else { progressSlider.value = 1f; required = experience; }
            progressText.text = string.Format("{0}<color=#FFCCAF><size=77%>/{1}</color></size>", experience, required);
		}

		private void PlayLevelUpAnimation()
		{
            // Native 0x16053b4. Bounded tail is the same DOPunchScale call, not decompiler-inlined library logic.
            audioPlayer.PlaySoundOnce("vip_or_graph_level_up");
            if (currentLevelText != null)
            {
                currentLevelText.transform.DOKill();
                currentLevelText.transform.DOPunchScale(Vector3.one * punchScale, 0.5f, punchVibrato, punchElasticity);
            }
            if (vipIcon != null)
            {
                vipIcon.DOKill();
                vipIcon.DOPunchScale(Vector3.one * punchScale, 0.5f, punchVibrato, punchElasticity);
            }
		}

		private int CalculateLevelForExperience(int experience)
		{
            // Native 0x16052a0 matches the original cumulative VIP thresholds.
            int level = 0;
            for (int next = 1; next <= vipConfig.MaxLevel; next++)
            {
                if (experience < vipConfig.GetRequiredExperience(next)) break;
                level = next;
            }
            return level;
		}

		protected override void Render()
		{
            // Native 0x16055f4 is actual RET; animation owns text updates.
		}

		public override void Cleanup()
		{
            // Native 0x16055f8.
            base.Cleanup();
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
                animationCoroutine = null;
            }
            DOTween.Kill(this);
		}
	}
}
