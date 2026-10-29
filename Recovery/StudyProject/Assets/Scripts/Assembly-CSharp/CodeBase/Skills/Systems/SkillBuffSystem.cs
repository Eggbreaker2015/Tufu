using System.Collections.Generic;
using Codebase.Core;
using Codebase.Skills.Components;
using Codebase.Unit;
using Scellecs.Morpeh;

namespace Codebase.Skills.Systems
{
	// Buff timers and periodic effects; expired buffs take their stat modifiers with them. Stun is written on apply
	// (BuffStunComponent, counted down by UnitBuffSystem). A dead or destroyed holder loses all its buffs.
	public class SkillBuffSystem : SystemBase
	{
		[Inject]
		private SkillService skillService;

		[Inject]
		private Stash<UnitBuffsComponent> buffsStash;

		private Filter filter;

		public override void OnAwake()
		{
			filter = Filter.With<UnitBuffsComponent>().Build();
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
