using DG.Tweening;
using Scellecs.Morpeh;
using System;
using BrunoMikoski.AnimationSequencer;
using Codebase.BossTower;
using Codebase.BossTower.Systems;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Rage;
using Scellecs.Morpeh.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Rage
{
	public class RageBarView : View
	{
		private const float BlinkIdleThreshold = 6.5f;

		private const float BlinkSpeed = 5.714286f;

		private const float BlinkMinAlpha = 0.2f;

		[SerializeField]
		private GameObject root;

		[SerializeField]
		private Image slider;

		[SerializeField]
		private GameObject[] levelIcons;

		[SerializeField]
		private GameObject[] maxLevelParticles;

		[SerializeField]
		private CanvasGroup levelIconCanvasGroup;

		[SerializeField]
		private GameObject bigSizeDivider;

		[SerializeField]
		private GameObject smallSizeDivider;

		[SerializeField]
		private RectTransform viewBarRect;

		[SerializeField]
		private AnimationSequencerController reactAnimation;

		[Inject]
		private RageModel rageModel;

		[Inject]
		private BossTowerModel bossTowerModel;

		[Inject]
		private BossRaidSystem bossRaidSystem;

		private int previousMaxRageLevel;

		private IDisposable subscription;

		protected override void Start()
		{
            // Native 0x16339f8.
            base.Start();
            Initialize();
            subscription = Scellecs.Morpeh.World.Default.GetEvent<RageLevelUpEvent>().Subscribe(ShowReactAnimIfNeeded);
		}

		private void ShowReactAnimIfNeeded(FastList<RageLevelUpEvent> obj)
		{
            // Native 0x1633b48 reacts only to a Vikings fight's nonempty rage-level batch.
            if (obj.length == 0 || !bossTowerModel.IsFighting) return;
            var boss = bossRaidSystem.GetCurrentBossModel();
            if (boss == null || boss.Def.Key != "vikings") return;
            reactAnimation.Play();
		}

		public override void Cleanup()
		{
            // Native 0x1633c00.
            subscription?.Dispose();
            base.Cleanup();
		}

		protected override void Render()
		{
            // Native 0x1633ca8.
            int maxLevel = rageModel.EffectiveMaxLevel;
            bool visible = rageModel.RageLevel > 0 || rageModel.MaxAccessibleLevel > 0;
            if (rageModel.EffectiveMaxLevel != previousMaxRageLevel)
            {
                previousMaxRageLevel = rageModel.EffectiveMaxLevel;
                SetRagesMaxLevel(rageModel.EffectiveMaxLevel);
            }
            root.SetActive(visible);
            if (!visible) return;
            int segments = rageModel.EffectiveMaxLevel > 3 ? 4 : 3;
            int level = Mathf.Clamp(rageModel.RageLevel, 0, maxLevel);
            float progress = Mathf.Clamp01(rageModel.RagePoints / rageModel.GetThreshold(level));
            slider.DOFillAmount((float)level / segments + progress / segments, 0.2f);
            for (int index = 0; index < levelIcons.Length; index++) levelIcons[index].SetActive(index == level);
            for (int index = 0; index < maxLevelParticles.Length; index++) maxLevelParticles[index].SetActive(level >= segments);
		}

		private void LateUpdate()
		{
            // Native 0x1633f74 uses Time.unscaledTime and sin; it does not alter rage points.
            if (rageModel.RageLevel > 0 && rageModel.TimeSinceLastRage >= BlinkIdleThreshold)
                levelIconCanvasGroup.alpha = Mathf.Lerp(BlinkMinAlpha, 1f, (Mathf.Sin(Time.unscaledTime * BlinkSpeed) + 1f) * 0.5f);
            else levelIconCanvasGroup.alpha = 1f;
		}

		public void SetRagesMaxLevel(int level)
		{
            // Native 0x1633ed8 modifies sizeDelta.y (S1), retaining width (S0).
            Vector2 size = viewBarRect.sizeDelta;
            viewBarRect.sizeDelta = new Vector2(size.x, level > 3 ? 378f : 283f);
            bigSizeDivider.gameObject.SetActive(level > 3);
            smallSizeDivider.gameObject.SetActive(level < 4);
		}
	}
}
