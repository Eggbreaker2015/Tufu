using System;
using System.Collections.Generic;
using Codebase.UI.World;
using Codebase.Core;
using Codebase.Enemy;
using Codebase.Player;
using Codebase.Unit.Components;
using Scellecs.Morpeh;
using Scellecs.Morpeh.Providers;
using UnityEngine;
using UnityEngine.AI;

namespace Codebase.Unit
{
	public class UnitCore : MonoBehaviour
	{
		private Stat[] statLookup;

		public Action<UnitCore> OnDied;

		public Action<UnitCore> OnDevoured;

		public List<Stat> Stats { get; set; } = new List<Stat>();

		public HealthBar HealthBar { get; set; }

		[field: SerializeField]
		public Transform HealthBarPoint { get; private set; }

		[field: SerializeField]
		public NavMeshAgent Agent { get; private set; }

		[field: SerializeField]
		public bool DisableAgent { get; private set; }

		[field: SerializeField]
		public Transform VisualRoot { get; private set; }

		public EntityProvider EntityProvider { get; private set; }

		public double CurrentHealth { get; set; }

		private double Damage => statLookup == null ? 0.0 : GetStatValue(StatType.Damage);

		public double LastDamageGet { get; set; }

		public double LastDamageDealt { get; set; }

		public bool IsFar { get; set; }

		public bool VisualRootEnabled { get; set; } = true;

		public bool IsDead => CurrentHealth <= 0.01 || IsDying;

		public bool IsDying { get; set; }

		public bool IsDeathAnimationActive { get; set; }

		public bool IsReviving { get; set; }

		public Vector3 OriginalScale { get; set; }

		private void Awake()
		{
			// Native ELF VA 0x15cb268.
			EntityProvider = GetComponent<EntityProvider>();
			OriginalScale = transform.localScale;
		}

		private void Start()
		{
			// Native ELF VA 0x15cb2f4; use the original Morpeh entity/provider.
			World world = World.Default;
			Entity entity = EntityProvider.Entity;
			world.GetStash<UnitComponent>().Set(entity, new UnitComponent { Core = this });
			world.GetStash<NavMeshAgentComponent>().Set(entity, new NavMeshAgentComponent { Agent = Agent });
			if (Agent.enabled) world.GetStash<NavMeshAgentActiveMarker>().Set(entity);
			world.GetStash<PositionComponent>().Set(entity, new PositionComponent { Position = transform.position });
			world.GetStash<RotationComponent>().Set(entity, default);
			if (this is EnemyUnit) world.GetStash<EnemyTag>().Set(entity);
			if (this is PlayerUnit) world.GetStash<PlayerTag>().Set(entity);
			BuildStatLookup();
		}

		public void BuildStatLookup()
		{
			// Native ELF VA 0x15cb8d4: fixed 36 entries; first duplicate wins.
			statLookup = new Stat[36];
			foreach (Stat stat in Stats)
				if (statLookup[(int)stat.Type] == null) statLookup[(int)stat.Type] = stat;
		}

		public bool TryGetStat(StatType type, out Stat stat)
		{
			// Native ELF VA 0x15cbb48 preserves direct array index behavior.
			stat = statLookup[(int)type];
			return stat != null;
		}

		public double GetStatSpecificValue(StatType type, bool isFast = true)
		{
			// Native ELF VA 0x15cbb94, tail calls verified in bounded ARM64.
			Stat stat = statLookup[(int)type];
			return stat == null ? 0.0 : isFast ? stat.FastSpecificValue : stat.SpecificValue;
		}

		public double GetStatValue(StatType type, bool isFast = true)
		{
			// Native ELF VA 0x15cb14c: regen deliberately uses its specific value.
			Stat stat = statLookup[(int)type];
			if (stat == null) return 0.0;
			if (type == StatType.HealthRegen) return isFast ? stat.FastSpecificValue : stat.SpecificValue;
			return isFast ? stat.FastValue : stat.Value;
		}

		public void SetupHealthBar(HealthBar prefab)
		{
			// Native ELF VA 0x15cbbe0. Generic tail bodies are not method contents.
			DynamicWorldCanvas canvas = GameContext.Get<DynamicWorldCanvas>();
			HealthBar = canvas.CreateNew(prefab);
			Injector.InjectInto(HealthBar);
			HealthBar.Initialize(this);
			canvas.AddTarget(HealthBarPoint, HealthBar.transform as RectTransform);
		}

		public void Dispose()
		{
			// Native ELF VA 0x15cbd40.
			DynamicWorldCanvas canvas = GameContext.Get<DynamicWorldCanvas>();
			canvas.RemoveTarget(HealthBarPoint);
			if (HealthBar != null) canvas.Despawn(HealthBar);
		}

		public virtual void ApplyHitAnimation()
		{
		}

		public bool HasStat(StatType statType)
		{
			// Native ELF VA 0x15cbe30.
			return statLookup[(int)statType] != null;
		}
	}
}
