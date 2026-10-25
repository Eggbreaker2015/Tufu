using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CodeBase.Audio;
using Codebase.BossTower.Components;
using Codebase.BossTower.Systems;
using Codebase.Core;
using Codebase.Core.Flows;
using Codebase.Player.Models;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using System;
using UnityEngine;
using Codebase.UI.Flows;
using Codebase.BossTower.Commands;
using Codebase.Player.Commands;

namespace Codebase.BossTower.Flows
{
	public struct StartNextFloorFlowRunner : IFlowRunner<StartBossArenaFloorFlow>, IFlowHandlerBase
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CExecute_003Ed__4 : IAsyncStateMachine
		{
			public int _003C_003E1__state;
			public AsyncUniTaskMethodBuilder _003C_003Et__builder;
			public StartNextFloorFlowRunner _003C_003E4__this;
			public StartBossArenaFloorFlow flow;
			private BossArenaComponent _003Carena_003E5__2;
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

		[Inject]
		private BossTowerModel bossTowerModel;
		[Inject]
		private BossArenaSystem bossArenaSystem;
		[Inject]
		private PlayerModel playerModel;
		[Inject]
		private IAudioPlayer audioPlayer;
		public async UniTask Execute(StartBossArenaFloorFlow flow)
		{
			if (!bossTowerModel.TryGetBoss(flow.BossKey, out BossModel boss) || !bossTowerModel.IsBossUnlocked(flow.BossKey))
				return;
			if (!flow.UseCheats && !flow.IsCalledFromArena)
				foreach (var model in bossTowerModel.Bosses)
				{
					model.IsUseEliteBoss = null;
					model.IsUseWeakenedBoss = null;
				}

			string arenaKey = boss.Def.ArenaKey;
			if (!bossArenaSystem.TryGet(arenaKey, out BossArenaComponent arena))
			{
				UnityEngine.Debug.LogError("Arena with key '" + arenaKey + "' not found for boss '" + flow.BossKey + "'");
				return;
			}

			audioPlayer.PlaySoundOnce("boss_fight_start");
			await App.InitiateFlow(new ShowCurtainFlow { disableSound = true });
			if (flow.ReviveFirst)
			{
				App.ExecuteCommand(new RevivePlayerCommand { KillerName = flow.BossKey, restoreHealth = true, revivePosition = arena.PlayerSpawnPoint.position });
				playerModel.Unit.SetAliveState();
			}

			await UniTask.Delay(TimeSpan.FromSeconds(1));
			App.ExecuteCommand(new EnterBossArenaCommand { BossKey = flow.BossKey, IsCalledFromArena = flow.IsCalledFromArena, IsRetry = flow.IsCalledFromArena });
			await UniTask.Delay(TimeSpan.FromSeconds(0.5));
			await App.InitiateFlow(new HideCurtainFlow());
		}
	}
}
