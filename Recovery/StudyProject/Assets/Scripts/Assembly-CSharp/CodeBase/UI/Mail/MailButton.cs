using Codebase.BossTower;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Mail;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Mail
{
	public class MailButton : View
	{
		[SerializeField]
		private Button button;

		[SerializeField]
		private GameObject notification;

		[SerializeField]
		private GameObject mailBox;

		[SerializeField]
		private VisibilityAnimator visibilityAnimator;

		[Inject]
		private MailsRepository mailsRepository;

		[Inject]
		private BossTowerModel bossTowerModel;

		protected override void Awake()
		{
            base.Awake();
            global::Codebase.Core.GameContext.Set(this);
            button.onClick.AddListener(Open);
        }

		public void OpenMailbox()
		{
		}

		private void Open()
		{
		}

		protected override void Render()
		{
		}
	}
}
