using Codebase.Core.UI;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Codebase.Core;
using CodeBase.Audio;

namespace Codebase.UI.BossTower
{
	public class BossTowerPortalUnlockView : PopupView
	{
		[SerializeField]
		private Button continueButton;
		private UniTaskCompletionSource continueSource;
		protected override void Awake()
		{
			base.Awake();
			continueButton.onClick.AddListener(OnContinue);
			GameContext.Get<IAudioPlayer>().PlaySoundOnce("features_pop_up");
		}

		public UniTask WaitForContinue()
		{
			continueSource = new UniTaskCompletionSource();
			return continueSource.Task;
		}

		private void OnContinue()
		{
			continueSource?.TrySetResult();
			Close();
		}

		public override void Cleanup()
		{
			base.Cleanup();
			continueSource?.TrySetResult();
			continueSource = null;
		}
	}
}
