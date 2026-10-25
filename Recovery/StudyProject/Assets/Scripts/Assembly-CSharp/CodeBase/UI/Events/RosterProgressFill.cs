using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using UnityEngine;

namespace Codebase.UI.Events
{
	public class RosterProgressFill : MonoBehaviour
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CScrollDown_003Ed__5 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskVoidMethodBuilder _003C_003Et__builder;

			public RosterProgressFill _003C_003E4__this;

			public int totalStages;

			public int progress;

			private Cysharp.Threading.Tasks.YieldAwaitable.Awaiter _003C_003Eu__1;

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
		private RectTransform progressFill;

		[SerializeField]
		private RectTransform stageTemplate;

		[SerializeField]
		private RectTransform content;

		private bool isEnabled;

		public void Setup(int progress, int totalStages)
		{
		}

		[AsyncStateMachine(typeof(_003CScrollDown_003Ed__5))]
		private UniTaskVoid ScrollDown(int progress, int totalStages)
		{
			return default;
		}

		private void OnDisable()
		{
		}

		private void LateUpdate()
		{
		}
	}
}
