using System.Globalization;
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

		private static RpgProgressHudView Create(Joystick joystick)
		{
			if (!RpgModeConfig.Enabled || joystick == null || joystick.transform.parent == null) return null;
			Transform parent = joystick.transform.parent;
			RectTransform root = PetUiBuilder.CreateRect(parent, "RpgProgressHud");
			PetUiBuilder.SetRect(root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 110f), new Vector2(900f, 90f));
			RpgProgressHudView view = root.gameObject.AddComponent<RpgProgressHudView>();
			view.progress = GameContext.Get<RpgProgressModel>();
			view.currency = GameContext.Get<CurrencyModel>();
			view.Build();
			view.Prune(parent);
			RpgRewardService.Granted += view.ShowReward;
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
			if (!RpgModeConfig.Enabled) { Destroy(gameObject); return; }
			levelText.text = "Lv " + progress.Level;
			double need = progress.XpToNext;
			xpFill.fillAmount = need > 0 ? Mathf.Clamp01((float)(progress.Xp / need)) : 1f;
			xpText.text = need > 0 ? Format(progress.Xp) + " / " + Format(need) + " XP" : "MAX";
			goldText.text = Format(currency.Get(CurrencyKeys.RpgGold));
		}

		private void ShowReward(Vector3 position, double xp, int gold)
		{
			if (!RpgModeConfig.Enabled || !GameContext.Has<StatRewardContainer>()) return;
			string value = xp > 0 ? "+" + Format(xp) + " XP" : string.Empty;
			if (gold > 0) value += (value.Length > 0 ? "  " : string.Empty) + "+" + gold + " G";
			if (value.Length > 0) GameContext.Get<StatRewardContainer>().Create(value);
		}

		private static string Format(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

		private void Prune(Transform parent)
		{
			Transform cheats = parent.Find("HudButtons/Cheats");
			if (cheats == null) Debug.LogWarning("[RPG] HUD path not found: HudButtons/Cheats");
			else if (cheats is RectTransform rect) { cheatsRect = rect; cheatsPosition = rect.anchoredPosition; rect.anchoredPosition += new Vector2(-150f, 0f); }
			string[] paths = {
				"SafeArea/ButtonsContainer/VipButton", "LeftSideContainer/EventButton", "LeftSideContainer/Rewarded", "LeftSideContainer/GemOffer",
				"HudButtons/Container/InnerContainer/Crystals", "HudButtons/Container/InnerContainer/ShopButton", "HudButtons/Container/InnerContainer/InventoryButton",
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
			if (!RpgModeConfig.Enabled) return;
			if (levelPass != null && levelPass.gameObject.activeSelf) levelPass.gameObject.SetActive(false);
			if (firstBuyPack != null && firstBuyPack.gameObject.activeSelf) firstBuyPack.gameObject.SetActive(false);
		}

		private void OnDestroy()
		{
			RpgRewardService.Granted -= ShowReward;
			if (cheatsRect != null) cheatsRect.anchoredPosition = cheatsPosition;
		}
	}
}
