using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using TMPro;
using UnityEngine;
using System.Collections.Generic;
using Codebase.Core;
using Codebase.Analytics;

namespace Codebase.UI
{
	public class TypewriterText : MonoBehaviour
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CReveal_003Ed__14 : IAsyncStateMachine
		{
			public int _003C_003E1__state;
			public AsyncUniTaskVoidMethodBuilder _003C_003Et__builder;
			public TypewriterText _003C_003E4__this;
			public CancellationToken token;
			private int _003Cvisible_003E5__2;
			private UniTask<bool>.Awaiter _003C_003Eu__1;
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
		private TMP_Text text;
		[SerializeField]
		private float charactersPerSecond = 40f;
		private const float EllipsisPauseSeconds = 0.5f;
		private CancellationTokenSource cts;
		private int totalCharacters;
		[CompilerGenerated]
		private Action m_Completed;
		public bool IsTyping { get; private set; }

		public event Action Completed
		{
			add
			{
				Action oldValue, newValue;
				do
				{
					oldValue = m_Completed;
					newValue = (Action)Delegate.Combine(oldValue, value);
				}
				while (!ReferenceEquals(Interlocked.CompareExchange(ref m_Completed, newValue, oldValue), oldValue));
			}

			remove
			{
				Action oldValue, newValue;
				do
				{
					oldValue = m_Completed;
					newValue = (Action)Delegate.Remove(oldValue, value);
				}
				while (!ReferenceEquals(Interlocked.CompareExchange(ref m_Completed, newValue, oldValue), oldValue));
			}
		}

		public void Play(string content)
		{ // Original ELF 0x15fe548; candidate only.
			cts?.Cancel();
			cts = new CancellationTokenSource();
			text.text = content;
			text.ForceMeshUpdate(false, false);
			totalCharacters = text.textInfo.characterCount;
			text.maxVisibleCharacters = 0;
			IsTyping = true;
			Reveal(cts.Token).Forget();
		}

		public void Skip()
		{ // Original ELF 0x15fe724; candidate only.
			cts?.Cancel();
			cts = null;
			text.maxVisibleCharacters = totalCharacters;
			IsTyping = false;
			m_Completed?.Invoke();
		}

		private async UniTaskVoid Reveal(CancellationToken token)
		{ // Original ELF 0x15fe674; candidate only.
			int visible = 0;
			while (visible < totalCharacters)
			{
				visible++;
				text.maxVisibleCharacters = visible;
				await UniTask.Delay(TimeSpan.FromSeconds(1f / charactersPerSecond), false, PlayerLoopTiming.Update, token).SuppressCancellationThrow();
				if (token.IsCancellationRequested)
					return;
				if (ShouldPauseAfterEllipsis(visible))
				{
					await UniTask.Delay(TimeSpan.FromSeconds(.5), false, PlayerLoopTiming.Update, token).SuppressCancellationThrow();
					if (token.IsCancellationRequested)
						return;
				}
			}

			IsTyping = false;
			m_Completed?.Invoke();
		}

		private bool ShouldPauseAfterEllipsis(int visibleCount)
		{ // Original ELF 0x15fe79c; candidate only.
			if (visibleCount < 1)
				return false;
			var characters = text.textInfo.characterInfo;
			char last = characters[visibleCount - 1].character;
			if (last == '…')
				return true;
			return visibleCount >= 3 && last == '.' && characters[visibleCount - 3].character == '.' && characters[visibleCount - 2].character == '.';
		}
	}
}
