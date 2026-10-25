using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Codebase.CloudSave;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using Multicast.CloudSave;
using UnityEngine;

namespace Codebase.UI
{
	public class CloudSaveWindows : MonoBehaviour
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CWaitForChoice_003Ed__7 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder<CloudSaveChoice?> _003C_003Et__builder;

			public CloudSaveWindows _003C_003E4__this;

			public DevourSaveSummary local;

			public DevourSaveSummary cloud;

			public DateTimeOffset? cloudUpdatedAt;

			public DateTimeOffset? serverTime;

			public string cloudClientVersion;

			private UniTask<CloudSaveChoice>.Awaiter _003C_003Eu__1;

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

		private const string RESOURCES_PATH = "CloudSave/CloudSaveConflictWindow";

		[SerializeField]
		private CloudSaveConflictView conflictView;

		[SerializeField]
		private CloudSaveRestartBlockerView restartBlocker;

		private static CloudSaveWindows instance;

		public static UniTask<CloudSaveChoice?> ShowConflict(DevourSaveSummary local, DevourSaveSummary cloud, DateTimeOffset? cloudUpdatedAt, DateTimeOffset? serverTime, string cloudClientVersion)
		{
			return default;
		}

		public static void ShowRestartBlocker()
		{
		}

		private void Awake()
		{
		}

		[AsyncStateMachine(typeof(_003CWaitForChoice_003Ed__7))]
		private UniTask<CloudSaveChoice?> WaitForChoice(DevourSaveSummary local, DevourSaveSummary cloud, DateTimeOffset? cloudUpdatedAt, DateTimeOffset? serverTime, string cloudClientVersion)
		{
			return default;
		}

		private static CloudSaveWindows GetOrCreate()
		{
			return null;
		}
	}
}
