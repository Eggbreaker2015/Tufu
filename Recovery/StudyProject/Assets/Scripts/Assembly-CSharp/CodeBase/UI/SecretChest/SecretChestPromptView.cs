using Codebase.Core;
using Codebase.Core.UI;
using Codebase.SecretChest;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.SecretChest
{
	public class SecretChestPromptView : View
	{
		[SerializeField]
		private Button openButton;

		[SerializeField]
		private GameObject foundLabel;

		[SerializeField]
		private TMP_Text foundText;

		[Inject]
		private SecretChestModel secretChestModel;

		private string chestKey;

		private bool consumed;

		protected override void Awake()
		{
		}

		public void Setup(string chestKey)
		{
		}

		public void Dispose()
		{
		}

		protected override void Render()
		{
		}

		private void HandleOpen()
		{
		}
	}
}
