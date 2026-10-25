using Codebase.Core;
using Codebase.Map.Components;
using Codebase.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Map
{
	public class MiniMapIcon : MonoBehaviour
	{
		[SerializeField]
		private Image iconImage;

		[SerializeField]
		private Image secondLayerIconImage;

		[SerializeField]
		private GameObject highlightMarker;

		private const float HighlightScaleMultiplier = 1.3f;

		private Image highlightImage;

		private GameObject iconGameObject;

		private Vector2 defaultSizeDelta;

		private Vector3 highlightDefaultScale;

		[Inject]
		private IconStyleConfig iconStyle;

		public RectTransform RectTransform { get; private set; }

		public PointOfInterestComponent PointOfInterest { get; private set; }

		private void Awake()
		{
			RectTransform = transform as RectTransform;
			iconGameObject = gameObject;
			defaultSizeDelta = iconImage.rectTransform.sizeDelta;
			highlightImage = highlightMarker.GetComponent<Image>();
			highlightDefaultScale = highlightMarker.transform.localScale;
		}

		public void SetVisible(bool isVisible)
		{
			if (iconGameObject.activeSelf != isVisible) iconGameObject.SetActive(isVisible);
		}

		public void Setup(ref PointOfInterestComponent poi)
		{
			if (poi.IsDirty) ApplyVisuals(poi);
		}

		public void ForceSetup(PointOfInterestComponent poi)
		{
			ApplyVisuals(poi);
		}

		private void ApplyVisuals(PointOfInterestComponent poi)
		{
			PointOfInterest = poi;
			iconImage.sprite = poi.Sprite;
			iconImage.color = poi.Color;
			iconImage.rectTransform.sizeDelta = defaultSizeDelta * poi.Size;
			if (poi.isHighlighted)
			{
			    highlightMarker.SetActive(true);
			    highlightMarker.transform.localScale = highlightDefaultScale * (HighlightScaleMultiplier + poi.HightlightAdditionalSize);
			    highlightImage.color = poi.highlightColor;
			}
			else if (highlightMarker.activeSelf) highlightMarker.SetActive(false);
			secondLayerIconImage.enabled = poi.RemainingCooldownPercent > 0f;
			if (!(poi.RemainingCooldownPercent > 0f)) return;
			secondLayerIconImage.fillAmount = poi.RemainingCooldownPercent;
			secondLayerIconImage.sprite = iconStyle.MinimapStyle.SpawnerTimer;
			secondLayerIconImage.color = new Color(.9f, .9f, .9f, 1f);
			secondLayerIconImage.rectTransform.sizeDelta = defaultSizeDelta * 1.2f;
			iconImage.color = new Color(poi.Color.r, poi.Color.g, poi.Color.b, .4f);
		}
	}
}
