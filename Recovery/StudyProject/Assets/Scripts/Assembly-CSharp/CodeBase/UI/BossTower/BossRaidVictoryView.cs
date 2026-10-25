using Codebase.Rewards.Commands;
using Codebase.BossTower.Flows;
using Codebase.Localization;
using UniMob;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BrunoMikoski.AnimationSequencer;
using CodeBase.Audio;
using Codebase.BossTower;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Rewards;
using Codebase.Services.Cheats;
using Codebase.UI.Events;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using TMPro;
using UniMob.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.BossTower
{
	public class BossRaidVictoryView : PopupView
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CCloseWithDelay_003Ed__32 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder _003C_003Et__builder;

			public BossRaidVictoryView _003C_003E4__this;

			private UniTask.Awaiter _003C_003Eu__1;

			private void MoveNext()
			{
			}

			void IAsyncStateMachine.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				this.MoveNext();
			}

			[DebuggerHidden]
			private void SetStateMachine(IAsyncStateMachine stateMachine)
			{
			}

			void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
			{
				//ILSpy generated this explicit interface implementation from .override directive in SetStateMachine
				this.SetStateMachine(stateMachine);
			}
		}

		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CPlayAnimation_003Ed__27 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskVoidMethodBuilder _003C_003Et__builder;

			public BossRaidVictoryView _003C_003E4__this;

			private int _003Cfloor_003E5__2;

			private UniTask.Awaiter _003C_003Eu__1;

			private int _003Ci_003E5__3;

			private Cysharp.Threading.Tasks.YieldAwaitable.Awaiter _003C_003Eu__2;

			private void MoveNext()
			{
			}

			void IAsyncStateMachine.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				this.MoveNext();
			}

			[DebuggerHidden]
			private void SetStateMachine(IAsyncStateMachine stateMachine)
			{
			}

			void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
			{
				//ILSpy generated this explicit interface implementation from .override directive in SetStateMachine
				this.SetStateMachine(stateMachine);
			}
		}

		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CSetScrollValue_003Ed__23 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder _003C_003Et__builder;

			public BossRaidVictoryView _003C_003E4__this;

			private int _003Ci_003E5__2;

			private UniTask.Awaiter _003C_003Eu__1;

			private void MoveNext()
			{
			}

			void IAsyncStateMachine.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				this.MoveNext();
			}

			[DebuggerHidden]
			private void SetStateMachine(IAsyncStateMachine stateMachine)
			{
			}

			void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
			{
				//ILSpy generated this explicit interface implementation from .override directive in SetStateMachine
				this.SetStateMachine(stateMachine);
			}
		}

		[SerializeField]
		private TMP_Text bossNameText;

		[SerializeField]
		private HitCellsBar hitCellsBar;

		[SerializeField]
		private Transform rewardsContainer;

		[SerializeField]
		private RewardView rewardPrefab;

		[SerializeField]
		private Button hubButton;

		[SerializeField]
		private Button continueButton;

		[SerializeField]
		private AnimationSequencerController animController;

		[SerializeField]
		private TMP_Text floorTxt;

		[SerializeField]
		private ScrollRect scrollRect;

		[CompilerGenerated]
		private List<RewardBase> __Rewards__BackingField;

		[Inject]
		private UINavigator uiNavigator;

		[Inject]
		private BossTowerModel bossTowerModel;

		[Inject]
		private IAudioPlayer audioPlayer;

		[Inject]
		private CheatsModel cheatsModel;

		private ViewContainer<RewardView, RewardBase> rewardsViewContainer;

		private string bossKey;

		private bool actionTaken;

		private int bossFloor;

		private BossModel bossModel;

		private ComputedAtom<List<RewardBase>> __Rewards;

		private List<RewardBase> Rewards
		{
			[CompilerGenerated]
			get
			{
			// Native atom getter ELF 0x13c18ac.
			LifetimeScopeExtension.ThrowIfDisposed(this);
			if (__Rewards == null) __Rewards = CodeGenAtom.CreatePooled(this, null, () => Rewards, false);
			return __Rewards.DirectEvaluate() ? __Rewards__BackingField : __Rewards.Value;
		}
			[CompilerGenerated]
			set
			{
			// Native atom setter ELF 0x13c19d4.
			LifetimeScopeExtension.ThrowIfDisposed(this);
			if (__Rewards != null && !__Rewards.CompareAndInvalidate(value)) return;
			__Rewards__BackingField = value;
		}
		}

		protected override void Awake()
		{
			// Native ELF 0x13c1a54.
			base.Awake(); hubButton.onClick.AddListener(GoToHub); continueButton.onClick.AddListener(Continue);
		}

		
		private async UniTask SetScrollValue()
		{
			// Native MoveNext ELF 0x13c32e4.
			for (int i = 0; i < 50; i++) { scrollRect.horizontalNormalizedPosition = 0f; await UniTask.NextFrame(destroyCancellationToken); }
		}

		protected override void Start()
		{
			// Native ELF 0x13c1be4; source closure 0x13c261c, setup 0x13c2708.
			base.Start();
			rewardsViewContainer = new ViewContainer<RewardView, RewardBase>(rewardsContainer, rewardPrefab)
			    .BindTo(Lifetime, () => Rewards ?? new List<RewardBase>()).Setup(SetupRewardView);
		}

		private static void SetupRewardView(RewardView view, RewardBase reward)
		{
			// Native ELF 0x13c1de0.
			view.Setup(reward, RewardViewStyle.Simplified); view.SetInfoBtnVisible(false);
		}

		public void Setup(BossModel bossModel, List<RewardBase> displayRewards, int floor)
		{
			// Native ELF 0x13c1e18.
			actionTaken = false; bossKey = bossModel.Def.Key; Rewards = displayRewards; this.bossModel = bossModel; bossFloor = floor;
			bossNameText.text = string.Format("{0} - <color=#FFD500>{1}", LocalizationService.Get("BOSS_NAME_" + bossModel.Def.DisplayName), bossFloor);
			hitCellsBar.SetValue(0);
			floorTxt.text = string.Format("{0} {1}", LocalizationService.Get("BOSS_TOWER_FLOOR"), bossFloor);
			PlayAnimation().Forget(); _ = SetScrollValue();
		}

		
		private async UniTaskVoid PlayAnimation()
		{
			// Native MoveNext ELF 0x13c2b0c; 0.1s is original ELF rodata 0x920f84.
			audioPlayer.PlaySoundOnce("boss_tower_win"); await animController.PlayAsync();
			int floor = bossFloor % 10; if (floor < 1) floor = 10;
			for (int i = 1; i <= floor; i++) { hitCellsBar.SetValue(i - 1); audioPlayer.PlaySoundOnce("hit_bar_change"); await UniTask.WaitForSeconds(.1f); }
			hitCellsBar.SetFinal(floor - 1); await UniTask.Yield();
		}

		private void SkipAnimation()
		{
			// Native ELF 0x13c212c.
			hitCellsBar.SetValue(bossFloor); hitCellsBar.SetFinal(bossFloor); animController.Complete(true);
		}

		private void GoToHub()
		{
			// Native ELF 0x13c21b8; guard precedes reward commands and flow dispatch.
			if (actionTaken) return; actionTaken = true;
			GrantRewardsWithoutDropMultiplier(); App.InitiateFlow(new LeaveBossArenaFlow()).Forget(); CloseWithDelay().Forget();
		}

		private void Continue()
		{
			// Native ELF 0x13c2540; ReviveFirst stays false, IsCalledFromArena is true.
			if (actionTaken) return; actionTaken = true;
			GrantRewardsWithoutDropMultiplier();
			App.InitiateFlow(new StartBossArenaFloorFlow { BossKey = bossKey, IsCalledFromArena = true }).Forget(); CloseWithDelay().Forget();
		}

		private void GrantRewardsWithoutDropMultiplier()
		{
			// Native ELF 0x13c2260. Original restores the old multiplier after normal enumeration, not in a finally.
			int multiplier = cheatsModel.DropMultiplier; cheatsModel.DropMultiplier = 1;
			foreach (RewardBase reward in Rewards) App.ExecuteCommand(new GrantRewardCommand { Reward = reward });
			cheatsModel.DropMultiplier = multiplier;
		}

		
		private async UniTask CloseWithDelay()
		{
			// Native MoveNext ELF 0x13c2714.
			await UniTask.WaitForSeconds(1f, cancellationToken: destroyCancellationToken); Close();
		}
	}
}
