using System;
using UnityEngine;
using UnityEngine.UI;

public class ECEffectActor : MonoBehaviour
{
	[Serializable]
	public class projectile
	{
		public string name;

		public GameObject bombPrefab;

		public Transform bombPrefabTransform;
	}

	public Transform spawnLocator;

	public projectile[] bombList;

	private string FauxName;

	public Text UiText;

	public int bombType;

	public float min;

	public float max;

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
