using UniMob;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BrunoMikoski.AnimationSequencer;
using Codebase.Core.UI;
using Codebase.Rewards;
using Codebase.UI.Events;
using UniMob.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.BossTower
{
	public class BossRaidRewardView : PopupView
	{
		[SerializeField]
		private AnimationSequencerController animationController;

		[SerializeField]
		private Transform rewardsContainer;

		[SerializeField]
		private RewardView rewardPrefab;

		[SerializeField]
		private Button collectButton;

		[SerializeField]
		private GridLayoutGroup gridLayout;

		[CompilerGenerated]
		private List<RewardBase> __Rewards__BackingField;

		private ViewContainer<RewardView, RewardBase> rewardsViewContainer;

		private Action onCollect;

		private bool collected;

		private bool isAnimationFinished;

		private ScrollRect rewardsScroll;

		private ComputedAtom<List<RewardBase>> __Rewards;

		private List<RewardBase> Rewards
		{
			[CompilerGenerated]
			get
			{
			// Native generated atom getter ELF VA 0x13c106c.
			LifetimeScopeExtension.ThrowIfDisposed(this);
			if (__Rewards == null) __Rewards = CodeGenAtom.CreatePooled(this, null, () => Rewards, false);
			return __Rewards.DirectEvaluate() ? __Rewards__BackingField : __Rewards.Value;
		}
			[CompilerGenerated]
			set
			{
			// Native generated atom setter ELF VA 0x13c1194.
			LifetimeScopeExtension.ThrowIfDisposed(this);
			if (__Rewards != null && !__Rewards.CompareAndInvalidate(value)) return;
			__Rewards__BackingField = value;
		}
		}

		protected override void Awake()
		{
			// Native ELF VA 0x13c1214.
			base.Awake();
			animationController.OnFinishedEvent.AddListener(OnAnimationFinished);
			collectButton.onClick.AddListener(HandleClick);
		}

		private void HandleClick()
		{
			// Native ELF VA 0x13c1308.
			if (isAnimationFinished) Collect(); else Skip();
		}

		private void Skip()
		{
			// Native ELF VA 0x13c1378; Complete(true) invokes the original completion event.
			if (isAnimationFinished) return;
			isAnimationFinished = true;
			animationController.Complete(true);
		}

		protected override void Start()
		{
			// Native ELF VA 0x13c13b4; source 0x13c17b4.
			base.Start();
			rewardsViewContainer = new ViewContainer<RewardView, RewardBase>(rewardsContainer, rewardPrefab)
			    .BindTo(Lifetime, () => Rewards ?? new List<RewardBase>()).Setup(SetupItemView);
		}

		private static void SetupItemView(RewardView view, RewardBase reward)
		{
			// Native ELF VA 0x13c15b0.
			view.Setup(reward, RewardViewStyle.Simplified);
			view.SetInfoBtnVisible(false);
		}

		public void Setup(List<RewardBase> rewards, Action onCollect)
		{
			// Native ELF VA 0x13c15e8; virtual slots Complete/ResetToInitialState/Play.
			this.onCollect = onCollect;
			collected = false;
			collectButton.interactable = true;
			animationController.Complete(true);
			animationController.ClearPlayingSequence();
			animationController.ResetToInitialState();
			isAnimationFinished = false;
			gridLayout.childAlignment = TextAnchor.UpperLeft;
			Rewards = rewards;
			ResetRewardsScroll();
			animationController.Play();
		}

		private void ResetRewardsScroll()
		{
			// Native ELF VA 0x13c16b4.
			if (rewardsScroll == null) rewardsScroll = rewardsContainer.GetComponentInParent<ScrollRect>(true);
			if (rewardsScroll == null) return;
			rewardsScroll.StopMovement();
			rewardsScroll.normalizedPosition = new Vector2(0f, 1f);
		}

		private void OnAnimationFinished()
		{
			// Native ELF VA 0x13c17a0.
			isAnimationFinished = true;
		}

		private void Collect()
		{
			// Native ELF VA 0x13c1318; guard is set before callback, including reentrant clicks.
			if (collected) return;
			collected = true;
			collectButton.interactable = false;
			onCollect?.Invoke();
			Close();
		}
	}
}
