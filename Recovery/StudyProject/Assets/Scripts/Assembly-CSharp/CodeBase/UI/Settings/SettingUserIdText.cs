using Codebase.Analytics;
using Codebase.Core;
using Codebase.Core.UI;
using TMPro;
using UnityEngine;

namespace Codebase.UI.Settings
{
	public class SettingUserIdText : BaseView
	{
		[Inject]
		private UserModel userModel;

		[SerializeField]
		private TMP_Text text;

		private void OnEnable()
		{
// Original ELF VA 0x163127c: immediate virtual Render, not Initialize.
            Render();
		}

		protected override void Render()
		{
// Original ELF VA 0x1631288.
            text.text = userModel.UserId.Value;
		}
	}
}
