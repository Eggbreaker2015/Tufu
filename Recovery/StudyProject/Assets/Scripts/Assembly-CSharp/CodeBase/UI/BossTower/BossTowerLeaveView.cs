using Codebase.BossTower.Systems;
using Codebase.Core;
using Codebase.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.BossTower
{
	public class BossTowerLeaveView : PopupView
	{
		[SerializeField]
		private Button leaveButton;

		[SerializeField]
		private Button stayButton;

		[SerializeField]
		private Button closeButton;

		[Inject]
		private BossRaidSystem bossRaidSystem;

		protected override void Awake()
		{
			// Native ELF 0x13c723c.
			base.Awake(); leaveButton.onClick.AddListener(OnLeave); stayButton.onClick.AddListener(Close); closeButton.onClick.AddListener(Close);
		}

		private void OnLeave()
		{
			// Native ELF 0x13c736c.
			bossRaidSystem.ForcePlayerLeaveArena(); Close();
		}
	}
}
