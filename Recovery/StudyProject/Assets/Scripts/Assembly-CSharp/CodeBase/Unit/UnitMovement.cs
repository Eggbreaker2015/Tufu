using Codebase.Enemy;
using Codebase.Core;
using Codebase.Player;
using Codebase.Player.Input;
using Codebase.Player.Models;
using UnityEngine;
using UnityEngine.AI;

namespace Codebase.Unit
{
	public class UnitMovement : MonoBehaviour
	{
		private const float VisualTargetEnterRadius = 4f;

		private const float VisualTargetExitRadius = 6f;

		private bool isPlayer;

		[SerializeField]
		private NavMeshAgent agent;

		private UnityEngine.Camera cachedCam;

		private EnemyUnit visualTarget;

		public EnemyUnit VisualTarget => this.visualTarget;

		private void Awake()
		{
			// Native ELF VA 0x15cbf00.
			if (TryGetComponent<PlayerUnit>(out _))
			{
				isPlayer = true;
				cachedCam = UnityEngine.Camera.main;
			}
		}

		private void Update()
		{
			// Native ELF VA 0x15cbf80; ARM64 confirms the vector return values
			// that the decompiler loses and a rotation rate of 460 degrees/sec.
			if (!isPlayer) return;
			PlayerModel player = GameContext.Get<PlayerModel>();
			if (player.Unit.IsDying) return;
			// Stunned: no movement and no turning; CanMove is not used for stuns.
			if (global::Codebase.Skills.FriendlyDebuffs.IsStunned(player.Unit)) return;
			float speed = player.CanMove ? (float)player.Unit.GetStatSpecificValue(StatType.MovementSpeed) : 0f;
			Vector2 input = GameContext.Get<Joystick>().GetInput();
			float deltaTime = Time.deltaTime;
			Vector3 cameraPosition = cachedCam.transform.position;
			Vector3 playerPosition = player.Unit.transform.position;
			float angle = Mathf.Atan2(cameraPosition.z - playerPosition.z, cameraPosition.x - playerPosition.x) * Mathf.Rad2Deg + 90f;
			Vector2 displacement = RotateVector(new Vector2(speed * (input.x * deltaTime), speed * (input.y * deltaTime)), angle);
			Vector3 movement = new Vector3(displacement.x, 0f, displacement.y);
			agent.Move(movement);
			UpdateVisualTarget();
			if (visualTarget != null && !visualTarget.IsDead)
			{
				Vector3 direction = visualTarget.transform.position - transform.position;
				direction.y = 0f;
				if (direction.sqrMagnitude <= 0f) return;
				transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), 460f * Time.deltaTime);
			}
			else if (input.magnitude > 0f)
			{
				transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(movement), 460f * Time.deltaTime);
			}
		}

		private void UpdateVisualTarget()
		{
			// Native ELF VA 0x15cc4fc. The 4/6-unit radii provide hysteresis.
			EnemyUnit current = GameContext.Get<PlayerModel>().Unit.CurrentTarget;
			if (current != null && !current.IsDead)
			{
				visualTarget = current;
				return;
			}
			EnemyUnit nearest = null;
			float nearestDistanceSquared = float.MaxValue;
			foreach (EnemyUnit enemy in GameContext.Get<EnemiesModel>().Enemies)
			{
				if (enemy.IsDead) continue;
				float distanceSquared = (enemy.transform.position - transform.position).sqrMagnitude;
				if (distanceSquared < nearestDistanceSquared)
				{
					nearest = enemy;
					nearestDistanceSquared = distanceSquared;
				}
			}
			if (nearest != null && ((visualTarget != null && nearestDistanceSquared <= 36f) || (visualTarget == null && nearestDistanceSquared < 16f)))
				visualTarget = nearest;
			else
				visualTarget = null;
		}

		public static Vector2 RotateVector(Vector2 v, float degrees)
		{
			// Native ELF VA 0x15cc4a8.
			float radians = degrees * Mathf.Deg2Rad;
			float sine = Mathf.Sin(radians);
			float cosine = Mathf.Cos(radians);
			return new Vector2(v.x * cosine - v.y * sine, v.x * sine + v.y * cosine);
		}
	}
}
