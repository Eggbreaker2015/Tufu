using Codebase.Core.UI;
using Codebase.UI.Flows;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI
{
	public class AlertDialogView : PopupView
	{
		[SerializeField]
		private TMP_Text titleText;

		[SerializeField]
		private TMP_Text messageText;

		[SerializeField]
		private Button confirmButton;

		[SerializeField]
		private TMP_Text confirmButtonText;

		[SerializeField]
		private GameObject cancelButtonRoot;

		[SerializeField]
		private Button cancelButton;

		[SerializeField]
		private TMP_Text cancelButtonText;

		private UniTaskCompletionSource<bool> resultSource;

		protected override void Awake()
		{
		}

		public void Setup(OpenDialogFlow flow)
		{
		}

		public UniTask<bool> WaitForResult()
		{
			return default;
		}

		private void Complete(bool result)
		{
		}

		public override void Cleanup()
		{
		}
	}
}
