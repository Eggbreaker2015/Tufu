using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CodeBase.Services.Cheats
{
	public class CheatTabButton : MonoBehaviour
	{
		[SerializeField]
		private Button button;

		[SerializeField]
		private TMP_Text text;

		[SerializeField]
		private Image icon;

		[SerializeField]
		private Image background;

		[SerializeField]
		private Color activeColor;

		[SerializeField]
		private Color inactiveColor;

		[SerializeField]
		private Color activeTextColor;

		[SerializeField]
		private Color inactiveTextColor;

		private Action onClick;

		public void Setup(string name, Sprite iconSprite, Action onClick)
		{
		}

		public void SetActive(bool isActive)
		{
		}

		private void HandleClick()
		{
		}
	}
}
