using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CodeBase.Audio;
using Codebase.Analytics;
using Codebase.Core;
using Codebase.Core.Commands;
using Codebase.Core.UI;
using Codebase.Core.UserData;
using Codebase.Player.Models;
using Codebase.Rewards;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;

namespace Codebase.Mail.Commands
{
	public class ClaimMailCommandHandler : ICommandHandler<ClaimMailCommand>
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CClaimAsync_003Ed__7 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskVoidMethodBuilder _003C_003Et__builder;

			public ClaimMailCommandHandler _003C_003E4__this;

			public ClaimMailCommand command;

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
		private PlayerModel playerModel;

		[Inject]
		private UserModel userModel;

		[Inject]
		private IAnalyticsWrapper analyticsWrapper;

		[Inject]
		private IAudioPlayer audioPlayer;

		[Inject]
		private UINavigator uiNavigator;

		[Inject]
		private GameDataService gameDataService;

		public void Execute(ClaimMailCommand command)
		{
		}

		[AsyncStateMachine(typeof(_003CClaimAsync_003Ed__7))]
		private UniTaskVoid ClaimAsync(ClaimMailCommand command)
		{
			return default;
		}

		private void GrantRewards(MailModel mail)
		{
		}

		private List<RewardBase> BuildDisplayRewards(MailModel mail)
		{
			return null;
		}
	}
}
