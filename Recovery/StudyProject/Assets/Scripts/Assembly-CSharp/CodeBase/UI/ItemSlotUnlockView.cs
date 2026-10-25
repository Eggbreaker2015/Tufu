using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Player;
using Codebase.Player.Inventory;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI
{
	public class ItemSlotUnlockView : PopupView
	{
		[SerializeField]
		private GameObject normalState;

		[SerializeField]
		private Image normalIcon;

		[SerializeField]
		private GameObject vipState;

		[SerializeField]
		private Image vipIcon;

		[SerializeField]
		private TMP_Text titleText;

		[SerializeField]
		private TMP_Text descriptionText;

		[SerializeField]
		private TMP_Text amountTxt;

		[SerializeField]
		private Button continueButton;

		[Inject]
		private IconStyleConfig iconStyleConfig;

		private UniTaskCompletionSource continueSource;

		protected override void Awake()
		{
            // Original ELF VA 0x15f54f0: base lifetime/render setup precedes the serialized Button listener.
            base.Awake();
            continueButton.onClick.AddListener(OnContinue);
		}

		public void Setup(ItemSlotType slotType, int index, bool isVip)
		{
            // Original ELF VA 0x15f558c. Use the authored unlock sprite, with the original missing-style null result.
            SlotTypeStyle style = iconStyleConfig.SlotTypeStyles.Find(value => value.SlotType == slotType);
            Sprite sprite = style == null ? null : style.UnlockSprite;
            normalState.SetActive(!isVip);
            normalIcon.sprite = sprite;
            vipState.SetActive(isVip);
            vipIcon.sprite = sprite;
            titleText.text = ResolveText("TITLE", slotType, index, isVip);
            descriptionText.text = ResolveText("DESCRIPTION", slotType, index, isVip);
            amountTxt.text = slotType == ItemSlotType.Weapon ? unchecked(index + 1).ToString() : "+";
            GameContext.Get<global::CodeBase.Audio.IAudioPlayer>().PlaySoundOnce("features_pop_up");
		}

		private string ResolveText(string part, ItemSlotType slotType, int index, bool isVip)
		{
            // Original ELF VA 0x15f58c0. VIP text bypasses the normal weapon-number substitution.
            if (isVip) return global::Codebase.Utilities.LocalizationStrings.Get("ITEM_SLOT_UNLOCK_" + part + "_VIP");
            string text = global::Codebase.Utilities.LocalizationStrings.Get("ITEM_SLOT_UNLOCK_" + part + "_" + slotType.ToString().ToUpperInvariant());
            if (slotType == ItemSlotType.Weapon) text = text.Replace("num", unchecked(index + 1).ToString());
            return text;
		}

		private static string ToOrdinal(int number)
		{
            // Original ELF VA 0x15f5a2c: signed remainders and unsigned switch bounds retain negative/int-limit behavior.
            string suffix = "th";
            if (unchecked((uint)(number % 100 - 11)) > 2u)
            {
                uint last = unchecked((uint)(number % 10 - 1));
                if (last < 3u) suffix = last == 0 ? "st" : last == 1 ? "nd" : "rd";
            }
            return string.Format("{0}{1}", number, suffix);
		}

		public UniTask WaitForContinue()
		{
            // Original ELF VA 0x15f5b4c: a fresh promise belongs to each displayed popup.
            continueSource = new UniTaskCompletionSource();
            return continueSource.Task;
		}

		private void OnContinue()
		{
            // Original ELF VA 0x15f5bc0: complete the flow before invoking the popup close callback.
            continueSource?.TrySetResult();
            Close();
		}

		public override void Cleanup()
		{
            // Original ELF VA 0x15f5be8: pooling/disable releases a waiting flow successfully, without cancellation.
            base.Cleanup();
            continueSource?.TrySetResult();
            continueSource = null;
		}
	}
}
