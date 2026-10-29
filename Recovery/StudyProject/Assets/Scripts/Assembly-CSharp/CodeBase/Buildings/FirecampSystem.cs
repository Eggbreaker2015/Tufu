using System;
using Codebase.Core;
using Codebase.Pet;
using Codebase.Player;
using Codebase.Unit;
using Codebase.Player.Models;
using UnityEngine;

namespace Codebase.Buildings
{
	public class FirecampSystem : MonoBehaviour
	{
		[SerializeField]
		private float Radius;

		[SerializeField]
		[Range(0f, 1f)]
		private float Regen;

		[SerializeField]
		private float JumpOutRadius = 2.5f;

		[SerializeField]
		private ParticleSystem jumpOutEffect;

		[SerializeField]
		private ParticleSystem underFirecampEffect;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private PetsModel petsModel;

		public ParticleSystem JumpOutEffect => this.jumpOutEffect;

		public ParticleSystem UnderFirecampEffect => this.underFirecampEffect;

		public Vector3 GetRandomJumpOutPoint()
		{
			float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
			return transform.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * JumpOutRadius;
		}

		private void Awake()
		{
			GameContext.Set(this);
		}

		private void Update()
		{
			var unit = playerModel.Unit;
			if (unit.IsDying) return;
			if (PetConfig.Enabled) UpdatePets(unit);
			if (Vector3.Distance(unit.transform.position, transform.position) >= Radius) return;
			if ((double)Time.time <= unit.LastDamageGet + 4.0) return;
			double maximum = unit.GetStatValue(StatType.Health);
			unit.CurrentHealth = Math.Min(unit.CurrentHealth + maximum * Regen * Time.deltaTime, maximum);
		}

		// Pet mode: the player at the fountain revives dead pets; living pets inside it heal like the player.
		private void UpdatePets(PlayerUnit unit)
		{
			Vector3 center = transform.position;
			Transform owner = global::Codebase.Rpg.RpgControl.ControlledUnit(playerModel).transform;
			if (Vector3.Distance(owner.position, center) < Radius)
				petsModel.ReviveDead(owner.position, owner.eulerAngles.y);
			foreach (PetModel pet in petsModel.Pets)
			{
				if (!pet.IsAlive) continue;
				PetUnit petUnit = pet.Unit;
				if (petUnit.IsDead) continue;
				if (Vector3.Distance(petUnit.transform.position, center) >= Radius) continue;
				if ((double)Time.time <= petUnit.LastDamageGet + 4.0) continue;
				double maximum = petUnit.GetStatValue(StatType.Health);
				petUnit.CurrentHealth = Math.Min(petUnit.CurrentHealth + maximum * Regen * Time.deltaTime, maximum);
			}
		}

		private void OnDrawGizmos()
		{
			Gizmos.color = new Color(1f, 235f / 255f, 4f / 255f, 1f);
			Gizmos.DrawWireSphere(transform.position, Radius);
		}
	}
}
