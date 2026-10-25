using UnityEngine;

public class ECExplodingProjectile : MonoBehaviour
{
	public GameObject impactPrefab;

	public GameObject explosionPrefab;

	public float thrust;

	public Rigidbody thisRigidbody;

	public GameObject particleKillGroup;

	private Collider thisCollider;

	public bool LookRotation;

	public bool Missile;

	public Transform missileTarget;

	public float projectileSpeed;

	public float projectileSpeedMultiplier;

	public bool ignorePrevRotation;

	public bool explodeOnTimer;

	public float explosionTimer;

	private float timer;

	private Vector3 previousPosition;

	private void Start()
	{
	}

	private void Update()
	{
	}

	private void FixedUpdate()
	{
	}

	private void CheckCollision(Vector3 prevPos)
	{
	}

	private void OnCollisionEnter(Collision collision)
	{
	}

	private void Explode()
	{
	}
}
