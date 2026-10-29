using Codebase.Abilities;
using Codebase.Localization;
using Codebase.Player.Inventory;
using Codebase.UI.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Pets
{
	public class PetAbilitySummaryRow : MonoBehaviour
	{
		public const float Height = 120f;

		private readonly Image[] icons = new Image[6];
		private Button button;
		private Image buttonImage;
		private TMP_Text label;
		private ItemModel item;

		public static PetAbilitySummaryRow Create(RectTransform parent, string name, TMP_Text labelTemplate, Sprite buttonSprite)
		{
			Image hitArea = PetUiBuilder.CreateImage(parent, name, null, Color.clear);
			hitArea.raycastTarget = true;
			PetAbilitySummaryRow row = hitArea.gameObject.AddComponent<PetAbilitySummaryRow>();
			for (int i = 0; i < row.icons.Length; i++)
			{
				Image icon = PetUiBuilder.CreateImage(hitArea.transform, "Icon" + i, null, Color.white);
				PetUiBuilder.SetRect(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(i * 94f, 0f), new Vector2(80f, 80f));
				icon.preserveAspect = true;
				icon.raycastTarget = false;
				icon.gameObject.SetActive(false);
				row.icons[i] = icon;
			}
			row.buttonImage = PetUiBuilder.CreateImage(hitArea.transform, "Button", buttonSprite, new Color(0.25f, 0.55f, 0.95f, 1f));
			RectTransform buttonRect = row.buttonImage.rectTransform;
			PetUiBuilder.SetRect(buttonRect, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(300f, 100f));
			row.button = hitArea.gameObject.AddComponent<Button>();
			row.button.targetGraphic = row.buttonImage;
			row.button.onClick.AddListener(row.OnClicked);
			row.label = labelTemplate != null ? PetStatsText.CreateBlock(labelTemplate, buttonRect, "Label") : PetUiBuilder.CreateText(buttonRect, "Label", 44f, TextAlignmentOptions.Center);
			LocalizationFontApplier.RegisterLike(row.label, labelTemplate);
			PetUiBuilder.Stretch(row.label.rectTransform, new Vector2(12f, 8f), new Vector2(-12f, -8f));
			row.label.alignment = TextAlignmentOptions.Center;
			row.label.enableVertexGradient = false;
			row.label.color = Color.white;
			row.label.fontSizeMax = 44f;
			row.label.gameObject.SetActive(true);
			row.Render(null);
			return row;
		}

		public void Render(ItemModel item)
		{
			this.item = item;
			var indices = item != null ? PetAbilitiesView.GetAbilityIndices(item) : null;
			var skills = PetAbilitiesView.GetSpeciesSkills(item);
			int count = skills.Count + (indices != null ? indices.Count : 0);
			for (int i = 0; i < icons.Length; i++)
			{
				Image icon = icons[i];
				icon.gameObject.SetActive(i < count);
				if (i >= count) continue;
				if (i < skills.Count)
				{
					icon.sprite = AbilityIcons.Get(skills[i]);
					icon.color = Color.white;
				}
				else
				{
					var def = item.Config.AbilityDefs[indices[i - skills.Count]];
					icon.sprite = AbilityIcons.Get(def);
					icon.color = item.IsAbilityUnlocked(def) ? Color.white : ItemAbilityView.LockedIconTint;
				}
				icon.enabled = icon.sprite != null;
			}
			PetStatsText.SetText(label, "技能 " + count + " ›");
			button.interactable = count > 0;
			if (buttonImage.sprite != null) buttonImage.color = count > 0 ? Color.white : new Color(0.55f, 0.55f, 0.55f, 1f);
		}

		private void OnClicked()
		{
			PetUiBuilder.PlayTap();
			PetUiBuilder.OpenAbilities(item);
		}
	}
}
