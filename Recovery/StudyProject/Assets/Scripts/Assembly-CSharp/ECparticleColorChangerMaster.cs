using System;
using UnityEngine;

[ExecuteInEditMode]
public class ECparticleColorChangerMaster : MonoBehaviour
{
	[Serializable]
	public class colorChange
	{
		public string Name;

		public ParticleSystem[] colored_ParticleSystem;

		public Gradient Gradient_custom;
	}

	public float Speed_custom;

	public colorChange[] colorChangeList;

	public bool applyChanges;

	public bool Keep_applyChanges;

	private void Start()
	{
	}

	private void Update()
	{
	}
}
