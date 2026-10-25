using Codebase.Analytics;
using Codebase.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Settings
{
	public class SettingSwitch : MonoBehaviour
	{
		[Inject]
		private UserModel userModel;

		[SerializeField]
		private Sprite offIcon;

		[SerializeField]
		private Sprite onIcon;

		[SerializeField]
		private Sprite offSwitchIcon;

		[SerializeField]
		private Sprite onSwitchIcon;

		[SerializeField]
		private Image image;

		[SerializeField]
		private Image switchImage;

		[SerializeField]
		private SettingTarget settingTarget;

		[SerializeField]
		private Button button;

		private bool state;

		private float offset;

		private void Awake()
		{
// Original ELF VA 0x1630b74; only vibration/quality use this switch.
            offset = switchImage.GetComponent<RectTransform>().anchoredPosition.x;
            button.onClick.AddListener(Switch);
            switch (settingTarget)
            {
                case SettingTarget.Vibration: state = userModel.HapticsEnabled.Value; break;
                case SettingTarget.HighQuality: state = userModel.HighQualityEnabled.Value; break;
                default: state = false; break;
            }
            OnValueChanged(state);
		}

		private void Switch()
		{
// Original ELF VA 0x1630e50 tail-calls OnValueChanged.
            state = !state;
            OnValueChanged(state);
		}

		private void OnValueChanged(bool value)
		{
// Original ELF VA 0x1630ce4; preserve Y, mirror original captured X.
            switch (settingTarget)
            {
                case SettingTarget.Vibration: userModel.HapticsEnabled.Value = value; break;
                case SettingTarget.HighQuality: userModel.HighQualityEnabled.Value = value; break;
            }
            image.sprite = value ? onIcon : offIcon;
            switchImage.sprite = value ? onSwitchIcon : offSwitchIcon;
            var rect = switchImage.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(value ? offset : -offset, rect.anchoredPosition.y);
		}
	}
}
