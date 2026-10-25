using Codebase.Events.Models;
using Codebase.Simulation.Forecast;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Events
{
	public class RosterLeagueProgressPanel : MonoBehaviour
	{
		[SerializeField]
		private Slider inProgressBossesSlider;

		[SerializeField]
		private GameObject bossesLeagueReachedState;

		[SerializeField]
		private GameObject bossesLeagueProgressState;

		[SerializeField]
		private Image inProgressLeagueIcon;

		[SerializeField]
		private TMP_Text inProgressBossesCountTxt;

		[SerializeField]
		private TMP_Text reachedLeagueNameTxt;

		[SerializeField]
		private TMP_Text reachedLeagueDescriptionText;

		[SerializeField]
		private Image reachedLeagueIcon;

		[SerializeField]
		private Image reachedFrameBg;

		[SerializeField]
		private Image reachedInnerBg;

		public void UpdateProgress(EventModel currentEvent, ForecastEventPlan? eventPlan)
		{
		}
	}
}
