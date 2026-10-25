using UnityEngine;

public class EffectCoreProjectile : MonoBehaviour
{
	private float disappearAfterTime;

	private float disappearTimer;

	private bool isActive;

	private Vector3 direction;

	private Vector3 startPosition;

	private float movementSpeed;

	private Vector3 targetPosition;

	private float distanceToTarget;

	private float movementValue;

	private void Start()
	{
	}

	private void Update()
	{
	}

	private void Move()
	{
	}

	private void MoveWithoutTargetHit()
	{
	}

	public void Fire(Vector3 target, Vector3 spawnPosition, Vector3 Direction, float speed)
	{
	}

	private void Explode()
	{
	}

	public bool GetIsActive()
	{
		return this.isActive;
	}
}
