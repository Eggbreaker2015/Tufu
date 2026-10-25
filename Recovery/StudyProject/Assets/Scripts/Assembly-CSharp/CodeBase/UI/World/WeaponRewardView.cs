using Codebase.Core;
using Codebase.Player.Inventory;
using Codebase.UI.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.World
{
	public class WeaponRewardView : MonoBehaviour
	{
		[SerializeField]
		private GameObject root;

		[SerializeField]
		private InventoryItemView view;

		[SerializeField]
		private Transform raysRoot;

		[SerializeField]
		private TMP_Text levelUpText;

		[SerializeField]
		private TextMeshProUGUI damageText;

		[SerializeField]
		private TMP_Text copiesText;

		[SerializeField]
		private CanvasGroup canvasGroup;

		[SerializeField]
		private Image[] rays;

		[Inject]
		private ItemsProvider itemsProvider;

		private Vector3 textStartPos;

		private float textFloatDistance = 30f;

		private float textFadeStart = 0.7f;

		private ItemModel model;

		private float timer = -1f;

		public bool Disabled { get; set; }

		public bool IsVisible => timer >= 0f;

		private void Awake()
		{
            global::Codebase.Core.GameContext.Set(this);
            textStartPos = levelUpText.rectTransform.anchoredPosition;
        }

		public void Setup(ItemModel model, int previousLevel, bool upgrade = true, bool isCopy = false)
		{
// Original ELF VA 0x1603858. Copy and evolution feedback follow separate original branches.
            if (Disabled) return;
            this.model = model;
            bool useLevelFeedback;
            Rarity rarity;
            if (isCopy)
            {
                useLevelFeedback = model.IsPreDivine;
                bool forceBaseRarity = model.IsPreDivine;
                view.Initialize(model, false, forceBaseRarity);
                rarity = forceBaseRarity ? model.Config.Rarity : model.Rarity;
            }
            else
            {
                view.Initialize(model, false, false);
                useLevelFeedback = true;
                rarity = model.Rarity;
            }
            timer = (float)rarity / 4f + 2.5f;
            foreach (Image ray in rays) ray.color = itemsProvider.GetRarityColor(rarity);
            string statName = global::Codebase.Utilities.LocalizationStrings.Get(model.IsArmor ? "STAT_Armor" : "STAT_Damage");
            if (!useLevelFeedback)
            {
                int copiesAdded = unchecked(model.Copies - previousLevel);
                if (copiesAdded < 2) copiesAdded = 1;
                double perCopy = model.StatPerCopy;
                levelUpText.text = "<sprite name=atlas_42>" + global::Codebase.Utilities.LocalizationStrings.GetPlural("REWARD_COPY_ADDED", copiesAdded) + "<sprite name=atlas_42>";
                damageText.text = statName + " <color=green>+" + global::Codebase.Utilities.DoubleExtensions.Format(perCopy * (double)copiesAdded) + "</color>";
                SetCopiesTextVisible(true);
                bool canEvolve = model.CanEvolve;
                int copies = model.Copies;
                int required = model.LevelsToEvolve;
                copiesText.text = string.Format(canEvolve ? "<color=yellow>{0}/{1}</color>" : "{0}/{1}", copies, required);
                return;
            }
            if (isCopy || upgrade)
            {
                levelUpText.text = "<sprite name=atlas_42>" + global::Codebase.Utilities.LocalizationStrings.Get("REWARD_LEVEL_UP") + "<sprite name=atlas_42>";
                double oldValue = model.IsArmor ? model.CalcArmor(previousLevel) : model.CalcDamage(previousLevel);
                double newValue = model.IsArmor ? model.Armor : model.Damage;
                damageText.text = statName + " <color=white>" + global::Codebase.Utilities.DoubleExtensions.Format(oldValue) + " </color>> <color=green>" + global::Codebase.Utilities.DoubleExtensions.Format(newValue);
            }
            else
            {
                levelUpText.text = global::Codebase.Utilities.LocalizationStrings.Get(model.IsArmor ? "REWARD_NEW_ARMOR" : "REWARD_NEW_WEAPON");
                damageText.text = string.Empty;
            }
            SetCopiesTextVisible(false);
		}

		private void SetCopiesTextVisible(bool visible)
		{
// Original ELF VA 0x1603f78: optional original copy counter is the sole guarded reference.
            if (copiesText == null) return;
            copiesText.gameObject.SetActive(visible);
		}

		public void Update()
		{
// Original ELF VA 0x160401c. The original B.PL accepts unordered timer for this branch.
            if (timer < 0f) { root.SetActive(false); return; }
            root.SetActive(true);
            timer -= Time.deltaTime;
            raysRoot.Rotate(Vector3.forward, Time.deltaTime * 60f);
            Vector3 offset = Vector3.up * textFloatDistance * (timer / -3f + 1f);
            levelUpText.rectTransform.anchoredPosition = textStartPos + offset;
            damageText.rectTransform.anchoredPosition = textStartPos + offset + Vector3.down * 60f;
            float alpha = 1f;
            if (timer < textFadeStart) alpha = Mathf.Clamp01(1f - (textFadeStart - timer) / textFadeStart);
            canvasGroup.alpha = alpha;
		}
	}
}
