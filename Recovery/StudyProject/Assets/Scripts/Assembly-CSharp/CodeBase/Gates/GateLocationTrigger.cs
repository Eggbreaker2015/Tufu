using Codebase.Core;
using Codebase.Player.Models;
using UnityEngine;
using Codebase.Player;

namespace Codebase.Gates
{
	public class GateLocationTrigger : MonoBehaviour
	{
		[SerializeField]
		private Gate gate;
		[SerializeField]
		private bool isExit;
		[SerializeField]
		private Collider trigger;
		[SerializeField]
		private int locationNumber;
		[Inject]
		private PlayerModel playerModel;
		private void OnTriggerEnter(Collider other)
		{
			if (other.TryGetComponent<PlayerUnit>(out _))
				playerModel.SetCurrentLocation(locationNumber);
		}

		private void UpdateLocationNumber()
		{
			if (gate != null)
				locationNumber = gate.NextLocationNumber - (isExit ? 1 : 0);
		}

		private void OnDrawGizmos()
		{
		}
	}
}
