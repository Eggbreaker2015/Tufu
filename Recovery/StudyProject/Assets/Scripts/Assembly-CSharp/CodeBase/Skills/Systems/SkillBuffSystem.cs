using System.Collections.Generic;
using Codebase.Core;
using Codebase.Enemy;
using Codebase.Pet;
using Codebase.Player;
using Codebase.Skills.Components;
using Codebase.Unit;
using Codebase.Unit.Components;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.Skills.Systems
{
	// Buff timers and periodic effects; expired buffs take their stat modifiers with them. Stun is written on apply
	// (BuffStunComponent, counted down by UnitBuffSystem). A dead or destroyed holder loses all its buffs.
	// Friendly stuns end on death, revive and teleport; enemy stuns on the hero last at most 1.5 s (SkillService).
	public class SkillBuffSystem : SystemBase
	{
		[Inject]
		private SkillService skillService;

		[Inject]
		private Stash<UnitBuffsComponent> buffsStash;

		[Inject]
		private Stash<UnitComponent> unitStash;

		[Inject]
		private Stash<BuffStunComponent> stunStash;

		private const float TeleportClearDistance = 2f;

		private readonly Dictionary<UnitCore, Vector3> stunnedPositions = new Dictionary<UnitCore, Vector3>();

		private readonly List<UnitCore> staleUnits = new List<UnitCore>();

		private Filter friendlyStunFilter;

		private Filter filter;

		public override void OnAwake()
		{
			filter = Filter.With<UnitBuffsComponent>().Build();
			friendlyStunFilter = Filter.With<UnitComponent>().With<BuffStunComponent>().Build();
		}

		public override void OnUpdate(float deltaTime)
		{
			foreach (Entity entity in filter)
			{
				ref UnitBuffsComponent buffs = ref buffsStash.Get(entity);
				UnitCore owner = buffs.Owner;
				List<BuffInstance> list = buffs.Buffs;
				if (owner == null || owner.IsDead || list == null)
				{
					skillService.ClearBuffs(entity);
					continue;
				}
				for (int i = list.Count - 1; i >= 0; i--)
				{
					if (i >= list.Count)
						continue;
					BuffInstance instance = list[i];
					instance.Remaining -= deltaTime;
					BuffDef def = instance.Def;
					if (def.TickInterval > 0f && def.OnTick != null && def.OnTick.Count > 0)
					{
						instance.TickTimer += deltaTime;
						while (instance.TickTimer >= def.TickInterval)
						{
							instance.TickTimer -= def.TickInterval;
							Tick(owner, instance);
						}
					}
					int index = list.IndexOf(instance);
					if (instance.Remaining <= 0f && index >= 0)
						skillService.RemoveBuffAt(list, index);
				}
				if (list.Count == 0 && buffsStash.Has(entity))
					buffsStash.Remove(entity);
			}
			UpdateFriendlyStuns();
		}

		private void UpdateFriendlyStuns()
		{
			foreach (Entity entity in friendlyStunFilter)
			{
				UnitCore core = unitStash.Get(entity).Core;
				if (core is EnemyUnit)
					continue;
				// A destroyed holder cannot supply its entity to ClearStun.
				if (core == null)
				{
					stunStash.Remove(entity);
					continue;
				}
				if (!(core is PlayerUnit) && !(core is PetUnit))
					continue;
				if (core.IsDead || core.IsDying || core.IsReviving)
				{
					skillService.ClearStun(core);
					continue;
				}
				Vector3 position = core.transform.position;
				position.y = 0f;
				if (stunnedPositions.TryGetValue(core, out Vector3 previous) && (position - previous).sqrMagnitude > TeleportClearDistance * TeleportClearDistance)
				{
					skillService.ClearStun(core);
					continue;
				}
				stunnedPositions[core] = position;
			}
			staleUnits.Clear();
			foreach (var pair in stunnedPositions)
				if (!skillService.IsStunned(pair.Key))
					staleUnits.Add(pair.Key);
			foreach (UnitCore core in staleUnits)
				stunnedPositions.Remove(core);
		}

		private void Tick(UnitCore owner, BuffInstance instance)
		{
			if (owner.IsDead)
				return;
			var context = new SkillCastContext
			{
				Caster = owner,
				CasterEntity = owner.EntityProvider != null ? owner.EntityProvider.Entity : default,
				Instigator = instance.Source != null ? instance.Source : owner,
				PrimaryTarget = owner,
				Point = owner.transform.position,
				Buff = instance.Def,
				Level = 1,
				Power = instance.Power
			};
			SkillService.Execute(instance.Def.OnTick, context);
		}
	}
}
