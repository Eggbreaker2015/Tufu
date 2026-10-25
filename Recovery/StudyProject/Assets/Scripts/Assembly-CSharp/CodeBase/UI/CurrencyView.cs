using Codebase.Core.UI;
using Codebase.Core;
using Codebase.Player.Models;
using Codebase.Amulet;
using Codebase.Economy;
using Codebase.UI.Amulet;
using Codebase.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI
{
	public class CurrencyView : View
	{
		[SerializeField]
		[KeysDropdown(typeof(CurrencyKeys))]
		private string currencyKey;

		[SerializeField]
		private Image icon;

		[SerializeField]
		private SoulView soulView;

		[SerializeField]
		private TMP_Text amountText;

		private bool isSoulCurrency;

		private const string GemsKey = "gems";

		public RectTransform IconRect => isSoulCurrency ? soulView.IconRect : icon.rectTransform; // Original ELF 0x15d42e4.

		public Sprite IconSprite => isSoulCurrency ? soulView.IconSprite : icon.sprite; // Original ELF 0x15d431c.

		protected override void Awake()
		{
			// Original ELF 0x15d4354.
			base.Awake();
			ApplyIcon();
		}

		public void Setup(string currencyKey)
		{
			// Original ELF 0x15d45e4: icon, initialization and explicit render in this order.
			this.currencyKey = currencyKey;
			ApplyIcon();
			Initialize();
			Render();
		}

		private void ApplyIcon()
		{
			// Original ELF 0x15d4370. StartsWith uses the original culture-sensitive overload.
			if (string.IsNullOrEmpty(currencyKey))
			{
				isSoulCurrency = false;
				icon.gameObject.SetActive(true);
				soulView.gameObject.SetActive(false);
				return;
			}
			isSoulCurrency = currencyKey.StartsWith("soul.");
			if (isSoulCurrency)
			{
				icon.gameObject.SetActive(false);
				soulView.gameObject.SetActive(true);
				soulView.Setup(GameContext.Get<SoulStyleConfig>().GetStyle(currencyKey.Substring("soul.".Length)));
			}
			else
			{
				icon.gameObject.SetActive(true);
				soulView.gameObject.SetActive(false);
				var sprite = GameContext.Get<CurrencyStyleConfig>().GetIcon(currencyKey);
				if (sprite != null) icon.sprite = sprite;
			}
		}

		private void OnEnable()
		{
			Initialize(); // Original ELF 0x15d4620.
		}

		protected override void Render()
		{
			// Original ELF 0x15d462c: gems live in PlayerModel, other balances in CurrencyModel.
			amountText.text = (currencyKey == GemsKey ? GameContext.Get<PlayerModel>().Gems : GameContext.Get<CurrencyModel>().Get(currencyKey)).Format();
		}
	}
}
