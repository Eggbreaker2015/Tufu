using UnityEngine;

namespace FIMSpace.FTail
{
	[AddComponentMenu("FImpossible Creations/Hidden/Tail Collision Helper")]
	public class TailCollisionHelper : MonoBehaviour
	{
		public TailAnimator2 ParentTail;

		public Collider TailCollider;

		public Collider2D TailCollider2D;

		public int Index;

		private Transform previousCollision;

		internal Rigidbody RigBody { get; private set; }

		internal Rigidbody2D RigBody2D { get; private set; }

		internal TailCollisionHelper Init(bool addRigidbody = true, float mass = 1f, bool kinematic = false)
		{
			return null;
		}

		private void OnCollisionEnter(Collision collision)
		{
		}

		private void OnCollisionExit(Collision collision)
		{
		}

		private void OnTriggerEnter(Collider other)
		{
		}

		private void OnTriggerExit(Collider other)
		{
		}

		private void OnTriggerEnter2D(Collider2D other)
		{
		}

		private void OnTriggerExit2D(Collider2D other)
		{
		}
	}
}
