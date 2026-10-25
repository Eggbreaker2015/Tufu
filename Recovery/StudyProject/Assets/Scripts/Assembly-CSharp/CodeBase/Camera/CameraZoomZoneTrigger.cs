using Codebase.Core;
using UnityEngine;

namespace Codebase.Camera
{
	[RequireComponent(typeof(BoxCollider))]
	public class CameraZoomZoneTrigger : MonoBehaviour
	{
		[SerializeField]
		private float distanceBonus;

		[Inject]
		private CameraFollowSystem cameraFollowSystem;

		private bool isPlayerInside;

		private void OnTriggerEnter(Collider other)
		{
		}

		private void OnTriggerExit(Collider other)
		{
		}

		private void OnDisable()
		{
		}
	}
}
