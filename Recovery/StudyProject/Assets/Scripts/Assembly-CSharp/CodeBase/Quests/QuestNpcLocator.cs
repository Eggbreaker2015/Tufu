using Codebase.Core;
using Codebase.Quests.Components;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.Quests
{
	public static class QuestNpcLocator
	{
		private static Filter filter;
		private static Stash<QuestNpcComponent> npcStash;
		public static Transform FindForChain(QuestChainDef chain)
		{ // Original ELF 0x1429958; candidate only.
			if (chain == null)
				return null;
			if (!string.IsNullOrEmpty(chain.QuestGiverKey))
			{
				var found = FindByGiverKey(chain.QuestGiverKey);
				if (found != null)
					return found;
			}

			return chain.Steps.Count > 0 ? FindTransform(chain.Steps[0].StepKey) : null;
		}

		public static Transform FindByGiverKey(string giverKey)
		{ // Original ELF 0x1429a44; candidate only.
			if (filter == null)
				filter = World.Default.Filter.With<QuestNpcComponent>().Build();
			if (npcStash == null)
				npcStash = World.Default.GetStash<QuestNpcComponent>();
			foreach (var entity in filter)
			{
				var npc = npcStash.Get(entity);
				if (npc.GiverKey == giverKey)
					return npc.Transform;
			}

			return null;
		}

		public static Transform FindTransform(string stepKey)
		{ // Original ELF 0x1429d30; candidate only.
			if (filter == null)
				filter = World.Default.Filter.With<QuestNpcComponent>().Build();
			if (npcStash == null)
				npcStash = World.Default.GetStash<QuestNpcComponent>();
			foreach (var entity in filter)
			{
				var npc = npcStash.Get(entity);
				if (npc.StepKey == stepKey)
					return npc.Transform;
			}

			return null;
		}

		public static Transform FindZeroQuestGiverPlaqueAnchor()
		{ // Original ELF 0x142a018; candidate only.
			var worlds = World.Default.Filter.With<QuestStepWorldComponent>().Build();
			var worldsStash = World.Default.GetStash<QuestStepWorldComponent>();
			foreach (var entity in worlds)
			{
				var world = worldsStash.Get(entity);
				if (world.StepKey != "quest_c0_step_1" || world.ObjectsToEnable == null || world.ObjectsToEnable.Length == 0)
					continue;
				foreach (var item in world.ObjectsToEnable)
					if (item != null && item.activeInHierarchy)
						return item.transform;
			}

			var fallback = FindTransform("quest_c0_step_1");
			if (fallback != null)
			{
				_ = fallback.gameObject.activeInHierarchy;
			}

			return fallback;
		}

		[StaticReset]
		private static void ResetStaticState()
		{ // Original ELF 0x142a384; candidate only.
			filter = null;
			npcStash = null;
		}
	}
}
