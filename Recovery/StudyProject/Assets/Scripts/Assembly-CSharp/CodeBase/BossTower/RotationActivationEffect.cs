using UnityEngine;

namespace Codebase.BossTower
{
	public class RotationActivationEffect : MonoBehaviour
	{
		[SerializeField]
		private Transform targetTransform;

		[SerializeField]
		private Vector3 rotationAxis = Vector3.up;

		[SerializeField]
		private float minDegreesPerSec = 30f;

		[SerializeField]
		private float maxDegreesPerSec = 360f;

		private float remainingTime;

		private float totalDuration;

		private bool isPlaying;

		private Quaternion initialRotation;

		private void Awake()
		{
			// Original ELF VA 0x155d798.
			if (targetTransform == null) targetTransform = transform;
			initialRotation = targetTransform.localRotation;
		}

		public void Play(float duration)
		{
			// Original ELF VA 0x155d83c.
			remainingTime = duration;
			totalDuration = duration;
			isPlaying = true;
			targetTransform.localRotation = initialRotation;
			targetTransform.gameObject.SetActive(true);
		}

		public void Stop()
		{
			// Original ELF VA 0x155d890.
			isPlaying = false;
			targetTransform.localRotation = initialRotation;
			targetTransform.gameObject.SetActive(false);
		}

		private void Update()
		{
			// Original ELF VA 0x155d8dc; maximum-speed rotation continues until Stop.
			if (!isPlaying || targetTransform == null) return;
			remainingTime = Mathf.Max(0f, remainingTime - Time.deltaTime);
			float progress = totalDuration > 0f ? 1f - remainingTime / totalDuration : 1f;
			float speed = Mathf.Lerp(minDegreesPerSec, maxDegreesPerSec, progress * progress * progress);
			targetTransform.Rotate(rotationAxis, speed * Time.deltaTime, Space.Self);
		}
	}
}
