using Codebase.BossTower.Flows;
using Codebase.Localization;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BrunoMikoski.AnimationSequencer;
using CodeBase.Audio;
using Codebase.BossTower;
using Codebase.Core;
using Codebase.Core.UI;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.BossTower
{
	public class BossRaidDefeatView : PopupView
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CCloseWithDelay_003Ed__19 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder _003C_003Et__builder;

			public BossRaidDefeatView _003C_003E4__this;

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
		private struct _003CPlayAnimation_003Ed__15 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskVoidMethodBuilder _003C_003Et__builder;

			public BossRaidDefeatView _003C_003E4__this;

			private int _003Cfloor_003E5__2;

			private UniTask.Awaiter _003C_003Eu__1;

			private int _003Ci_003E5__3;

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
		private struct _003CPlayHealthDropAnimation_003Ed__16 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskVoidMethodBuilder _003C_003Et__builder;

			public BossRaidDefeatView _003C_003E4__this;

			private float _003Cduration_003E5__2;

			private float _003Ct_003E5__3;

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
		private TMP_Text healthText;

		[SerializeField]
		private Button hubButton;

		[SerializeField]
		private Button retryButton;

		[SerializeField]
		private TMP_Text floorTxt;

		[SerializeField]
		private AnimationSequencerController animSequencer;

		[Inject]
		private UINavigator uiNavigator;

		[Inject]
		private IAudioPlayer audioPlayer;

		private string bossKey;

		private bool actionTaken;

		private int bossFloor;

		private float healthPercent;

		protected override void Awake()
		{
			// Native ELF 0x13be430.
			base.Awake(); hubButton.onClick.AddListener(GoToHub); retryButton.onClick.AddListener(Retry);
		}

		public void Setup(BossModel bossModel, double currentHp, double maxHp, int floor)
		{
			// Native ELF 0x13be524. Ratio narrows to float BEFORE multiplication and banker's rounding.
			actionTaken = false; bossKey = bossModel.Def.Key; bossFloor = floor;
			bossNameText.text = string.Format("{0} - <color=#FFD500>{1}", LocalizationService.Get("BOSS_NAME_" + bossModel.Def.DisplayName), floor);
			hitCellsBar.SetValue(bossModel.IsSuperBoss ? 10 : bossFloor % 10);
			healthPercent = (float)Math.Round(maxHp > 0.0 ? (double)((float)(currentHp / maxHp) * 100f) : 0.0, 1);
			healthText.text = LocalizationService.Get("BOSS_TOWER_BOSS_HP_LEFT") + " - <color=#FF005E>100%";
			floorTxt.text = string.Format("{0} {1}", LocalizationService.Get("BOSS_TOWER_FLOOR"), bossFloor);
			hitCellsBar.SetValue(0); PlayAnimation().Forget();
		}

		
		private async UniTaskVoid PlayAnimation()
		{
			// Native MoveNext ELF 0x13befb4.
			audioPlayer.PlaySoundOnce("boss_tower_lose"); await animSequencer.PlayAsync();
			int floor = bossFloor % 10; if (floor < 1) floor = 10;
			for (int i = 1; i <= floor; i++) { hitCellsBar.SetValue(i - 1); audioPlayer.PlaySoundOnce("hit_bar_change"); await UniTask.WaitForSeconds(.1f); }
			hitCellsBar.SetFinal(floor - 1); PlayHealthDropAnimation().Forget();
		}

		
		private async UniTaskVoid PlayHealthDropAnimation()
		{
			// Native MoveNext ELF 0x13bf700; duration=2f at ELF rodata 0x8df6d0, unscaled quadratic interpolation.
			float duration = 2f, t = 0f;
			while (t < 1f) {
			    t = Mathf.Clamp01(t + Time.unscaledDeltaTime / duration);
			    double value = Math.Round(Mathf.Lerp(100f, healthPercent, t * t), 1);
			    healthText.text = string.Format("{0} - <color=#FF005E>{1}%", LocalizationService.Get("BOSS_TOWER_BOSS_HP_LEFT"), value);
			    await UniTask.NextFrame();
			}
		}

		private void GoToHub()
		{
			// Native ELF 0x13be9ac.
			if (actionTaken) return; actionTaken = true;
			App.InitiateFlow(new LeaveBossArenaFlow()).Forget(); CloseWithDelay().Forget();
		}

		private void Retry()
		{
			// Native ELF 0x13beae4; both native booleans are true (0x0101).
			if (actionTaken) return; actionTaken = true;
			App.InitiateFlow(new StartBossArenaFloorFlow { BossKey = bossKey, ReviveFirst = true, IsCalledFromArena = true }).Forget(); CloseWithDelay().Forget();
		}

		
		private async UniTask CloseWithDelay()
		{
			// Native MoveNext ELF 0x13bebbc.
			await UniTask.WaitForSeconds(1f, cancellationToken: destroyCancellationToken); Close();
		}
	}
}
