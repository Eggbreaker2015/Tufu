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
            // Native 0x1604268.
            base.Awake();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnButtonClick);
            Initialize();
		}

		protected override void Render()
		{
            // Native 0x160432c.
            notification.SetActive(vipModel.DailyChestCooldownRemaining == System.TimeSpan.Zero && vipModel.Level > 0);
            visibilityAnimator.SetVisible(!bossTowerModel.IsFighting);
            vipLevelTxt.text = string.Format("<sprite name=vip_2> {0}", vipModel.Level);
		}

		private void OnButtonClick()
		{
            // Native 0x1604490; bounded tail branch opens the original popup without background.
            audioPlayer.PlaySoundOnce("tap");
            navigator.Open<VipView>("Assets/Prefabs/UI/VIP/VIP.prefab");
		}
	}
}
