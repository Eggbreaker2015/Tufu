using Codebase.Core.UI;
using TMPro;
using UnityEngine;

namespace Codebase.UI.BossTower
{
	public class BossTowerUpgradeDescriptionView : View
	{
		[SerializeField]
		private TMP_Text descriptionText;

		public void Setup(string description)
		{
			// Native ELF VA 0x13c7d0c.
			descriptionText.text = description;
			UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
		}
	}
}
