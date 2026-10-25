using System;
using UnityEngine;
using UnityEngine.UI;

public class ECprojectileActor : MonoBehaviour
{
	[Serializable]
	public class projectile
	{
		public string name;

		public Rigidbody bombPrefab;

		public GameObject muzzleflare;

		public float min;

		public float max;

		public bool rapidFire;

		public float rapidFireCooldown;

		public bool shotgunBehavior;

		public int shotgunPellets;

		public GameObject shellPrefab;

		public bool hasShells;
	}

	public Transform spawnLocator;

	public Transform spawnLocatorMuzzleFlare;

	public Transform shellLocator;

	public Animator recoilAnimator;

	public Transform[] shotgunLocator;

	public projectile[] bombList;

	private string FauxName;

	public Text UiText;

	public bool UImaster;

	public bool CameraShake;

	public float rapidFireDelay;

	public ECCameraShakeProjectile CameraShakeCaller;

	private float firingTimer;

	public bool firing;

	public int bombType;

	public bool swarmMissileLauncher;

	public bool Torque;

	public float Tor_min;

	public float Tor_max;

	public bool MinorRotate;

	public bool MajorRotate;

	private int seq;

	private void Start()
	{
	}

	private void Update()
	{
	}

	public void Switch(int value)
	{
	}

	public void Fire()
	{
	}

	private void RandomizeRotation()
	{
	}

	private void Major_RandomizeRotation()
	{
	}
}
