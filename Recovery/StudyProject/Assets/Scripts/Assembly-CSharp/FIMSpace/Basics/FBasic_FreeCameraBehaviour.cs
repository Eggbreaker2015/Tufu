using UnityEngine;

namespace FIMSpace.Basics
{
	public class FBasic_FreeCameraBehaviour : MonoBehaviour
	{
		[Header("> Hold right mouse button to rotate camera <")]
		[Tooltip("How fast camera should fly")]
		public float SpeedMultiplier;

		[Tooltip("Value of acceleration smoothness")]
		public float AccelerationSmothnessValue;

		[Tooltip("Value of rotation smoothness")]
		public float RotationSmothnessValue;

		public float MouseSensitivity;

		public bool NeedRMB;

		private float turboModeMultiply;

		private Vector3 speeds;

		private float ySpeed;

		private Vector3 rotation;

		private float turbo;

		private void Start()
		{
		}

		private void Update()
		{
		}

		public void FixedUpdate()
		{
		}
	}
}
