using Codebase.Analytics;
using Codebase.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Settings
{
	public class SettingSlider : MonoBehaviour
	{
		[Inject]
		private UserModel userModel;

		[SerializeField]
		private Sprite offIcon;

		[SerializeField]
		private Sprite onIcon;

		[SerializeField]
		private Image image;

		[SerializeField]
		private SettingTarget settingTarget;

		[SerializeField]
		private Slider slider;

		private void Awake()
		{
// Original ELF VA 0x1630944; listener is registered before setting Slider.value.
            slider.onValueChanged.AddListener(OnSliderValueChanged);
            switch (settingTarget)
            {
                case SettingTarget.Sound: slider.value = userModel.SoundVolume.Value; break;
                case SettingTarget.Music: slider.value = userModel.MusicVolume.Value; break;
            }
		}

		private void OnSliderValueChanged(float value)
		{
// Original ELF VA 0x1630a94; FCMP/CSEL GT makes NaN select the off icon.
            switch (settingTarget)
            {
                case SettingTarget.Sound: userModel.SoundVolume.Value = value; break;
                case SettingTarget.Music: userModel.MusicVolume.Value = value; break;
            }
            image.sprite = value > 0f ? onIcon : offIcon;
		}
	}
}
