using System;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.VIP;
using TMPro;
using UnityEngine;

namespace Codebase.UI.VIP
{
	public class VipCurrencyView : View
	{
		[Inject]
		private VipModel vipModel;

		[Inject]
		private VipConfig vipConfig;

		[SerializeField]
		private TMP_Text title;

		[SerializeField]
		private TMP_Text text;

		private void OnEnable()
		{
            // Native 0x1604598.
            Initialize(); Render();
		}

		protected override void Render()
		{
            // Native 0x16045c0; authored title is intentionally untouched.
            int next = vipConfig.GetRequiredExperience(vipModel.Level + 1);
            text.text = string.Format("{0}<color=#FFCCAF><size=77%>/{1}</color></size>", vipModel.Experience, next);
		}
	}
}
