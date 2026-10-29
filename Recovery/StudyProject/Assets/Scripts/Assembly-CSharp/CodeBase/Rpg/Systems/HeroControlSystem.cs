using Codebase.Core;
using Codebase.Enemy;
using Codebase.Pet;
using Codebase.Player;
using Codebase.Player.Input;
using Codebase.Player.Models;
using Codebase.Rpg.UI;
using Codebase.Skills;
using Codebase.Unit;
using Scellecs.Morpeh;
using UnityEngine;
using UnityEngine.AI;

namespace Codebase.Rpg.Systems
{
	public class HeroControlSystem : SystemBase
	{
		[Inject] private PlayerModel playerModel;
		[Inject] private HeroRosterModel roster;
		[Inject] private SkillService skillService;
		private HeroSwitchHudView hud;
		private EnemyUnit visualTarget;
		private HeroUnit visualOwner;

		public override void OnAwake() { }

		public override void OnUpdate(float deltaTime)
		{
			if (!RpgModeConfig.Enabled) return;
			if (hud == null && GameContext.Has<Joystick>()) hud = HeroSwitchHudView.Create(GameContext.Get<Joystick>(), roster);
			HeroModel request = hud != null ? hud.TakeRequest() : null;
			if (UnityEngine.Input.GetKeyDown(KeyCode.Tab)) request = NextLiving();
			for (int i = 0; i < 3; i++)
				if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1 + i)) request = roster.Heroes.Find(hero => hero.SlotOrder == i);
			PlayerUnit player = playerModel.Unit;
			HeroModel protagonist = roster.Get(player);
			if (player == null || protagonist == null) return;
			if (player.IsDying || player.IsReviving)
			{
				if (roster.Controlled != protagonist) roster.ForceControl(protagonist);
				request = null;
			}
			else if (roster.Controlled != null && !roster.Controlled.IsAlive)
			{
				HeroModel next = protagonist.IsAlive ? protagonist : FirstLiving();
				if (next != null) roster.ForceControl(next);
				request = null;
			}
			if (request != null) roster.TrySwitch(request);
			if (roster.Controlled?.Unit is PetUnit pet) Move(pet, deltaTime);
		}

		public override void Dispose()
		{
			if (hud != null) Object.Destroy(hud.gameObject);
		}

		private HeroModel FirstLiving()
		{
			HeroModel best = null;
			foreach (HeroModel hero in roster.Heroes)
				if (hero.IsAlive && (best == null || hero.SlotOrder < best.SlotOrder)) best = hero;
			return best;
		}

		private HeroModel NextLiving()
		{
			HeroModel first = FirstLiving();
			HeroModel best = null;
			int slot = roster.Controlled != null ? roster.Controlled.SlotOrder : -1;
			foreach (HeroModel hero in roster.Heroes)
				if (hero.IsAlive && hero.SlotOrder > slot && (best == null || hero.SlotOrder < best.SlotOrder)) best = hero;
			return best ?? first;
		}

		private void Move(PetUnit unit, float deltaTime)
		{
			NavMeshAgent agent = unit.Agent;
			if (unit.IsDead || agent == null || !agent.enabled || !agent.isOnNavMesh) return;
			if (FriendlyDebuffs.IsStunned(unit) || FriendlyDebuffs.IsDisplaced(unit) || FriendlyDebuffs.IsChanneling(unit))
			{
				unit.SetWalking(false);
				return;
			}
			Vector2 input = GameContext.Get<Joystick>().GetInput();
			float speed = playerModel.CanMove ? (float)unit.GetStatSpecificValue(StatType.MovementSpeed) * skillService.GetMoveSpeedFactor(unit) : 0f;
			UnityEngine.Camera camera = UnityEngine.Camera.main;
			Vector3 forward = camera != null ? camera.transform.forward : Vector3.forward;
			forward.y = 0f;
			forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
			Vector3 movement = (Quaternion.LookRotation(forward) * new Vector3(input.x, 0f, input.y)) * speed * deltaTime;
			agent.Move(movement);
			if (visualOwner != unit) { visualOwner = unit; visualTarget = null; }
			if (unit.CurrentTarget != null && !unit.CurrentTarget.IsDead) visualTarget = unit.CurrentTarget;
			else
			{
				EnemyUnit nearest = null;
				float nearestSqr = float.MaxValue;
				foreach (EnemyUnit enemy in GameContext.Get<EnemiesModel>().Enemies)
				{
					if (enemy == null || enemy.IsDead) continue;
					float sqr = (enemy.transform.position - unit.transform.position).sqrMagnitude;
					if (sqr < nearestSqr) { nearest = enemy; nearestSqr = sqr; }
				}
				visualTarget = nearest != null && (visualTarget != null ? nearestSqr <= 36f : nearestSqr < 16f) ? nearest : null;
			}
			Vector3 direction = visualTarget != null && !visualTarget.IsDead ? visualTarget.transform.position - unit.transform.position : movement;
			direction.y = 0f;
			if (direction.sqrMagnitude > 0f) unit.transform.rotation = Quaternion.RotateTowards(unit.transform.rotation, Quaternion.LookRotation(direction), 460f * deltaTime);
			unit.SetWalking(playerModel.CanMove && input.sqrMagnitude > 0f);
		}
	}
}
