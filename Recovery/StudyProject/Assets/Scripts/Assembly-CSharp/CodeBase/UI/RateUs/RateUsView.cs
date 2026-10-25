using Codebase.Analytics;
using Codebase.Core;
using Codebase.Core.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using RuStore.Review;
using Codebase.Analytics.Events;

namespace Codebase.UI.RateUs
{
	public class RateUsView : PopupView
	{
		[SerializeField]
		private GameObject Root;
		[SerializeField]
		private GameObject RateState;
		[SerializeField]
		private GameObject ReviewState;
		[SerializeField]
		private TMP_InputField ReviewInputField;
		[SerializeField]
		private Button ReviewButton;
		[Inject]
		private UserModel userModel;
		[Inject]
		private IAnalyticsWrapper analytics;
		private int rating;
		private bool ruStoreInitialized;
		private bool ruStoreReady;
		private static string GooglePlayURL
		{
			get
			{
				return "https://play.google.com/store/apps/details?id=com.multicastgames.butcher";
			}
		}

		private static string RuStoreURL
		{
			get
			{
				return "https://www.rustore.ru/catalog/app/" + Application.identifier;
			}
		}

		private static bool IsRuStore
		{
			get
			{
				return Application.installerName == "ru.vk.store";
			}
		}

		protected override void Awake()
		{
			base.Awake();
			ReviewButton.onClick.AddListener(() =>
			{
				analytics.Send(new RateUsEvent(rating, ReviewInputField.text));
				userModel.CompletedRateUs.Value = true;
				Close();
			});
		}

		public void SetRating(int rating)
		{
			this.rating = rating;
			if (rating < 5)
			{
				ReviewState.SetActive(true);
				RateState.SetActive(false);
				return;
			}

			userModel.CompletedRateUs.Value = true;
			analytics.Send(new RateUsEvent(this.rating, ""));
			if (!IsRuStore)
				Application.OpenURL(GooglePlayURL);
			else if (ruStoreReady)
				RuStoreReviewManager.Instance.LaunchReviewFlow(error => Debug.LogError("RsLaunchReviewFlow: " + error.description), () =>
				{
				});
			Close();
		}

		public void SetActive(bool active)
		{
			ReviewState.SetActive(false);
			RateState.SetActive(true);
			Root.SetActive(active);
			if (!IsRuStore || !active)
				return;
			if (!ruStoreInitialized)
			{
				ruStoreInitialized = true;
				RuStoreReviewManager.Instance.Init();
			}

			ruStoreReady = false;
			RuStoreReviewManager.Instance.RequestReviewFlow(error => Debug.LogError("RsRequestReviewFlow: " + error.name + "-" + error.description), () => ruStoreReady = true);
		}
	}
}
