using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Codebase.SaveStore;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI
{
	public class SnapshotSelectorView : MonoBehaviour
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CShowAndWaitForSelection_003Ed__11 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder<IDataSnapshot> _003C_003Et__builder;

			public SnapshotSelectorView _003C_003E4__this;

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
		private GameObject panel;

		[SerializeField]
		private Transform snapshotListContainer;

		[SerializeField]
		private GameObject snapshotButtonPrefab;

		[SerializeField]
		private Button skipButton;

		[SerializeField]
		private TextMeshProUGUI titleText;

		[SerializeField]
		private PlayerPrefsSnapshotStore snapshotStore;

		[SerializeField]
		private UserDataLocalSnapshotStore udLocalSnapshotStore;

		private List<IDataSnapshot> snapshots;

		private bool isWaitingForSelection;

		private IDataSnapshot selectedSnapshot;

		private void Awake()
		{
		}

		[AsyncStateMachine(typeof(_003CShowAndWaitForSelection_003Ed__11))]
		public UniTask<IDataSnapshot> ShowAndWaitForSelection()
		{
			return default;
		}

		private void PopulateSnapshotList()
		{
		}

		private void OnSnapshotSelected(IDataSnapshot snapshot)
		{
		}

		private void OnSkipClicked()
		{
		}

		private static string ParseIsoToLocalString(string iso)
		{
			return null;
		}
	}
}
