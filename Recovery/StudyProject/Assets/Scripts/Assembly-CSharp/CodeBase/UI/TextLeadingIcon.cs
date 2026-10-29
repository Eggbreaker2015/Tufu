using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI
{
	// An icon that sits just left of a label's rendered text. The label gets a matching left margin so icon
	// and text stay centred together; the position follows the text every frame (font fallback, locale changes).
	public class TextLeadingIcon : MonoBehaviour
	{
		private const float Gap = 10f;

		private TMP_Text label;

		private Image image;

		private RectTransform rect;

		private Vector4 baseMargin;

		private float size;

		public static TextLeadingIcon GetOrCreate(TMP_Text label)
		{
			TextLeadingIcon icon = label.GetComponentInChildren<TextLeadingIcon>(true);
			if (icon != null)
				return icon;
			GameObject go = new GameObject("LeadingIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
			icon = go.AddComponent<TextLeadingIcon>();
			icon.label = label;
			icon.baseMargin = label.margin;
			icon.size = Mathf.Max(32f, label.fontSize * 1.25f);
			icon.rect = (RectTransform)go.transform;
			icon.rect.SetParent(label.transform, false);
			icon.rect.anchorMin = icon.rect.anchorMax = icon.rect.pivot = new Vector2(0.5f, 0.5f);
			icon.rect.sizeDelta = new Vector2(icon.size, icon.size);
			icon.image = go.GetComponent<Image>();
			icon.image.raycastTarget = false;
			icon.image.preserveAspect = true;
			return icon;
		}

		public void Set(Sprite sprite, Color tint)
		{
			image.sprite = sprite;
			image.color = tint;
			bool visible = sprite != null;
			gameObject.SetActive(visible);
			Vector4 margin = baseMargin;
			if (visible)
				margin.x += size + Gap;
			label.margin = margin;
			if (visible)
			{
				// Rebuild now so the first frame already uses the new text and margin.
				label.ForceMeshUpdate(true);
				Follow();
			}
		}

		private void LateUpdate()
		{
			Follow();
		}

		private void Follow()
		{
			if (label.textInfo == null || label.textInfo.characterCount == 0)
			{
				image.enabled = false;
				return;
			}
			image.enabled = true;
			// textBounds is in the label's local space, the same space as this child's localPosition.
			Bounds bounds = label.textBounds;
			rect.localPosition = new Vector3(bounds.min.x - Gap - size / 2f, bounds.center.y, 0f);
		}
	}
}
