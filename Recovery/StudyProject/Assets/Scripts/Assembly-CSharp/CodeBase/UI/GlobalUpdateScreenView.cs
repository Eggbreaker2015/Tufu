using System.Runtime.CompilerServices;
using CodeBase.Audio;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Mail;
using DG.Tweening;
using UniMob.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI
{
	public class GlobalUpdateScreenView : PopupView
	{
		private const string BaseMailKey = "2_0_0_base";

		private const int PageCount = 7;

		private const float FeatureSoundDelay = 2.2f;

		[SerializeField]
		private Button closeButton;

		[SerializeField]
		private Button nextButton;

		[SerializeField]
		private Button previousButton;

		[SerializeField]
		private GameObject mailObject;

		[SerializeField]
		private GlobalUpdateFeatureView featureA;

		[SerializeField]
		private GlobalUpdateFeatureView featureB;

		[SerializeField]
		private Sprite[] featureSprites;

		[SerializeField]
		private Transform pagesContainer;

		[SerializeField]
		private GlobalUpdatePageIndicatorView[] pageIndicators;

		[SerializeField]
		private float fadeDuration;

		[Inject]
		private IAudioPlayer audioPlayer;

		[Inject]
		private MailsRepository mailsRepository;

		[CompilerGenerated]
		private int __CurrentPage__BackingField;

		private GlobalUpdateFeatureView currentFeature;

		private GlobalUpdateFeatureView nextFeature;

		private Tween fadeTween;

		private Tween featureSoundTween;

		private bool isFading;

		private ComputedAtom<int> __CurrentPage;

		private int CurrentPage
		{
			[CompilerGenerated]
			get
			{
				return 0;
			}
			[CompilerGenerated]
			set
			{
			}
		}

		protected override void Awake()
		{
		}

		public override void Initialize()
		{
		}

		public override void Cleanup()
		{
		}

		protected override void Render()
		{
		}

		private void UpdatePageIndicators()
		{
		}

		private void HandleNext()
		{
		}

		private void HandlePrevious()
		{
		}

		private void SwitchPage(int page)
		{
		}

		private void ApplyPage(GlobalUpdateFeatureView feature, int page)
		{
		}

		private void HandleClose()
		{
		}
	}
}
