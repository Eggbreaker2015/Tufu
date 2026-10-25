using System;
using TMPro;
using UniMob;
using UnityEngine;

namespace CodeBase.Services.Cheats
{
	public class CheatInputField : LifetimeMonoBehaviour
	{
		[SerializeField]
		private TMP_Text label;

		[SerializeField]
		private TMP_InputField inputField;

		[SerializeField]
		private TMP_Text formattedValueText;

		private Action<int> OnValueChanged;

		private Action<float> OnFloatValueChanged;

		private bool showsFormattedTime;

		public void Setup(string name, int defaultValue, Action<int> onValueChanged)
		{
		}

		public void Setup(string name, float defaultValue, Action<float> onValueChanged)
		{
		}

		public void SetupTimeOffset(string name, int defaultValue, Action<int> onValueChanged)
		{
		}

		public void SetValueWithoutNotify(int value)
		{
		}

		public void UpdateFormattedValue(float seconds)
		{
		}

		private void OnInputChanged(string value)
		{
		}
	}
}
