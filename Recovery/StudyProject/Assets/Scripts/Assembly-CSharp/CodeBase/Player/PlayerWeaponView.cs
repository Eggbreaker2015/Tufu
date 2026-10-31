using System;
using System.Linq;
using System.Reflection;
using Codebase.Core;
using DG.Tweening;
using Codebase.Enemy;
using Codebase.Pet;
using Codebase.Player.Chain;
using Codebase.Player.Inventory;
using Codebase.Player.Models;
using Codebase.Utilities;
using UnityEngine;

namespace Codebase.Player
{
	public class PlayerWeaponView : MonoBehaviour
	{
		private const float PierceAttackHitPoint = 0.33f;
		private const float SmashAttackHitPoint = 0.42f;
		private const float CutAttackHitPoint = 0.45f;
		private const float LongHandCastThreshold = 0.4f;
		private const float HandCastFastHalfDuration = 0.2f;
		[SerializeField]
		[KeysDropdown(typeof(ItemSlotKeys))]
		private string slotKey;
		[SerializeField]
		private Transform bone;
		[SerializeField]
		private Component chainDeformer;
		[SerializeField]
		private SkinnedMeshRenderer tentacle;
		[SerializeField]
		private Animator animator;
		[SerializeField]
		private WeaponSlotSide slotSide = (WeaponSlotSide)1;
		[SerializeField]
		private string attackAnimationKey;
		[SerializeField]
		private string handSpeedKey = "handRSpeed";
		[SerializeField]
		private float attackHitPoint = .4f;
		[SerializeField]
		private float attackEndSpeed = 5;
		[SerializeField]
		private Vector3 viewPositionOffset;
		[SerializeField]
		private Vector3 viewRotationOffset;
		[SerializeField]
		private Vector3 viewScaleOffset = Vector3.one;
		[Header("Chain death/revive")]
		[SerializeField]
		private float fallDuration = .45f;
		[SerializeField]
		private float fallRadius = .6f;
		[SerializeField]
		private float bounceHeight = .2f;
		[SerializeField]
		private float bounceDuration = .18f;
		[SerializeField]
		private float weaponPopDuration = .18f;
		[SerializeField]
		private float chainStaggerMax = .08f;
		[SerializeField]
		private Vector2 stickAngleRange = new Vector2(60, 110);
		[SerializeField]
		private float stickProbability = .5f;
		private static readonly int RevealId = Shader.PropertyToID("_Reveal");
		private static readonly int EdgeProgressId = Shader.PropertyToID("_EdgeProgress");
		private MaterialPropertyBlock chainMpb;
		private Mesh bakedChainMesh;
		private float chainReveal = 1;
		private float chainEdgeProgress;
		private bool deferViewCreation;
		private WeaponView view;
		private Vector3 viewIdleScale = Vector3.one;
		private string currentWeaponKey;
		private ItemSlotModel currentSlot;
		private PlayerModel player;
		private Transform target => bone.transform;
		private bool HasAnimator => animator != null;
		private bool IsChainSlot => !HasAnimator;
		public string SlotKey => slotKey;
		public Animator CharacterAnimator => animator;
		public string AttackAnimationKey => attackAnimationKey;
		public string HandSpeedKey => handSpeedKey;
		public float AttackEndSpeed => attackEndSpeed;

		private float ChainReveal
		{
			get
			{
				return this.chainReveal;
			}

			set
			{
				chainReveal = value;
				tentacle.GetPropertyBlock(chainMpb);
				chainMpb.SetFloat(RevealId, value);
				tentacle.SetPropertyBlock(chainMpb);
			}
		}

		private float ChainEdgeProgress
		{
			get
			{
				return this.chainEdgeProgress;
			}

			set
			{
				chainEdgeProgress = value;
				tentacle.GetPropertyBlock(chainMpb);
				chainMpb.SetFloat(EdgeProgressId, value);
				tentacle.SetPropertyBlock(chainMpb);
			}
		}

		private void Awake()
		{
			// Native 0x14473fc: bind the existing slot; do not manufacture equipment.
			player = GameContext.Get<PlayerModel>();
			currentSlot = player.ItemSlots.FirstOrDefault(slot => slot.Key == slotKey);
			chainMpb = new MaterialPropertyBlock();
			if (!HasAnimator)
				BakeChainDissolveT();
		}

		private void BakeChainDissolveT()
		{
			// Native 0x1447570: project the longest mesh axis into the second UV channel.
			Mesh original = tentacle.sharedMesh;
			Vector3[] vertices = original.vertices;
			if (vertices.Length == 0)
				return;
			Vector3 min = Vector3.positiveInfinity, max = Vector3.negativeInfinity;
			foreach (Vector3 vertex in vertices)
			{
				min = Vector3.Min(min, vertex);
				max = Vector3.Max(max, vertex);
			}

			Vector3 size = max - min;
			int axis = Mathf.Abs(size.x) >= Mathf.Abs(size.y) && Mathf.Abs(size.x) >= Mathf.Abs(size.z) ? 0 : (Mathf.Abs(size.y) >= Mathf.Abs(size.z) ? 1 : 2);
			Mesh copy = Instantiate(original);
			copy.name = original.name + "_Dissolve";
			Vector2[] coordinates = new Vector2[copy.vertexCount];
			for (int i = 0; i < vertices.Length; i++)
				coordinates[i] = new Vector2(Mathf.Clamp01((vertices[i][axis] - min[axis]) / size[axis]), 0);
			copy.uv2 = coordinates;
			tentacle.sharedMesh = copy;
			bakedChainMesh = copy;
		}

		private void Start()
		{
			player.Unit.OnDeathPhaseEnter += HandleDeathPhase;
		}

		private void OnDestroy()
		{
			player.Unit.OnDeathPhaseEnter -= HandleDeathPhase;
			if (bakedChainMesh)
				Destroy(bakedChainMesh);
		}

		private void HandleDeathPhase(DeathPhase phase, float duration)
		{
			return;
		}

		private void HideForDeath(float duration)
		{
			if (!HasAnimator)
			{
				PlayChainDeathSequence(duration);
				return;
			}

			if (view)
			{
				DOTween.Kill(view);
				DOTween.To(() => view.ScaleMultiplier, value => view.ScaleMultiplier = value, Vector3.zero, duration).SetTarget(view);
			}
		}

		private void RestoreFromDeath(float duration)
		{
			if (!HasAnimator)
			{
				PlayChainReviveSequence(duration);
				return;
			}

			if (view)
			{
				DOTween.Kill(view);
				DOTween.To(() => view.ScaleMultiplier, value => view.ScaleMultiplier = value, Vector3.one, duration).SetTarget(view);
			}
		}

		private void PlayChainDeathSequence(float duration)
		{
			PlayerChainDeformer chain = ResolveChainDeformer();
			DOTween.Kill(this);
			deferViewCreation = true;
			chain.SetFrozen(true);
			float stagger = UnityEngine.Random.Range(0f, chainStaggerMax);
			ChainReveal = 1f;
			ChainEdgeProgress = 0f;
			float dissolveDuration = duration * 0.85f;
			DOTween.To(() => ChainReveal, value => ChainReveal = value, 0f, dissolveDuration).SetDelay(stagger).SetEase(Ease.InQuad).SetTarget(this);
			DOTween.To(() => ChainEdgeProgress, value => ChainEdgeProgress = value, 1f, dissolveDuration).SetDelay(stagger).SetEase(Ease.Linear).SetTarget(this);
			WeaponView current = view;
			if (!current)
				return;
			DOTween.Kill(current);
			DOTween.Kill(current.transform);
			current.IsExternallyAnimated = true;
			AnimateWeaponFall(current, stagger);
		}

		private void AnimateWeaponFall(WeaponView v, float stagger)
		{
			v.transform.SetParent(null, true);
			bone.localScale = Vector3.one;
			float groundY = player.Unit.transform.position.y;
			Vector3 position = v.transform.position;
			float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
			float radius = UnityEngine.Random.Range(0.1f, fallRadius);
			Vector3 landing = new Vector3(position.x + Mathf.Cos(angle) * radius, groundY, position.z + Mathf.Sin(angle) * radius);
			bool sticks = UnityEngine.Random.value < stickProbability;
			Sequence sequence = DOTween.Sequence().SetDelay(stagger).SetTarget(v.transform);
			if (sticks)
			{
				Quaternion rotation = Quaternion.Euler(UnityEngine.Random.Range(stickAngleRange.x, stickAngleRange.y), UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(-25f, 25f));
				sequence.Append(v.transform.DOMove(landing, fallDuration).SetEase(Ease.InQuad));
				sequence.Join(v.transform.DORotateQuaternion(rotation, fallDuration).SetEase(Ease.OutQuad));
				sequence.Join(v.transform.DOScale(viewIdleScale, fallDuration).SetEase(Ease.OutQuad));
				sequence.Append(v.transform.DOMoveY(groundY + 0.04f, 0.05f).SetEase(Ease.OutQuad));
				sequence.Append(v.transform.DOMoveY(groundY, 0.05f).SetEase(Ease.InQuad));
			}
			else
			{
				Quaternion rotation = Quaternion.Euler(UnityEngine.Random.Range(70f, 110f), UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(-180f, 180f));
				sequence.Append(v.transform.DOMove(landing, fallDuration).SetEase(Ease.InQuad));
				sequence.Join(v.transform.DORotateQuaternion(rotation, fallDuration).SetEase(Ease.OutCubic));
				sequence.Join(v.transform.DOScale(viewIdleScale, fallDuration).SetEase(Ease.OutQuad));
				sequence.Append(v.transform.DOMoveY(groundY + bounceHeight, bounceDuration * 0.5f).SetEase(Ease.OutQuad));
				sequence.Append(v.transform.DOMoveY(groundY, bounceDuration * 0.5f).SetEase(Ease.InQuad));
			}
		}

		private void VanishForWarp(float duration)
		{
			if (HasAnimator)
				return;
			WeaponView current = view;
			if (!current)
				return;
			DOTween.Kill(current.transform);
			DOTween.Kill(current);
			current.transform.DOScale(Vector3.zero, duration).SetEase(Ease.InQuad).SetTarget(current.transform).OnComplete(() =>
			{
				if (current)
					Destroy(current.gameObject);
				if (view == current)
					view = null;
			});
		}

		private void PlayChainReviveSequence(float duration)
		{
			PlayerChainDeformer chain = ResolveChainDeformer();
			DOTween.Kill(this);
			chain.SetFrozen(false);
			chain.LengthMultiplier = 0f;
			tentacle.enabled = true;
			ChainReveal = 0f;
			ChainEdgeProgress = 0f;
			float stagger = UnityEngine.Random.Range(0f, chainStaggerMax);
			Sequence sequence = DOTween.Sequence().SetDelay(stagger).SetTarget(this);
			sequence.Append(DOTween.To(() => ChainReveal, value => ChainReveal = value, 1f, duration).SetEase(Ease.OutQuad));
			sequence.Join(DOTween.To(() => chain.LengthMultiplier, value => chain.LengthMultiplier = value, 1f, duration).SetEase(Ease.OutQuad));
			sequence.Join(DOTween.To(() => ChainEdgeProgress, value => ChainEdgeProgress = value, 1f, duration).SetEase(Ease.Linear));
			if (!currentSlot.IsEquipped)
			{
				sequence.OnComplete(() => deferViewCreation = false);
				return;
			}

			Vector3 idlePosition = transform.position;
			Vector3 chainStart = chain.GetStartWorldPosition();
			WeaponView current = BuildView();
			current.transform.position = chainStart;
			current.transform.localScale = Vector3.zero;
			current.IsExternallyAnimated = true;
			sequence.Join(current.transform.DOMove(idlePosition, duration).SetEase(Ease.OutCubic));
			sequence.Append(current.transform.DOScale(viewIdleScale, weaponPopDuration).SetEase(Ease.OutBack));
			sequence.OnComplete(() =>
			{
				current.transform.localScale = viewIdleScale;
				current.IsExternallyAnimated = false;
				deferViewCreation = false;
			});
		}

		private WeaponView BuildView()
		{
			// Native 0x14493a8 uses the actual equipped ItemConfig prefab and its native offsets.
			if (view)
				Destroy(view.gameObject);
			WeaponView result = Instantiate(currentSlot.ItemModel.Config.Prefab);
			viewIdleScale = result.transform.localScale;
			result.transform.SetParent(HasAnimator ? bone.transform : null, false);
			result.Setup(new WeaponViewSetupData { Model = currentSlot, Target = bone.transform, ChainDeformer = ResolveChainDeformer(), SlotIndex = currentSlot.ItemModel.SlotIndex, SlotSide = slotSide, PlayerTransform = player.Unit.transform, PlayerModel = player, AnimationKey = attackAnimationKey, Animator = animator, PositionOffset = HasAnimator ? viewPositionOffset : Vector3.zero, RotationOffset = HasAnimator ? viewRotationOffset : Vector3.zero, ScaleOffset = HasAnimator ? viewScaleOffset : Vector3.one, UseWorldIdle = !HasAnimator, IdleReference = transform });
			if (!HasAnimator)
				tentacle.enabled = true;
			currentWeaponKey = currentSlot.ItemModel.Key;
			view = result;
			return result;
		}

		private void Update()
		{
			// Native 0x1449adc, bounded ASM: synchronize equipment and the character hand speed.
			if (currentSlot == null)
				return;
			// Pet mode: weapons are pets, so the weapon view stays hidden.
			{
				HideForPets();
				return;
			}
		}

		private void HideForPets()
		{
			if (view != null)
			{
				Destroy(view.gameObject);
				view = null;
			}
			currentWeaponKey = null;
			if (!HasAnimator && tentacle.enabled)
				tentacle.enabled = false;
		}

		internal static float GetAttackHitPoint(WeaponType damageType)
		{
			switch (damageType)
			{
				case WeaponType.Stabbing:
					return PierceAttackHitPoint;
				case WeaponType.Cutting:
					return CutAttackHitPoint;
				case WeaponType.Buckshot:
					return SmashAttackHitPoint;
				default:
					throw new ArgumentOutOfRangeException(nameof(damageType), damageType, null);
			}
		}

		private PlayerChainDeformer ResolveChainDeformer()
		{
			if (HasAnimator || chainDeformer == null)
				return null;
			if (chainDeformer is PlayerChainDeformer restored)
			{
				restored.SetHiddenRenderer(tentacle);
				return restored;
			}

			Component legacy = chainDeformer;
			if (legacy is Behaviour behaviour)
				behaviour.enabled = false;
			PlayerChainDeformer result = legacy.gameObject.GetComponent<PlayerChainDeformer>();
			if (result == null)
				result = legacy.gameObject.AddComponent<PlayerChainDeformer>();
			result.SetupFromLegacy(GetLegacyBone(legacy, "StartBone"), GetLegacyBone(legacy, "EndBone"));
			chainDeformer = result;
			result.SetHiddenRenderer(tentacle);
			return result;
		}

		private Transform GetLegacyBone(Component legacy, string name)
		{
			const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
			var type = legacy.GetType();
			Transform result = type.GetField(name, flags)?.GetValue(legacy) as Transform;
			return result != null ? result : type.GetProperty(name, flags)?.GetValue(legacy) as Transform;
		}
	}
}
