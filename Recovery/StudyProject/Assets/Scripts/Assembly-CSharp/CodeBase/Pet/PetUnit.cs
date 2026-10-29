using System.Collections.Generic;
using Codebase.Enemy;
using Codebase.Player.Inventory;
using Codebase.Unit;
using UnityEngine;
using UnityEngine.AI;

namespace Codebase.Pet
{
	// Built at runtime by PetSpawnSystem; must not declare Awake/Start, UnitCore's private ones register the entity.
	public class PetUnit : HeroUnit
	{
		private const string VisualChildName = "Visual";

		private const string HealthBarPointName = "HealthBarPoint";

		private const float DefaultHealthBarHeight = 2.4f;

		// Boss Tower chains and quest markers on the enemy visuals.
		private static readonly string[] AlwaysHiddenChildren = { "Chains", "Signs_root" };

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

		// The cavalry controller has no Spawn trigger.
		private bool hasSpawnTrigger;

		public ItemModel Item { get; private set; }

		public PetDefinition Definition { get; private set; }

		public Animator Animator { get; private set; }

		public void Setup(ItemModel item, PetDefinition definition, NavMeshAgent agent, Transform healthBarPoint, Transform visualSlot)
		{
			Item = item;
			Definition = definition;
			Agent = agent;
			HealthBarPoint = healthBarPoint;
			VisualRoot = visualSlot;
		}

		public void AttachVisual(PetDefinition definition)
		{
			// Only the enemy's Visual child is used; its root would come up as an uninitialized EnemyUnit.
			HealthBarPoint.localPosition = Vector3.up * DefaultHealthBarHeight;
			GameObject enemyPrefab = definition != null ? definition.Prefab : null;
			if (enemyPrefab == null)
				return;
			float scale = definition.VisualScale;
			Transform source = enemyPrefab.transform.Find(VisualChildName);
			if (source == null)
			{
				Debug.LogWarning($"Pet visual: prefab {enemyPrefab.name} has no {VisualChildName} child");
				return;
			}

			Vector3 rootScale = scale > 0f ? Vector3.one * scale : enemyPrefab.transform.localScale;
			VisualRoot.localScale = rootScale;
			Transform visual = Instantiate(source.gameObject, VisualRoot, false).transform;
			visual.name = VisualChildName;
			visual.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
			visual.localScale = Vector3.one;
			Transform sourcePoint = source.Find(HealthBarPointName);
			if (sourcePoint != null)
				HealthBarPoint.localPosition = Vector3.Scale(sourcePoint.localPosition, rootScale);
			HideChildren(visual, definition);
			Animator = FindAnimator(enemyPrefab, source, visual);
			if (Animator != null)
				Animator.applyRootMotion = false;
			hasSpawnTrigger = false;
			if (Animator != null)
				foreach (AnimatorControllerParameter parameter in Animator.parameters)
					if (parameter.nameHash == SpawnId)
						hasSpawnTrigger = true;
			renderers.Clear();
			renderers.AddRange(visual.GetComponentsInChildren<MeshRenderer>(true));
			renderers.AddRange(visual.GetComponentsInChildren<SkinnedMeshRenderer>(true));
		}

		private static void HideChildren(Transform visual, PetDefinition definition)
		{
			foreach (string path in AlwaysHiddenChildren)
				SetChildInactive(visual, path, false);
			if (definition.HiddenChildren == null)
				return;
			foreach (string path in definition.HiddenChildren)
				if (!string.IsNullOrEmpty(path))
					SetChildInactive(visual, path, true);
		}

		private static void SetChildInactive(Transform visual, string path, bool warnIfMissing)
		{
			Transform child = visual.Find(path);
			if (child != null)
				child.gameObject.SetActive(false);
			else if (warnIfMissing)
				Debug.LogWarning($"Pet visual: {visual.name} has no child {path} to hide");
		}

		// The enemy's own animator (same path on the copy), else the first active one with a controller.
		private static Animator FindAnimator(GameObject enemyPrefab, Transform source, Transform visual)
		{
			EnemyUnit enemy = enemyPrefab.GetComponent<EnemyUnit>();
			Animator enemyAnimator = enemy != null ? enemy.Animator : null;
			if (enemyAnimator != null)
			{
				string path = GetRelativePath(enemyAnimator.transform, source);
				if (path != null)
				{
					Transform target = path.Length == 0 ? visual : visual.Find(path);
					Animator animator = target != null ? target.GetComponent<Animator>() : null;
					if (animator != null)
						return animator;
				}
			}
			Animator[] animators = visual.GetComponentsInChildren<Animator>(true);
			foreach (Animator animator in animators)
				if (animator.runtimeAnimatorController != null && IsActiveUnder(animator.transform, visual))
					return animator;
			foreach (Animator animator in animators)
				if (animator.runtimeAnimatorController != null)
					return animator;
			return animators.Length > 0 ? animators[0] : null;
		}

		// Null when child is not under root.
		private static string GetRelativePath(Transform child, Transform root)
		{
			string path = string.Empty;
			for (Transform current = child; current != null; current = current.parent)
			{
				if (current == root)
					return path;
				path = path.Length == 0 ? current.name : current.name + "/" + path;
			}
			return null;
		}

		private static bool IsActiveUnder(Transform child, Transform root)
		{
			for (Transform current = child; current != null && current != root; current = current.parent)
				if (!current.gameObject.activeSelf)
					return false;
			return true;
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

		public override void SetWalking(bool value)
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
			if (Animator != null && hasSpawnTrigger)
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
