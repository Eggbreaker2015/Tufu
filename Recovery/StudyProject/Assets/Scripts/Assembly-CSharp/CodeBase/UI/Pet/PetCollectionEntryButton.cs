using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Pets
{
	// Inventory 图鉴 button; keeps its label on the CJK fallback font after locale font reapplies.
	public class PetCollectionEntryButton : MonoBehaviour
	{
		private const string LabelText = "图鉴";

		private TMP_Text label;

		private TMP_FontAsset appliedFont;

		public void Setup(Button button, TMP_Text labelText)
		{
			label = labelText;
			if (button != null) button.onClick.AddListener(PetUiBuilder.OpenCollection);
			ApplyLabel();
		}

		private void OnEnable()
		{
			ApplyLabel();
		}

		private void LateUpdate()
		{
			if (label != null && label.font != appliedFont) ApplyLabel();
		}

		private void ApplyLabel()
		{
			if (label == null) return;
			PetStatsText.SetText(label, LabelText);
			appliedFont = label.font;
		}
	}
}
