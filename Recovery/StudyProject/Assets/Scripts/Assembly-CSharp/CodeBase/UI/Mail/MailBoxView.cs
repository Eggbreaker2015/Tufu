using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CodeBase.Audio;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Mail;
using UniMob.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Mail
{
	public class MailBoxView : View
	{
		private enum Tabs
		{
			Inbox = 0,
			Archive = 1
		}

		[SerializeField]
		private MailItemView viewPrefab;

		[SerializeField]
		private RectTransform content;

		[SerializeField]
		private MailItemContextView contextView;

		[SerializeField]
		private GameObject emptyBoxState;

		[SerializeField]
		private Button tabInbox;

		[SerializeField]
		private Button tabArchive;

		[SerializeField]
		private GameObject historyInfoLabel;

		[Inject]
		private MailsRepository mailsRepository;

		[Inject]
		private IAudioPlayer audioPlayer;

		[CompilerGenerated]
		private Tabs __ActiveTab__BackingField;

		private ViewContainer<MailItemView, MailModel> mailsContainer;

		private ComputedAtom<Tabs> __ActiveTab;

		private Tabs ActiveTab
		{
			[CompilerGenerated]
			get
			{
				return Tabs.Inbox;
			}
			[CompilerGenerated]
			set
			{
			}
		}

		private List<MailModel> Mails => null;

		protected override void Awake()
		{
            base.Awake();
            global::Codebase.Core.GameContext.Set(this);
        }

		private void OnEnable()
		{
		}

		protected override void Start()
		{
		}

		public void Close()
		{
		}

		private void SelectInboxTab()
		{
		}

		private void SelectArchiveTab()
		{
		}

		private void OpenMail(MailModel mail)
		{
		}

		protected override void Render()
		{
		}
	}
}
