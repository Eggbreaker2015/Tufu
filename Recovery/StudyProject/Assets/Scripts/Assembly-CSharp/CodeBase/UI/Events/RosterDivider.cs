using UnityEngine;

namespace Codebase.UI.Events
{
	public class RosterDivider : MonoBehaviour
	{
		[SerializeField]
		private GameObject PassedAndClaimedState;

		[SerializeField]
		private GameObject PassedNotClaimedState;

		public GameObject Button;

		public void SetState(RosterDividerState rosterDivider)
		{
		}
	}
}
