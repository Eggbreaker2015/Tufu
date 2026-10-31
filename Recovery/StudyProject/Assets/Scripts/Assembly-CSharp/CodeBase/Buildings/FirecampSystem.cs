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
			var roster = GameContext.Get<global::Codebase.Rpg.HeroRosterModel>();
			if (roster.InWipe) return;
			var owner = global::Codebase.Rpg.RpgControl.ControlledUnitOrNull(playerModel);
			if (owner == null) return;
			if (Vector3.Distance(owner.transform.position, transform.position) < Radius)
			{
				float yaw = owner.transform.eulerAngles.y;
				foreach (var hero in roster.Heroes) if (hero.IsDown && hero.Unit != null)
				{
					float fraction = PetConfig.Instance != null && PetConfig.Instance.ReviveHealthFraction > 0f ? PetConfig.Instance.ReviveHealthFraction : 0.2f;
					Vector3 pos = owner.transform.position;
					if (hero.IsProtagonist) PetsModel.TrySamplePosition(PetsModel.GetFollowPosition(pos, yaw, hero.SlotOrder), pos, out pos);
					GameContext.Get<global::Codebase.Rpg.HeroDeathService>().Revive(hero, pos, yaw, hero.Unit.GetStatValue(StatType.Health) * fraction);
				}
			}
			foreach (var hero in roster.Heroes)
			{
				if (!hero.IsAlive || Vector3.Distance(hero.Unit.transform.position, transform.position) >= Radius || (double)Time.time <= hero.Unit.LastDamageGet + 4.0) continue;
				double maximum = hero.Unit.GetStatValue(StatType.Health);
				hero.Unit.CurrentHealth = Math.Min(hero.Unit.CurrentHealth + maximum * Regen * Time.deltaTime, maximum);
			}
		}

		private void OnDrawGizmos()
		{
			Gizmos.color = new Color(1f, 235f / 255f, 4f / 255f, 1f);
			Gizmos.DrawWireSphere(transform.position, Radius);
		}
	}
}
