using System.Globalization;
using System.Collections.Generic;
using Codebase.Core;
using Codebase.Economy;
using Codebase.Player.Input;
using Codebase.UI;
using Codebase.UI.Pets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.Rpg.UI
{
	public class RpgProgressHudView : MonoBehaviour
	{
		private static Sprite whiteSprite;
		private RpgProgressModel progress;
		private CurrencyModel currency;
		private TMP_Text levelText;
		private TMP_Text xpText;
		private TMP_Text goldText;
		private Image xpFill;
		private Transform levelPass;
		private Transform firstBuyPack;
		private RectTransform cheatsRect;
		private Vector2 cheatsPosition;
		private TMP_Text notice;
		private float noticeUntil;
		private float noticeStarted;
		private readonly Queue<string> notices = new Queue<string>();

		private class Owner : MonoBehaviour
		{
			public RpgProgressHudView View;
			private void OnDestroy() { if (View != null) Destroy(View.gameObject); }
		}

		public static void Attach(GameObject owner, Joystick joystick)
		{
			RpgProgressHudView view = Create(joystick);
			if (view != null) owner.AddComponent<Owner>().View = view;
		}

		public static RpgProgressHudView Create(Joystick joystick)
		{
			if (joystick == null || joystick.transform.parent == null) return null;
			Transform parent = joystick.transform.parent;
			RectTransform root = PetUiBuilder.CreateRect(parent, "RpgProgressHud");
			PetUiBuilder.SetRect(root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 350f), new Vector2(900f, 90f));
			RpgProgressHudView view = root.gameObject.AddComponent<RpgProgressHudView>();
			view.progress = GameContext.Get<RpgProgressModel>();
			view.currency = GameContext.Get<CurrencyModel>();
			view.Build();
			view.Prune(parent);
			RpgRewardService.Granted += view.ShowReward;
			RpgRecruitService.Recruited += view.ShowRecruit;
			RpgRecruitService.ShardsGained += view.ShowShards;
			RpgRecruitService.StarChanged += view.ShowStar;
			return view;
		}

		private void Build()
		{
			if (whiteSprite == null) whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
			levelText = PetUiBuilder.CreateText(transform, "Level", 36f, TextAlignmentOptions.Center);
			PetUiBuilder.SetRect(levelText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(140f, 70f));
			Image background = PetUiBuilder.CreateImage(transform, "XpBackground", whiteSprite, Color.white);
			background.color = new Color(0.08f, 0.1f, 0.15f, 0.9f);
			background.raycastTarget = false;
			PetUiBuilder.SetRect(background.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(145f, 0f), new Vector2(480f, 50f));
			xpFill = PetUiBuilder.CreateImage(background.transform, "Fill", whiteSprite, Color.white);
			xpFill.color = new Color(0.2f, 0.55f, 0.95f, 1f);
			xpFill.raycastTarget = false;
			xpFill.type = Image.Type.Filled;
			xpFill.fillMethod = Image.FillMethod.Horizontal;
			PetUiBuilder.Stretch(xpFill.rectTransform, new Vector2(3f, 3f), new Vector2(-3f, -3f));
			xpText = PetUiBuilder.CreateText(background.transform, "Xp", 28f, TextAlignmentOptions.Center);
			PetUiBuilder.Stretch(xpText.rectTransform, Vector2.zero, Vector2.zero);
			Sprite icon = RpgModeConfig.Instance.GoldIcon;
			if (icon != null)
			{
				Image goldIcon = PetUiBuilder.CreateImage(transform, "GoldIcon", icon, Color.clear);
				goldIcon.raycastTarget = false;
				goldIcon.preserveAspect = true;
				PetUiBuilder.SetRect(goldIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(645f, 0f), new Vector2(52f, 52f));
			}
			goldText = PetUiBuilder.CreateText(transform, "Gold", 34f, TextAlignmentOptions.Left);
			PetUiBuilder.SetRect(goldText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(icon != null ? 705f : 645f, 0f), new Vector2(icon != null ? 195f : 255f, 70f));
		}

		private void Update()
		{
			levelText.text = "Lv " + progress.Level;
			double need = progress.XpToNext;
			xpFill.fillAmount = need > 0 ? Mathf.Clamp01((float)(progress.Xp / need)) : 1f;
			xpText.text = need > 0 ? Format(progress.Xp) + " / " + Format(need) + " XP" : "MAX";
			goldText.text = Format(currency.Get(CurrencyKeys.RpgGold));
			if (notice != null && notice.gameObject.activeSelf) notice.rectTransform.anchoredPosition = new Vector2(0f, 250f + (Time.unscaledTime - noticeStarted) * 18f);
			if (notice != null && Time.unscaledTime >= noticeUntil)
			{
				notice.gameObject.SetActive(false);
				if (notices.Count > 0) DisplayNotice(notices.Dequeue());
			}
		}

		private void ShowReward(Vector3 position, double xp, int gold)
		{
			if (!GameContext.Has<StatRewardContainer>()) return;
			StatRewardContainer container = GameContext.Get<StatRewardContainer>();
			if (xp > 0) container.Create("<color=#80CFFF>+" + Format(xp) + "经验");
			if (gold > 0) container.Create("<color=#FFCD38>+" + gold + "金币");
		}

		private void ShowRecruit(Codebase.Player.Inventory.ItemModel item)
		{
			var definition = RpgModeConfig.Instance.ResolveHero(item.Config);
			bool deployed = GameContext.Get<Codebase.Player.Models.PlayerModel>().EquippedItems.Contains(item);
			ShowNotice(RpgHeroCard.Name(item) + "（" + RpgHeroCard.Role(definition.Role) + "）加入队伍" + (deployed ? "" : "\n已进入名单"));
		}

		private void ShowShards(Codebase.Player.Inventory.ItemModel item, int amount, Vector3 position)
		{
			if (amount <= 0 || !GameContext.Has<StatRewardContainer>()) return;
			GameContext.Get<StatRewardContainer>().Create("<color=#C9A5FF>+" + amount + "碎片");
		}

		private void ShowStar(Codebase.Player.Inventory.ItemModel item, int star)
		{
			ShowNotice(RpgHeroCard.Name(item) + " 升至 ★" + star);
		}

		private void ShowNotice(string text)
		{
			if (notice != null && notice.gameObject.activeSelf)
			{
				if (notices.Count >= 5) notices.Dequeue();
				notices.Enqueue(text);
				return;
			}
			DisplayNotice(text);
		}

		private void DisplayNotice(string text)
		{
			if (notice == null)
			{
				notice = PetUiBuilder.CreateText(transform.parent, "RpgRecruitNotice", 42f, TextAlignmentOptions.Center);
				PetUiBuilder.SetRect(notice.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 250f), new Vector2(900f, 130f));
				notice.raycastTarget = false;
			}
			PetStatsText.SetText(notice, text);
			notice.gameObject.SetActive(true);
			noticeStarted = Time.unscaledTime;
			noticeUntil = noticeStarted + 2.5f;
		}

		private static string Format(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

		private void Prune(Transform parent)
		{
			Transform cheats = parent.Find("HudButtons/Cheats");
			if (cheats == null) Debug.LogWarning("[RPG] HUD path not found: HudButtons/Cheats");
			else if (cheats is RectTransform rect) { cheatsRect = rect; cheatsPosition = rect.anchoredPosition; rect.anchoredPosition += new Vector2(-150f, 0f); }
			string[] paths = {
				"SafeArea/ButtonsContainer/VipButton", "LeftSideContainer/EventButton", "LeftSideContainer/Rewarded", "LeftSideContainer/GemOffer",
				"HudButtons/Container/InnerContainer/Crystals", "HudButtons/Container/InnerContainer/ShopButton",
				"HudButtons/Container/InnerContainer/LevelPass", "HudButtons/Container/InnerContainer/FirstBuyPack"
			};
			foreach (string path in paths)
			{
				Transform target = parent.Find(path);
				if (target == null) { Debug.LogWarning("[RPG] HUD path not found: " + path); continue; }
				if (path.EndsWith("/LevelPass")) levelPass = target;
				if (path.EndsWith("/FirstBuyPack")) firstBuyPack = target;
				target.gameObject.SetActive(false);
			}
		}

		private void LateUpdate()
		{
			if (levelPass != null && levelPass.gameObject.activeSelf) levelPass.gameObject.SetActive(false);
			if (firstBuyPack != null && firstBuyPack.gameObject.activeSelf) firstBuyPack.gameObject.SetActive(false);
		}

		private void OnDestroy()
		{
			RpgRewardService.Granted -= ShowReward;
			RpgRecruitService.Recruited -= ShowRecruit;
			RpgRecruitService.ShardsGained -= ShowShards;
			RpgRecruitService.StarChanged -= ShowStar;
			if (notice != null) Destroy(notice.gameObject);
			if (cheatsRect != null) cheatsRect.anchoredPosition = cheatsPosition;
		}
	}
}
