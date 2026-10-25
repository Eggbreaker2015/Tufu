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
		}

		private void ShowReactAnimIfNeeded(FastList<RageLevelUpEvent> obj)
		{
		}

		public override void Cleanup()
		{
		}

		protected override void Render()
		{
		}

		private void LateUpdate()
		{
		}

		public void SetRagesMaxLevel(int level)
		{
		}
	}
}
