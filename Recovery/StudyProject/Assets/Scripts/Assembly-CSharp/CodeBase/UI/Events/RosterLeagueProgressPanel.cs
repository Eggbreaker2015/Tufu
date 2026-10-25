using Codebase.Utilities;
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
			// Original0x1668dfc: compare the running league with the plan's starting league.
			int planLeague = eventPlan.Value.LeagueIndex;
			int leagueIndex = currentEvent.RosterLeagueIndex;
			var leagues = currentEvent.Def.CrystalRoster.Leagues;
			int lastLeague = leagues.Count - 1;
			var current = leagues[leagueIndex];
			var next = leagueIndex < lastLeague ? leagues[planLeague + 1] : current;
			bossesLeagueProgressState.SetActive(leagueIndex < lastLeague && leagueIndex <= planLeague);
			bossesLeagueReachedState.SetActive(leagueIndex >= lastLeague || leagueIndex > planLeague);
			inProgressLeagueIcon.sprite = next.Icon;
			inProgressBossesCountTxt.text = string.Format("{0}/{1}", currentEvent.Progress, current.BossesToAdvance);
			inProgressBossesSlider.value = (float)currentEvent.Progress / current.BossesToAdvance;
			string leagueName = LocalizationStrings.Get("EVENT_LEAGUE_NAME_" + current.Name);
			reachedInnerBg.color = current.innerBgColor;
			reachedFrameBg.color = current.frameBgColor;
			reachedLeagueIcon.sprite = current.Icon;
			reachedLeagueDescriptionText.text = leagueIndex < lastLeague
				? LocalizationStrings.Get("EVENT_LEAGUE_NEXT_EVENT_STARTS_WITH").Replace("{value}", leagueName)
				: LocalizationStrings.Get("EVENT_LEAGUE_BEST_OF_BEST");
			reachedLeagueDescriptionText.color = current.frameBgColor;
			reachedLeagueNameTxt.text = LocalizationStrings.Get("EVENT_LEAGUE_TITLE_" + current.Name);
		}
	}
}
