using System;
using System.Collections.Generic;
using Codebase.Core;
using Codebase.Camera;
using Codebase.Player.Models;
using Codebase.Skills;
using UnityEngine.AI;
using Codebase.Unit;
using UniMob;
using UnityEngine;

namespace Codebase.Rpg
{
	public class HeroRosterModel : Model
	{
		public List<HeroModel> Heroes = new List<HeroModel>();
		public HeroModel Controlled { get; private set; }
		public bool InWipe => GameContext.Get<PlayerModel>().Unit != null && (GameContext.Get<PlayerModel>().Unit.IsDying || GameContext.Get<PlayerModel>().Unit.IsReviving);
		public event Action<HeroModel, HeroModel> ControlledChanged;
		public float LastSwitchTime;
		public float GraceUntil;

		public HeroRosterModel(Lifetime lifetime) : base(lifetime)
		{
		}

		private HeroModel pendingRequest;

		public void Request(HeroModel hero) { pendingRequest = hero; }
		public HeroModel TakeRequest() { HeroModel result = pendingRequest; pendingRequest = null; return result; }
		public HeroModel FirstLiving()
		{
			HeroModel best = null;
			foreach (var hero in Heroes) if (hero.IsAlive && (best == null || hero.SlotOrder < best.SlotOrder)) best = hero;
			return best;
		}

		public void Remove(HeroModel hero)
		{
			bool wasControlled = Controlled == hero;
			if (pendingRequest == hero) pendingRequest = null;
			Heroes.Remove(hero);
			if (wasControlled) ForceControl((InWipe ? null : FirstLiving()) ?? Heroes.Find(candidate => candidate.IsProtagonist));
		}

		public bool TrySwitch(HeroModel next)
		{
			if (next == null || !Heroes.Contains(next) || next.Unit == null || !next.IsAlive || next == Controlled) return false;
			PlayerModel player = GameContext.Get<PlayerModel>();
			if (player.Unit == null || !player.CanMove || InWipe || GameContext.Get<CameraFollowSystem>().IsOverriding || Time.time - LastSwitchTime < RpgModeConfig.Instance.SwitchCooldown) return false;
			Apply(next);
			return true;
		}

		public void ForceControl(HeroModel next)
		{
			if (Controlled == next)
				return;
			Apply(next);
		}

		public HeroModel Get(UnitCore unit)
		{
			return Heroes.Find(hero => hero.Unit == unit);
		}

		private void Apply(HeroModel next)
		{
			HeroModel old = Controlled;
			if (old != null && old.Unit != null)
			{
				old.Unit.IsControlled = false;
				NavMeshAgent oldAgent = old.Unit.Agent;
				if (oldAgent != null && oldAgent.enabled && oldAgent.isOnNavMesh && !FriendlyDebuffs.IsDisplaced(old.Unit)) oldAgent.isStopped = false;
				if (old.IsProtagonist && old.Unit.Agent != null)
				{
					old.Unit.Agent.acceleration = RpgModeConfig.Instance.AiAcceleration;
					old.Unit.Agent.angularSpeed = RpgModeConfig.Instance.AiAngularSpeed;
				}
			}
			Controlled = next;
			if (next != null && next.Unit != null)
			{
				next.Unit.IsControlled = true;
				NavMeshAgent agent = next.Unit.Agent;
				if (agent != null && agent.enabled && agent.isOnNavMesh)
				{
					agent.ResetPath();
					agent.velocity = Vector3.zero;
				}
				next.Unit.SetWalking(false);
			}
			LastSwitchTime = Time.time;
			GraceUntil = Time.time + RpgModeConfig.Instance.SwitchTeleportGrace;
			ControlledChanged?.Invoke(old, next);
		}
	}
}
