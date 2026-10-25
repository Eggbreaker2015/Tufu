using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CodeBase.Audio;
using Codebase.Camera;
using Codebase.Core;
using Codebase.Core.Commands;
using Codebase.Core.UI;
using Codebase.Player.Models;
using Codebase.Rewards;
using Codebase.SecretChest.Components;
using Codebase.UI;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using Scellecs.Morpeh;

namespace Codebase.SecretChest.Commands
{
	public class OpenSecretChestCommandHandler : ICommandHandler<OpenSecretChestCommand>
	{
		[CompilerGenerated]
		private sealed class _003C_003Ec__DisplayClass13_0
		{
			public SecretChestComponent chest;

			public OpenSecretChestCommandHandler _003C_003E4__this;

			public List<RewardBase> rewards;

			internal void _003COpenAsync_003Eb__0()
			{
			}
		}

		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003COpenAsync_003Ed__13 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskVoidMethodBuilder _003C_003Et__builder;

			public SecretChestComponent chest;

			public OpenSecretChestCommandHandler _003C_003E4__this;

			private _003C_003Ec__DisplayClass13_0 _003C_003E8__1;

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
		private struct _003CPlayOpenSound_003Ed__14 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskVoidMethodBuilder _003C_003Et__builder;

			public float delay;

			public OpenSecretChestCommandHandler _003C_003E4__this;

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

		private const float CameraFocusDelay = 1f;

		private const float OpenSoundLeadTime = 1f;

		private const float RewardPopupDelay = 0.1f;

		private const float CameraDistance = 3.4f;

		private const float CameraHeight = -15f;

		[Inject]
		private UINavigator uiNavigator;

		[Inject]
		private SecretChestModel secretChestModel;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private UiModel uiModel;

		[Inject]
		private CameraFollowSystem cameraFollowSystem;

		[Inject]
		private IAudioPlayer audioPlayer;

		private Filter filter;

		public void Execute(OpenSecretChestCommand command)
		{
		}

		[AsyncStateMachine(typeof(_003COpenAsync_003Ed__13))]
		private UniTaskVoid OpenAsync(SecretChestComponent chest)
		{
			return default;
		}

		[AsyncStateMachine(typeof(_003CPlayOpenSound_003Ed__14))]
		private UniTaskVoid PlayOpenSound(float delay)
		{
			return default;
		}

		private void RestorePlayerState()
		{
		}
	}
}
