using System.Collections.Generic;
using Codebase.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.ABTests
{
	public class ABTestDebugView : MonoBehaviour
	{
		private class ABTestButton
		{
			public string testName;

			public Button button;

			public TMP_Text text;

			public List<string> variants;
		}

		[SerializeField]
		private GameObject root;

		[SerializeField]
		private Transform buttonsContainer;

		[SerializeField]
		private Button buttonPrefab;

		[SerializeField]
		private Button toggleButton;

		[Inject]
		private ABTestModel model;

		[Inject]
		private ABTestDataConfig dataConfig;

		private readonly List<ABTestButton> buttons;

		private bool isVisible;

		private void Awake()
		{
		}

		private void Start()
		{
		}

		private void CreateButtons()
		{
		}

		private void WriteLocalAbData()
		{
		}

		private void OnButtonClick(ABTestButton abButton)
		{
		}

		private void SetVariant(string testName, string variant)
		{
		}

		private void UpdateButtons()
		{
		}

		public void Toggle()
		{
		}
	}
}
