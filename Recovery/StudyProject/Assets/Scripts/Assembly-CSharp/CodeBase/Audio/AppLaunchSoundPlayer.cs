using System.Diagnostics;
using Codebase.Core;
using Codebase.Core.UserData;
using Codebase.Analytics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using UnityEngine;
using UnityEngine.Video;

namespace CodeBase.Audio
{
	public class AppLaunchSoundPlayer : MonoBehaviour
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CPlayAsync_003Ed__6 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder _003C_003Et__builder;

			public AppLaunchSoundPlayer _003C_003E4__this;

			public CancellationToken cancellationToken;

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
		private struct _003CWaitForFirstVideoFrameAsync_003Ed__7 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder _003C_003Et__builder;

			public AppLaunchSoundPlayer _003C_003E4__this;

			public CancellationToken cancellationToken;

			private float _003Celapsed_003E5__2;

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

		private const string SoundKey = "app_launch";

		private const float VideoFrameTimeout = 5f;

		[SerializeField]
		private AudioSource audioSource;

		[SerializeField]
		private SoundsConfig soundsConfig;

		[SerializeField]
		private VideoPlayer videoPlayer;

		private bool played;

        // Original ARM64 flow restored from Ghidra-RootStartup.
        public async UniTask PlayAsync(CancellationToken cancellationToken = default)
        {
            if (played) return;
            await WaitForFirstVideoFrameAsync(cancellationToken);
            if (!GameContext.Has<GameDataService>() && !GameContext.Has<UserModel>()) return;
            float volume = GetSoundVolume();
            if (volume <= 0f) return;
            if (!soundsConfig.TryGetValue(SoundKey, out var entry)) return;
            if (entry.Clips.Length == 0) return;
            played = true;
            audioSource.PlayOneShot(entry.Clips[0], volume * entry.Volume);
        }
        private async UniTask WaitForFirstVideoFrameAsync(CancellationToken cancellationToken)
        {
            if (videoPlayer == null || IsFirstFrameReady()) return;
            if (!videoPlayer.isPrepared) videoPlayer.Prepare();
            float elapsed = 0f;
            while (!IsFirstFrameReady() && elapsed < VideoFrameTimeout)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                elapsed += Time.unscaledDeltaTime;
            }
        }
        private bool IsFirstFrameReady()
        {
            return videoPlayer.frame > 0 || (videoPlayer.isPrepared && videoPlayer.isPlaying && videoPlayer.texture != null);
        }
        private float GetSoundVolume()
        {
            return GameContext.Has<UserModel>()
                ? GameContext.Get<UserModel>().SoundVolume.Value
                : GameContext.Get<GameDataService>().Data.User.SoundVolume.Value;
        }
    }
}
