using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using Multicast.CloudSave;

namespace Codebase.UI
{
	public sealed class DevourCloudSavePrompt : ICloudSavePrompt
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CAskChoice_003Ed__2 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskVoidMethodBuilder _003C_003Et__builder;

			public CloudSaveConflict conflict;

			public Action<CloudSaveChoice?> respond;

			private UniTask<CloudSaveChoice?>.Awaiter _003C_003Eu__1;

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

		public void AskConflict(CloudSaveConflict conflict, Action<CloudSaveChoice?> respond)
		{
		}

		public void ShowRestartRequired()
		{
		}

		[AsyncStateMachine(typeof(_003CAskChoice_003Ed__2))]
		private static UniTaskVoid AskChoice(CloudSaveConflict conflict, Action<CloudSaveChoice?> respond)
		{
			return default;
		}
	}
}
