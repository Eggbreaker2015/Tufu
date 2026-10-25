using Codebase.Core.UI;
using UnityEngine;

namespace Codebase.UI.Events
{
	public class RosterEventMilestoneNavButton : View
	{
		[SerializeField]
		private RosterDivider rosterDivider;

		private int index;

		public void Setup(int index)
		{
			this.index = index;
		}

		public void SetupState(RosterDividerState state)
		{
			rosterDivider.SetState(state);
		}
	}
}
