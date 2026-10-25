using Codebase.Notifications;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Settings
{
	public class SettingNotificationTypeSwitch : MonoBehaviour
	{
		private NotificationsModel model;

		[SerializeField]
		private Sprite offSwitchIcon;

		[SerializeField]
		private Sprite onSwitchIcon;

		[SerializeField]
		private Image switchImage;

		[SerializeField]
		private Button button;

		[SerializeField]
		private string[] keys;

		private bool state;

		private float offset;

		private void Start()
		{
		}

		private void Switch()
		{
		}

		private void Render(bool value)
		{
		}
	}
}
