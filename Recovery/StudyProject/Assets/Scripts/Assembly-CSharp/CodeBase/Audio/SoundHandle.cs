using System;
using System.Threading;
using UnityEngine;

namespace CodeBase.Audio
{
	public class SoundHandle
	{
		private readonly AudioSource audioSource;

		private readonly Action onRelease;

		private bool isReleased;

		private CancellationTokenSource cancellationTokenSource;

		public SoundHandle(AudioSource audioSource, CancellationTokenSource cancellationTokenSource, Action onRelease)
		{
			// Native 0x14a8040.
			this.audioSource = audioSource;
			this.cancellationTokenSource = cancellationTokenSource;
			this.onRelease = onRelease;
		}

		public void Stop()
		{
			// Native 0x14aac4c; an already-cancelled source is deliberately not disposed here.
			if (isReleased) return;
			if (cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested)
			{
				cancellationTokenSource.Cancel();
				cancellationTokenSource.Dispose();
				cancellationTokenSource = null;
			}
			if (audioSource != null && audioSource.isPlaying) audioSource.Stop();
			onRelease?.Invoke();
			isReleased = true;
		}
	}
}
