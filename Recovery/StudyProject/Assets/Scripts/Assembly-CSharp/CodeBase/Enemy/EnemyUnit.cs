using System.Collections.Generic;
using Codebase.Player.Inventory;
using Codebase.BossTower.Components;
using Codebase.UI.World;
using Codebase.Unit;
using UniMob;
using UnityEngine;
using System.Linq;
using Scellecs.Morpeh;
using Codebase.Unit.Components;
using Codebase.Core;
using CodeBase.Audio;

namespace Codebase.Enemy
{
	public class EnemyUnit : UnitCore
	{
		private static readonly int IsWalking = Animator.StringToHash("isWalking");
		private static readonly int IsAttacking = Animator.StringToHash("isAttacking");
		private static readonly int DragonIsWalking = Animator.StringToHash("Walk Forward");
		private static readonly int DragonIsAttacking = Animator.StringToHash("Bite Attack");
		private static readonly int Dead = Animator.StringToHash("isDead");
		private static readonly int MatCapColorId = Shader.PropertyToID("_MatCapColor");
		[SerializeField]
		public HealthBar healthBarPrefab;
		[SerializeField]
		private Animator animator;
		[SerializeField]
		private string deathSoundKey;
		[SerializeField]
		private string disappearSoundKey;
		[SerializeField]
		private string spawnSoundKey;
		[SerializeField]
		private bool disableAnimations;
		[SerializeField]
		private bool isDragon;
		[SerializeField]
		private List<MeshRenderer> meshRenderers;
		[SerializeField]
		private List<SkinnedMeshRenderer> skinnedMeshRenderers;
		[SerializeField]
		public float DevourScaleMultiplier = 1f;
		private EnemySpawnerConfig config;
		public Vector3? LastDestination;
		private float patrolTimer;
		private Vector3 patrolCenter;
		private const float HitFlashDuration = 0.15f;
		private const float HitHue = 119f / 120f;
		private const float HitSaturation = 0.08f;
		private float hitTimer;
		private bool hasAnimator;
		private bool isStunned;
		private bool wasAnimatorEnabled;
		public IReadOnlyList<SkinnedMeshRenderer> SkinnedMeshRenderers
		{
			get
			{
				return skinnedMeshRenderers;
			}
		}

		public Animator Animator => this.animator;
		public EnemySpawnerConfig Config => this.config;
		public EventSpawnerVariant EventUnitReward { get; set; }

		public ArmorConfig ArmorConfig
		{
			get
			{
				return config.ArmorConfig;
			}
		}

		public bool IsStatic
		{
			get
			{
				return config.IsStatic;
			}
		}

		public bool ImmuneToDisplacement
		{
			get
			{
				World world = World.Default;
				bool valid = world != null && EntityProvider != null && !world.IsDisposed(EntityProvider.Entity);
				bool minion = valid && world.GetStash<BossMinionUnitMarker>().Has(EntityProvider.Entity);
				bool arenaBoss = valid && world.GetStash<BossArenaUnitTag>().Has(EntityProvider.Entity) && !minion;
				// Scaled agent radius above 1.2 m is a large body, independent of visual scale.
				return config != null && (config.IsStatic || config.IsDummy || config.IsEliteBoss || config.IsBossFightPart && !minion) ||
					arenaBoss || Agent != null && Agent.radius * Mathf.Abs(transform.lossyScale.x) > 1.2f;
			}
		}

		public bool IsFlying
		{
			get
			{
				return config.IsFlying;
			}
		}

		// Ranged enemies (skill loadout RangedAttack) stay engaged out to their shooting distance.
		public float AggroRange
		{
			get
			{
				return Ranged != null ? Mathf.Max(config.AggroRange, Ranged.AggroRange) : config.AggroRange;
			}
		}

		public float LeashDistance => Mathf.Max(18f, AggroRange * 2.5f) + Mathf.Max(config.SpawnArea.x, config.SpawnArea.y) * 0.5f;

		public float AggroDuration
		{
			get
			{
				return config.AggroDuration;
			}
		}

		public WeaponType DamageType
		{
			get
			{
				return config.WeaponType;
			}
		}

		public float AggroTimer { get; set; }

		public UnitCore RpgLastAttacker { get; set; }
		public float RpgLastAttackTime { get; set; }
		// Pet mode: the unit (player or pet) this enemy is fighting; set by EnemyAttackSystem.
		public UnitCore AggroTarget { get; set; }
		public float RetreatTimer { get; set; }
		public bool InCombat { get; set; }
		public bool IsReturningHome { get; set; }
		public float AttackCooldown { get; set; }
		public double ReflectionDamage { get; set; }
		public bool IsCasting { get; set; }
		// Skills: ranged basic attack from the skill loadout (null = melee); set on every spawn by SkillService.InstallForEnemy.
		public global::Codebase.Skills.RangedAttackDef Ranged { get; private set; }
		// EnemyAttackSystem ranged state: holding at range, step-back timers and the cached line-of-fire result.
		public bool RangedHolding { get; set; }
		public float RangedKiteTimer { get; set; }
		public float RangedKiteCooldown { get; set; }
		public float LineOfFireTimer { get; set; }
		public bool HasLineOfFire { get; set; }
		public UnitCore LineOfFireTarget { get; set; }
		public Vector3 CurrentSpawnerPatrolPoint { get; private set; }

		public Vector3 PatrolCenter
		{
			get
			{
				return patrolCenter;
			}
		}

		public int RpgLevel { get; set; }
		public float EventDamageRampTimer { get; private set; }
		public ItemConfig ItemReward { get; private set; }
		public List<StatModifier> StatRewards { get; private set; }
		public string SoulReward { get; private set; }
		public int SpawnerStatRewardMultiplier { get; set; }
		public bool HasSoul { get; set; }
		public bool HasEmptyAmulet { get; set; }
		public Vector3? LastWanderDestination { get; set; }
		public double PreviousHealth { get; set; }
		public double SpecialAdditionalCritChancePercent { get; set; }

		public void Initialize(Lifetime lifetime, EnemySpawnerConfig config, Vector3 center, List<Stat> stats = null)
		{
			if (global::Codebase.Rpg.RpgModeConfig.Enabled) RpgLevel = 0;
			this.config = config;
			patrolCenter = center;
			EventDamageRampTimer = 0f;
			ItemReward = config.ItemReward;
			SpawnerStatRewardMultiplier = 1;
			StatRewards = (config.StatRewards ?? new List<StatModifier>()).Select(modifier => new StatModifier { Name = modifier.Name, StatType = modifier.StatType, ModifierType = modifier.ModifierType, Source = modifier.Source, Value = modifier.Value, Duration = modifier.Duration }).ToList();
			SoulReward = config.SoulReward;
			Stats = new List<Stat>();
			foreach (Stat stat in stats ?? config.Stats)
				Stats.Add(new Stat(lifetime, stat.BaseValue, stat.Type));
			CurrentHealth = Stats.First(stat => stat.Type == StatType.Health).Value;
			AggroTimer = 0f;
			if (global::Codebase.Rpg.RpgModeConfig.Enabled) { RpgLastAttacker = null; RpgLastAttackTime = 0f; AggroTarget = null; }
			InCombat = false;
			IsReturningHome = false;
			AttackCooldown = 0f;
			Agent.speed = (float)Stats.First(stat => stat.Type == StatType.MovementSpeed).BaseValue;
			VisualRoot.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
			VisualRoot.localScale = Vector3.one;
			SetDeathState(false);
			ApplyStunAnimation(false);
			UpdatePatrolPoint();
			World world = World.Default;
			if (config.IsFlying)
			{
				Agent.enabled = false;
				world.GetStash<NavMeshAgentActiveMarker>().Remove(EntityProvider.Entity);
			}
			else if (config.IsStatic && !DisableAgent)
			{
				Agent.enabled = true;
				Agent.avoidancePriority = 0;
				if (Agent.isOnNavMesh)
					Agent.isStopped = true;
				world.GetStash<NavMeshAgentActiveMarker>().Set(EntityProvider.Entity);
			}

			world.GetStash<PositionComponent>().Set(EntityProvider.Entity, new PositionComponent { Position = transform.position });
			world.GetStash<UnitSpawnPositionComponent>().Set(EntityProvider.Entity, new UnitSpawnPositionComponent { Position = patrolCenter });
			BuildStatLookup();
			hasAnimator = animator != null;
			if (hasAnimator)
				animator.applyRootMotion = false;
			HasSoul = false;
			HasEmptyAmulet = false;
			// Skills: every spawn path ends here; also resets the previous pooled life's cast and buffs.
			if (GameContext.Has<global::Codebase.Skills.SkillService>())
				GameContext.Get<global::Codebase.Skills.SkillService>().InstallForEnemy(this, config);
			if (global::Codebase.Rpg.RpgModeConfig.Enabled) global::Codebase.Rpg.RpgEnemyScaling.Apply(this, global::Codebase.Rpg.RpgEnemyLevel.Compute(config), lifetime);
		}

		private void OnDisable()
		{
			InCombat = false;
			IsReturningHome = false;
			if (World.Default != null && GameContext.Has<global::Codebase.Skills.SkillService>())
			{
				GameContext.Get<global::Codebase.Skills.SkillService>().ClearForcedMovement(this);
				GameContext.Get<global::Codebase.Skills.SkillService>().CancelCast(this);
			}
		}

		public void ResetRangedState(global::Codebase.Skills.RangedAttackDef ranged)
		{
			Ranged = ranged;
			RangedHolding = false;
			RangedKiteTimer = 0f;
			RangedKiteCooldown = 0f;
			LineOfFireTimer = 0f;
			HasLineOfFire = true;
			LineOfFireTarget = null;
		}

		public void Tick(float deltaTime)
		{
			EventDamageRampTimer = config.IsEvent && CurrentHealth < GetStatValue(StatType.Health) - 0.01 ? EventDamageRampTimer + deltaTime : 0f;
			if (hasAnimator)
			{
				bool animatorEnabled = !IsFar;
				if (wasAnimatorEnabled != animatorEnabled)
				{
					wasAnimatorEnabled = animatorEnabled;
					animator.enabled = animatorEnabled;
				}

				if (!disableAnimations && !IsFar && !IsCasting)
				{
					Vector3 velocity = config.IsFlying ? Vector3.one * 3f : Agent.velocity;
					float speed = GetAttackAnimationSpeed();
					float threshold = 0.5f / speed;
					animator.speed = isStunned ? 0f : AttackCooldown > threshold ? speed : 1f;
					if (isDragon)
					{
						animator.SetBool(DragonIsWalking, velocity.magnitude > 1f);
						if (AttackCooldown > threshold)
							animator.SetTrigger(DragonIsAttacking);
					}
					else
					{
						animator.SetBool(IsWalking, velocity.magnitude > 1f);
						animator.SetBool(IsAttacking, AttackCooldown > threshold);
					}
				}
			}

			if (hitTimer > 0f)
			{
				hitTimer = Mathf.Max(0f, hitTimer - deltaTime);
				float amount = Mathf.Clamp01(1f - hitTimer / HitFlashDuration);
				Color color = Color.HSVToRGB(HitHue, HitSaturation, 0.8f - amount * 0.8f, true);
				ApplyMatcap(color, hitTimer > 0f);
			}

			if (patrolTimer > 0f)
			{
				patrolTimer -= deltaTime;
				return;
			}

			UpdatePatrolPoint();
		}

		private void UpdatePatrolPoint()
		{
			patrolTimer = UnityEngine.Random.Range(1, 8);
			float x = config.SpawnArea.x * UnityEngine.Random.Range(-0.5f, 0.5f);
			float z = config.SpawnArea.y * UnityEngine.Random.Range(-0.5f, 0.5f);
			CurrentSpawnerPatrolPoint = patrolCenter + Quaternion.Euler(0f, config.SpawnAreaRotation, 0f) * new Vector3(x, 0f, z);
		}

		// Skill casts without their own animation: stand idle instead of looping walk/attack while Tick skips the animator.
		public void StopLocomotionAnimation()
		{
			if (!hasAnimator || disableAnimations)
				return;
			animator.speed = isStunned ? 0f : 1f;
			if (isDragon)
			{
				animator.SetBool(DragonIsWalking, false);
				animator.ResetTrigger(DragonIsAttacking);
			}
			else
			{
				animator.SetBool(IsWalking, false);
				animator.SetBool(IsAttacking, false);
			}
		}

		public void ApplyStunAnimation(bool isStunned)
		{
			this.isStunned = isStunned;
			if (!hasAnimator)
				return;
			animator.speed = isStunned ? 0f : 1f;
		}

		public override void ApplyHitAnimation()
		{
			hitTimer = HitFlashDuration;
		}

		public void PlayDeathAudio()
		{
			if (string.IsNullOrEmpty(deathSoundKey))
				return;
			GameContext.Get<IAudioPlayer>().PlayAtPosition(deathSoundKey, transform.position);
		}

		public void PlayDisappearAudio()
		{
			if (string.IsNullOrEmpty(disappearSoundKey))
				return;
			GameContext.Get<IAudioPlayer>().PlayAtPosition(disappearSoundKey, transform.position);
		}

		public void PlaySpawnAudio()
		{
			if (string.IsNullOrEmpty(spawnSoundKey))
				return;
			GameContext.Get<IAudioPlayer>().PlayAtPosition(spawnSoundKey, transform.position);
		}

		public void SetDeathState(bool isDead)
		{
			if (isDead)
			{
				InCombat = false;
				IsReturningHome = false;
			}
			if (!hasAnimator)
				return;
			if (Agent != null && Agent.enabled && Agent.isOnNavMesh && !Agent.isStopped)
			{
				Agent.isStopped = true;
				Agent.updateRotation = false;
			}

			animator.SetBool(Dead, isDead);
		}

		public void SetMaterial(Material material)
		{
			foreach (MeshRenderer renderer in meshRenderers)
				renderer.material = material;
			foreach (SkinnedMeshRenderer renderer in skinnedMeshRenderers)
				renderer.material = material;
		}

		public Material GetOriginalMaterial()
		{
			if (meshRenderers.Count > 0)
				return meshRenderers[0].material;
			if (skinnedMeshRenderers.Count > 0)
				return skinnedMeshRenderers[0].material;
			return null;
		}

		public void UpdateMeshRenderers()
		{
			meshRenderers = GetComponentsInChildren<MeshRenderer>().ToList();
			skinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>().ToList();
		}

		private float GetAttackAnimationSpeed()
		{
			return (float)(1.0 / GetStatValue(StatType.AttackCooldown));
		}

		private void ApplyMatcap(Color color, bool enabled)
		{
			foreach (MeshRenderer renderer in meshRenderers)
				ApplyToRenderer(renderer, color, enabled);
			foreach (SkinnedMeshRenderer renderer in skinnedMeshRenderers)
				ApplyToRenderer(renderer, color, enabled);
		}

		private void ApplyToRenderer(Renderer renderer, Color color, bool enabled)
		{
			Material material = renderer.material;
			material.SetColor(MatCapColorId, color);
			if (enabled)
				material.EnableKeyword("TCP2_MATCAP");
			else
				material.DisableKeyword("TCP2_MATCAP");
		}
	}
}
