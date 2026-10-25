using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Mail;
using Codebase.Services.Time;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Mail
{
	public class MailItemView : View
	{
		[SerializeField]
		private Button button;

		[SerializeField]
		private GameObject notification;

		[SerializeField]
		private TMP_Text titleField;

		[SerializeField]
		private TMP_Text descriptionField;

		[SerializeField]
		private TMP_Text dateField;

		[Inject]
		private ITimeService timeService;

		private MailModel model;

		public Button Button => this.button;

		public void Setup(MailModel model)
		{
		}

		protected override void Render()
		{
		}
	}
}
