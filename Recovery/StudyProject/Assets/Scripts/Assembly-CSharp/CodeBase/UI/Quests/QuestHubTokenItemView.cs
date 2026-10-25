using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Quests;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Quests
{
	public class QuestHubTokenItemView : View
	{
		[SerializeField]
		private Image icon;

		[SerializeField]
		private TMP_Text nameText;

		[SerializeField]
		private TMP_Text statusText;

		[SerializeField]
		private Button deliverButton;

		[SerializeField]
		private GameObject deliveredOverlay;

		[SerializeField]
		private GameObject lockedOverlay;

		[Inject]
		private QuestModel questModel;

		private QuestChainDef chain;

		private QuestHubTokenSlot slot;

		protected override void Awake()
		{
		}

		public void Setup(QuestChainDef chain, QuestHubTokenSlot slot)
		{
		}

		protected override void Render()
		{
		}

		private void HandleDeliver()
		{
		}
	}
}
