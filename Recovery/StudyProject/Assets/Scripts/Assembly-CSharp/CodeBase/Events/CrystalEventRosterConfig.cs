using System.Collections.Generic;
using UnityEngine;

namespace Codebase.Events
{
	[CreateAssetMenu(menuName = "Codebase/Events/Crystal Event Roster Config")]
	public class CrystalEventRosterConfig : ScriptableObject
	{
		public CrystalEventForecastSettings ForecastSettings;

		public List<float> BossMinutes;

		[SerializeField]
		public List<CrystalStageRewards> Stages;

		[SerializeField]
		public List<CrystalMilestoneRewards> Milestones;

		[SerializeField]
		public List<CrystalRosterLeagueDef> Leagues;

		public int StagesCount => BossMinutes != null ? BossMinutes.Count : 0;

		public void FillTemplate()
		{
		}
	}
}
