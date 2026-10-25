using DG.Tweening;
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
			if (progressFill == null || stageTemplate == null) return;
			progressFill.sizeDelta = new Vector2(progressFill.sizeDelta.x, stageTemplate.rect.height * Mathf.Clamp(progress, 0, totalStages));
			if (isEnabled) return;
			ScrollDown(progress, totalStages).Forget();
			isEnabled = true;
		}

		private async UniTaskVoid ScrollDown(int progress, int totalStages)
		{
			await UniTask.Yield();
			float contentHeight = content.rect.height;
			float ownHeight = ((RectTransform)transform).rect.height;
			content.DOLocalMoveY(ownHeight / totalStages * progress - contentHeight * 0.5f, 0.5f).SetEase(Ease.OutQuad);
		}

		private void OnDisable()
		{
			isEnabled = false;
		}

		private void LateUpdate()
		{
			progressFill.SetSiblingIndex(0);
		}
	}
}
