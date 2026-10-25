using System.Diagnostics;
using System.Text;
using DG.Tweening;
using Codebase.Localization;
using Codebase.Utilities;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CodeBase.Audio;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.Core
{
	public class LoadingScreen : MonoBehaviour
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CDismissAsync_003Ed__16 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder _003C_003Et__builder;

			public LoadingScreen _003C_003E4__this;

			private TaskAwaiter _003C_003Eu__1;

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

		private const float DismissFadeDuration = 1f;

		private const string TipKeyPrefix = "TIP_";

		private const float TipFadeInDuration = 0.5f;

		[SerializeField]
		private CanvasGroup canvasGroup;

		[SerializeField]
		private Slider progressBar;

		[SerializeField]
		private TextMeshProUGUI progressPercentText;

		[SerializeField]
		private AppLaunchSoundPlayer appLaunchSoundPlayer;

		[SerializeField]
		private AudioListener audioListener;

		[SerializeField]
		private TextMeshProUGUI tipText;

		[SerializeField]
		private CanvasGroup tipCanvas;

		private bool isDismissed;

        // Restored from original APK native LoadingScreen methods.
        public UniTask PlayLaunchSoundAsync()
        {
            return appLaunchSoundPlayer.PlayAsync(destroyCancellationToken);
        }
        public void DisableAudioListener() { audioListener.enabled = false; }
        public void ShowRandomTip()
        {
            var keys = LocalizationService.GetKeysWithPrefix(TipKeyPrefix);
            string key = keys[UnityEngine.Random.Range(0, keys.Count)];
            LocalizationFontApplier.Register(gameObject);
            tipText.text = ApplyHighlightStyle(LocalizationStrings.Get(key));
            tipCanvas.alpha = 0f;
            tipCanvas.DOFade(1f, TipFadeInDuration);
        }
        private static string ApplyHighlightStyle(string text)
        {
            var builder = new StringBuilder(text.Length);
            bool opening = true;
            foreach (char character in text)
            {
                if (character == '$')
                {
                    builder.Append(opening ? "<style=$>" : "</style>");
                    opening = !opening;
                }
                else builder.Append(character);
            }
            return builder.ToString();
        }
        public void SetProgress(float normalizedProgress)
        {
            if (isDismissed) return;
            progressBar.gameObject.SetActive(normalizedProgress > 0f);
            progressPercentText.gameObject.SetActive(normalizedProgress > 0f);
            progressBar.value = normalizedProgress;
            progressPercentText.text = string.Format("{0}%", Mathf.RoundToInt(normalizedProgress * 100f));
        }
        public async UniTask DismissAsync()
        {
            if (isDismissed) return;
            isDismissed = true;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            await canvasGroup.DOFade(0f, DismissFadeDuration).AsyncWaitForCompletion();
            Destroy(gameObject);
        }
    }
}
