using Codebase.Core;
using Codebase.Core.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.DebugTools
{
	public class ConsoleButton : MonoBehaviour
	{
		[SerializeField]
		private Button button;

		[SerializeField]
		private Image errorIndicator;

		[SerializeField]
		private TMP_Text errorCountText;

		[Inject]
		private UINavigator navigator;

		private int lastErrorCount;

		private void Awake()
		{
			// Original ELF0x1516bcc unconditionally disables this release-build debug button.
			gameObject.SetActive(false);
		}

		private void OnDestroy()
		{
		}

		private void Update()
		{
		}

		private void OnLogsCleared()
		{
		}

		private void UpdateErrorIndicator()
		{
		}

		private void OnButtonClick()
		{
		}
	}
}
