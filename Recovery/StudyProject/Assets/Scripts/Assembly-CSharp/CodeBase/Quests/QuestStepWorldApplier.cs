using Codebase.Quests.Components;
using Scellecs.Morpeh;

namespace Codebase.Quests
{
	public static class QuestStepWorldApplier
	{
		public static void Apply(string stepKey)
		{ // Original ELF 0x142d028; independent candidate only.
			if (TryGetWorld(stepKey, out var world))
			{
				EnableObjects(world);
				DisableObjects(world);
			}
		}

		public static void Apply(QuestStepWorldComponent world)
		{ // Original ELF 0x142d08c; independent candidate only.
			EnableObjects(world);
			DisableObjects(world);
		}

		public static void ApplyDisable(string stepKey)
		{ // Original ELF 0x142d19c; independent candidate only.
			if (TryGetWorld(stepKey, out var world))
			{
				DisableObjects(world);
			}
		}

		public static void ApplyDisable(QuestStepWorldComponent world)
		{ // Original ELF 0x142d1e8; independent candidate only.
			DisableObjects(world);
		}

		public static void ApplyEnable(string stepKey)
		{ // Original ELF 0x142d214; independent candidate only.
			if (TryGetWorld(stepKey, out var world))
			{
				EnableObjects(world);
			}
		}

		public static void ApplyEnable(QuestStepWorldComponent world)
		{ // Original ELF 0x142d260; independent candidate only.
			EnableObjects(world);
		}

		public static void HideDuringCutscene(string stepKey)
		{ // Original ELF 0x142d28c; independent candidate only.
			if (TryGetWorld(stepKey, out var world))
			{
				HideCutsceneObjects(world);
			}
		}

		public static void HideDuringCutscene(QuestStepWorldComponent world)
		{ // Original ELF 0x142d2d8; independent candidate only.
			HideCutsceneObjects(world);
		}

		public static void ShowAfterCutscene(string stepKey)
		{ // Original ELF 0x142d368; independent candidate only.
			if (TryGetWorld(stepKey, out var world))
			{
				ShowCutsceneObjects(world);
			}
		}

		public static void ShowAfterCutscene(QuestStepWorldComponent world)
		{ // Original ELF 0x142d3b4; independent candidate only.
			ShowCutsceneObjects(world);
		}

		public static bool TryGetWorld(string stepKey, out QuestStepWorldComponent world)
		{ // Original ELF 0x142b4c8; independent candidate only.
			var filter = global::Scellecs.Morpeh.World.Default.Filter.With<QuestStepWorldComponent>().Build();
			var stash = global::Scellecs.Morpeh.World.Default.GetStash<QuestStepWorldComponent>();
			foreach (var entity in filter)
				if (stash.Has(entity))
				{
					var candidate = stash.Get(entity);
					if (candidate.StepKey == stepKey)
					{
						world = candidate;
						return true;
					}
				}

			world = default;
			return false;
		}

		private static void EnableObjects(QuestStepWorldComponent world)
		{ // Original ELF 0x142d0d4; independent candidate only.
			foreach (var item in world.ObjectsToEnable)
				item.SetActive(true);
		}

		private static void DisableObjects(QuestStepWorldComponent world)
		{ // Original ELF 0x142d138; independent candidate only.
			foreach (var item in world.ObjectsToDisable)
				item.SetActive(false);
		}

		private static void HideCutsceneObjects(QuestStepWorldComponent world)
		{ // Original ELF 0x142d304; independent candidate only.
			foreach (var item in world.ObjectsToHideDuringCutscene)
				item.SetActive(false);
		}

		private static void ShowCutsceneObjects(QuestStepWorldComponent world)
		{ // Original ELF 0x142d3e0; independent candidate only.
			foreach (var item in world.ObjectsToHideDuringCutscene)
				item.SetActive(true);
		}
	}
}
