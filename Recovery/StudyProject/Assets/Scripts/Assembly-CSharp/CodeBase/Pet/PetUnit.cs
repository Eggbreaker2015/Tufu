using System.Collections.Generic;
using Codebase.Enemy;
using Codebase.Player.Inventory;
using Codebase.Unit;
using UnityEngine;
using UnityEngine.AI;

namespace Codebase.Pet
{
	// Built at runtime by PetSpawnSystem; must not declare Awake/Start, UnitCore's private ones register the entity.
	public class PetUnit : UnitCore
	{
		private const string VisualChildName = "Visual";

		private const string HealthBarPointName = "HealthBarPoint";

		private const float DefaultHealthBarHeight = 2.4f;

		private static readonly int IsWalkingId = Animator.StringToHash("isWalking");

		private static readonly int IsAttackingId = Animator.StringToHash("isAttacking");

		private static readonly int IsDeadId = Animator.StringToHash("isDead");

		private static readonly int SpawnId = Animator.StringToHash("Spawn");

		// Same matcap flash as EnemyUnit (the visual is a copy of an enemy's).
		private static readonly int MatCapColorId = Shader.PropertyToID("_MatCapColor");

		private const string MatCapKeyword = "TCP2_MATCAP";

		private const float HitFlashDuration = 0.15f;

		private const float HitHue = 119f / 120f;

		private const float HitSaturation = 0.08f;

		private readonly List<Renderer> renderers = new List<Renderer>();

		private float hitTimer;

		public ItemModel Item { get; private set; }

		public PetDefinition Definition { get; private set; }

		public EnemyUnit CurrentTarget { get; set; }

		public Animator Animator { get; private set; }

		public void Setup(ItemModel item, PetDefinition definition, NavMeshAgent agent, Transform healthBarPoint, Transform visualSlot)
		{
			Item = item;
			Definition = definition;
			Agent = agent;
			HealthBarPoint = healthBarPoint;
			VisualRoot = visualSlot;
		}

		public void AttachVisual(GameObject enemyPrefab)
		{
			// Only the enemy's Visual child is used; its root would come up as an uninitialized EnemyUnit.
			HealthBarPoint.localPosition = Vector3.up * DefaultHealthBarHeight;
			if (enemyPrefab == null)
				return;
			Transform source = enemyPrefab.transform.Find(VisualChildName);
			if (source == null)
			{
				Debug.LogWarning($"Pet visual: prefab {enemyPrefab.name} has no {VisualChildName} child");
				return;
			}

			Vector3 rootScale = enemyPrefab.transform.localScale;
			VisualRoot.localScale = rootScale;
			Transform visual = Instantiate(source.gameObject, VisualRoot, false).transform;
			visual.name = VisualChildName;
			visual.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
			visual.localScale = Vector3.one;
			Transform sourcePoint = source.Find(HealthBarPointName);
			if (sourcePoint != null)
				HealthBarPoint.localPosition = Vector3.Scale(sourcePoint.localPosition, rootScale);
			Animator = visual.GetComponentInChildren<Animator>(true);
			if (Animator != null)
				Animator.applyRootMotion = false;
			renderers.Clear();
			renderers.AddRange(visual.GetComponentsInChildren<MeshRenderer>(true));
			renderers.AddRange(visual.GetComponentsInChildren<SkinnedMeshRenderer>(true));
		}

		public override void ApplyHitAnimation()
		{
			hitTimer = HitFlashDuration;
		}

		private void Update()
		{
			if (hitTimer <= 0f)
				return;
			hitTimer = Mathf.Max(0f, hitTimer - Time.deltaTime);
			float amount = Mathf.Clamp01(1f - hitTimer / HitFlashDuration);
			Color color = Color.HSVToRGB(HitHue, HitSaturation, 0.8f - amount * 0.8f, true);
			bool flashing = hitTimer > 0f;
			foreach (Renderer renderer in renderers)
			{
				if (renderer == null)
					continue;
				Material material = renderer.material;
				material.SetColor(MatCapColorId, color);
				if (flashing)
					material.EnableKeyword(MatCapKeyword);
				else
					material.DisableKeyword(MatCapKeyword);
			}
		}

		public void SetWalking(bool value)
		{
			if (Animator != null)
				Animator.SetBool(IsWalkingId, value);
		}

		public void SetAttacking(bool value)
		{
			if (Animator != null)
				Animator.SetBool(IsAttackingId, value);
		}

		// Like EnemyUnit.Tick: the animator runs at attack speed only while the attack bool is set.
		public void SetAttackAnimation(bool attacking, float speed)
		{
			if (Animator == null)
				return;
			Animator.SetBool(IsAttackingId, attacking);
			Animator.speed = attacking ? speed : 1f;
		}

		public void SetDeadAnimation(bool value)
		{
			if (Animator != null)
				Animator.SetBool(IsDeadId, value);
		}

		public void PlaySpawnAnimation()
		{
			if (Animator != null)
				Animator.SetTrigger(SpawnId);
		}

		// Death: the corpse is hidden once the death animation has played; ShowVisual undoes it on revive.
		public void HideVisualAfter(float delay)
		{
			CancelInvoke(nameof(HideVisual));
			Invoke(nameof(HideVisual), delay);
		}

		public void ShowVisual()
		{
			CancelInvoke(nameof(HideVisual));
			if (VisualRoot != null)
				VisualRoot.gameObject.SetActive(true);
		}

		private void HideVisual()
		{
			if (VisualRoot != null)
				VisualRoot.gameObject.SetActive(false);
		}
	}
}
