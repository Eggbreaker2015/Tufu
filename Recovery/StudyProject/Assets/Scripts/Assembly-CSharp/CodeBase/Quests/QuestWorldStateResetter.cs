using Codebase.Quests.Components;

namespace Codebase.Quests
{
	public static class QuestWorldStateResetter
	{
		public static void Reset()
		{
		}

		public static void ApplyInitialState(QuestStepWorldComponent world)
		{ // Original ELF 0x142b7b0; candidate only.
			foreach (var item in world.ObjectsToEnable)
				item.SetActive(false);
			foreach (var item in world.ObjectsToDisable)
				item.SetActive(true);
			if (world.HasScriptedSequence)
			{
				QuestScriptedSequenceAnimator.SetBool(world.ScriptedCharacters, QuestScriptedSequenceAnimatorParams.IsStoned, false);
				QuestScriptedSequenceAnimator.SetBool(world.ScriptedCharacters, QuestScriptedSequenceAnimatorParams.IsTalking, false);
				foreach (var renderer in world.StoneTransitionRenderers)
					QuestScriptedSequenceAnimator.SetStoneTransitionProgress(renderer, .3f);
				QuestScriptedSequenceAnimator.SetFloat(world.StoneTransitionAnimator, QuestScriptedSequenceAnimatorParams.Speed, 1f);
			}

			if (world.Cutscene != null)
				world.Cutscene.gameObject.SetActive(false);
			if (world.CamerasRoot != null)
				world.CamerasRoot.SetActive(false);
		}
	}
}
