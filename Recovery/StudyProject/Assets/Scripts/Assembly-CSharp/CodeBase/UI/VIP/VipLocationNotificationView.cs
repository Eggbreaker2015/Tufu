using Codebase.Utilities;
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
            // Native 0x1604cc8.
            base.Awake();
            button.onClick.AddListener(OnButtonClick);
		}

		protected override void Render()
		{
            // Native 0x1604d64.
            text.text = LocalizationStrings.Get("VIP_UNLOCK_ISLAND").Replace("{value}", Level.ToString());
		}

		private void OnButtonClick()
		{
            // Native 0x1604e24; final call is SetPreviewLevel (B setter), no purchase or unlock.
            navigator.Open<VipView>("Assets/Prefabs/UI/VIP/VIP.prefab").SetPreviewLevel(Level);
		}

		public void Dispose()
		{
            // Native 0x1604ea0 bounded tail is UnityEventBase.RemoveAllListeners.
            button.onClick.RemoveAllListeners();
		}
	}
}
