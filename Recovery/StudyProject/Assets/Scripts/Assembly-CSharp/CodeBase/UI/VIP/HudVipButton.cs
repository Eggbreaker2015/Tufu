using CodeBase.Audio;
using Codebase.BossTower;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.VIP;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.VIP
{
	public class HudVipButton : View
	{
		[SerializeField]
		private Button button;

		[SerializeField]
		private GameObject notification;

		[SerializeField]
		private VisibilityAnimator visibilityAnimator;

		[SerializeField]
		private TMP_Text vipLevelTxt;

		[Inject]
		private VipModel vipModel;

		[Inject]
		private UINavigator navigator;

		[Inject]
		private BossTowerModel bossTowerModel;

		[Inject]
		private IAudioPlayer audioPlayer;

		protected override void Awake()
		{
		}

		protected override void Render()
		{
		}

		private void OnButtonClick()
		{
		}
	}
}
