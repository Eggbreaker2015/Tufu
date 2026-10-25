using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Player;
using Codebase.Unit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Stats
{
	public class StatDescriptionView : PopupView
	{
		[SerializeField]
		private Button closeButton;

		[SerializeField]
		private Image statIcon;

		[SerializeField]
		private TMP_Text statNameText;

		[SerializeField]
		private TMP_Text statDescriptionText;

		[Inject]
		private IconStyleConfig iconStyleConfig;

		private StatType statType;

		protected override void Awake()
		{
		}

		public void Setup(StatType statType)
		{
		}

		protected override void Render()
		{
		}
	}
}
