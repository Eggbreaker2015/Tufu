using Codebase.Core;
using Codebase.Core.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.VIP
{
	public class VipLocationNotificationView : View
	{
		[SerializeField]
		private Button button;

		[SerializeField]
		private TMP_Text text;

		[Inject]
		private UINavigator navigator;

		public int Level { get; set; }

		protected override void Awake()
		{
		}

		protected override void Render()
		{
		}

		private void OnButtonClick()
		{
		}

		public void Dispose()
		{
		}
	}
}
