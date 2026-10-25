using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CodeBase.Services.Cheats
{
	public class CheatButton : MonoBehaviour
	{
		[SerializeField]
		private Button button;

		[SerializeField]
		private TMP_Text text;

		public void Setup(string name, Action action)
		{
		}
	}
}
