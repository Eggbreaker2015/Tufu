using TMPro;
using UnityEngine;

namespace Codebase.UI
{
	public class CloudSaveRestartBlockerView : MonoBehaviour
	{
		[SerializeField]
		private GameObject panel;

		[SerializeField]
		private TextMeshProUGUI messageText;

		[SerializeField]
		[TextArea]
		private string message;

		private void Awake()
		{
		}

		public void Show()
		{
		}
	}
}
