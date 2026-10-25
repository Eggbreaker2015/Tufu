using Codebase.UI.GemsHudOffer;
using Codebase.Core.UI;
using Codebase.Analytics.Events;
using Codebase.Utilities;
using Codebase.Unit;
using Codebase.Player.Systems;
using Codebase.Player.Models;
using Codebase.Player;
using Codebase.Core;
using System.Linq;
using Codebase.Analytics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.Integrations.Ads
{
	public class AdView : MonoBehaviour
	{
		private UserModel userModel;

		private AdsInitialize ads;

		private GemsHudOfferModel gemsHudOfferModel;

		[SerializeField]
		private GameObject root;

		[SerializeField]
		private TMP_Text value;

		[SerializeField]
		private Image timer;

		[SerializeField]
		private TMP_Text timerText;

		[SerializeField]
		private Image icon;

		[SerializeField]
		private Button button;

		[SerializeField]
		private GameObject doubleValue;

		[SerializeField]
		private Sprite gemsOfferIcon;

		[SerializeField]
		private Image adIcon;

		[SerializeField]
		private GameObject noAdsIcon;

		[SerializeField]
		private bool isAdOffer;

		private float scaleTimer;

		private void Start()
		{
            // Native ELF0x14c7dac.
            userModel = GameContext.Get<UserModel>(); ads = GameContext.Get<AdsInitialize>();
            if (!isAdOffer) gemsHudOfferModel = GameContext.Get<GemsHudOfferModel>(); button.onClick.AddListener(OnButtonClicked);
        }

		private void OnButtonClicked()
		{
            // Native ELF0x14c7f08.
            if (!isAdOffer) { OnGemsOfferClicked(); return; }
            if (userModel.NoAdsPurchased.Value) GrantRewardWithoutAd();
            else if (MaxSdk.IsRewardedAdReady(ads.RewardedAdUnitId)) MaxSdk.ShowRewardedAd(ads.RewardedAdUnitId);
        }

		private void GrantRewardWithoutAd()
		{
            // Native ELF0x14c8038.
            string key = userModel.CurrentRewardKey; double amount = userModel.CurrentRewardValue;
            Stat stat = GameContext.Get<PlayerModel>().Unit.Stats.First(x => x.Type.ToString() == key);
            GameContext.Get<PlayerRewardSystem>().GrantStatReward(new StatModifier { Name = "no_ads", ModifierType = ModifierType.Flat, Source = ModifierSource.Ads, StatType = stat.Type, Value = (float)amount });
            GameContext.Get<IAnalyticsWrapper>().Send(new RewardedNoAdsEvent("collected", key, amount)); ads.ResetAfterNoAdsReward();
        }

		private void OnGemsOfferClicked()
		{
            // Native ELF0x14c8360.
            var offer = gemsHudOfferModel.CurrentOffer; if (offer == null) return;
            var popup = GameContext.Get<UINavigator>().Open<GemsHudOfferPopupView>("Assets/Prefabs/UI/Rewarded/GemsOffer.prefab", true); if (popup != null) popup.Setup(offer);
        }

		private void Update()
		{
            // Native ELF0x14c8480.
            if (isAdOffer) UpdateAdMode(); else UpdateGemsOfferMode();
        }

		private void UpdateGemsOfferMode()
		{
            // Native ELF0x14c87fc.
            gemsHudOfferModel.Update(Time.deltaTime);
            if (!gemsHudOfferModel.IsVisible) { root.SetActive(false); return; }
            var offer = gemsHudOfferModel.CurrentOffer; timer.fillAmount = gemsHudOfferModel.TimerNormalized; timerText.text = gemsHudOfferModel.TimerSeconds.ToString("F0");
            if (!root.activeSelf) {
                root.SetActive(true); doubleValue.SetActive(false); value.text = offer.Value.Format();
                icon.sprite = GameContext.Get<IconStyleConfig>().Styles.First(x => x.StatType == offer.StatType).Sprite;
                adIcon.sprite = gemsOfferIcon; adIcon.gameObject.SetActive(true); noAdsIcon.SetActive(false);
            }
            AnimateIcon();
        }

		private void UpdateAdMode()
		{
            // Native ELF0x14c8490.
            if ((MaxSdk.IsRewardedAdReady(ads.RewardedAdUnitId) || userModel.NoAdsPurchased.Value) && ads.Duration > 0f) {
                timer.fillAmount = ads.Duration / ads.MaxDuration;
                if (!root.activeSelf) {
                    root.SetActive(true); doubleValue.SetActive(userModel.IsDoubleValue); value.text = userModel.CurrentRewardValue.Format(); timerText.text = ads.Duration.ToString("F0");
                    icon.sprite = GameContext.Get<IconStyleConfig>().Styles.First(x => x.StatType.ToString() == userModel.CurrentRewardKey).Sprite;
                    bool noAds = userModel.NoAdsPurchased.Value; adIcon.gameObject.SetActive(!noAds); noAdsIcon.SetActive(noAds);
                }
                AnimateIcon();
            } else root.SetActive(false);
        }

		private void AnimateIcon()
		{
            // Native ELF0x14c8db0.
            float dt = Time.deltaTime; scaleTimer = scaleTimer + dt + dt; float scale = Mathf.Sin(scaleTimer) * 0.1f + 1f; icon.rectTransform.localScale = new Vector3(scale, scale, 1f);
        }
	}
}
