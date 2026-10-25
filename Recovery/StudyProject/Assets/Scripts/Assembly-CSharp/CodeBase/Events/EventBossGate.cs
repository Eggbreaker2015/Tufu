using BrunoMikoski.AnimationSequencer;
using Codebase.Utilities;
using UnityEngine;
using UnityEngine.AI;

namespace Codebase.Events
{
	public class EventBossGate : MonoBehaviour
	{
		[field: SerializeField]
		public NavMeshObstacle Obstacle { get; private set; }

		[field: SerializeField]
		public AnimationSequencerController OpenAnimation { get; private set; }

		[field: SerializeField]
		public Transform ProgressBarAnchor { get; private set; }

		[field: SerializeField]
		[field: KeysDropdown(typeof(EventKeys))]
		public string EventKey { get; private set; }

		public bool IsOpened { get; private set; }

		public void Open()
		{
		}

		public void Reset()
		{
		}
	}
}
