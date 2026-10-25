using Codebase.Amulet;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Amulet
{
	public class SoulView : MonoBehaviour
	{
		[SerializeField]
		private Image iconImage;

		[SerializeField]
		private Image glowImage;

		public RectTransform IconRect => iconImage.rectTransform; // Native 0x13d9e4c.

		public Sprite IconSprite => iconImage.sprite; // Native 0x13d9e64.

		public void Setup(SoulStyle style)
		{
			// Native ELF VA 0x13d9e7c.
			iconImage.sprite = style.Icon;
			glowImage.gameObject.SetActive(style.HasGlow);
			if (style.HasGlow) glowImage.color = style.Color;
		}
	}
}
