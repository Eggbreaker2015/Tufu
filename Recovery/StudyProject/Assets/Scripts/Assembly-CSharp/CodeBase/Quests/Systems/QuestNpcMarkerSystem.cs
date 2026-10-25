using Codebase.Core;
using Codebase.Quests.Components;
using Codebase.UI;
using Scellecs.Morpeh;

namespace Codebase.Quests.Systems
{
	public class QuestNpcMarkerSystem : SystemBase
	{
		[Inject]
		private Stash<QuestNpcComponent> npcStash;
		[Inject]
		private QuestModel questModel;
		[Inject]
		private UiModel uiModel;
		private Filter filter;
		public override void OnAwake()
		{ // Original ELF 0x142f064; candidate only.
			filter = Filter.With<QuestNpcComponent>().Build();
		}

		public override void OnUpdate(float deltaTime)
		{ // Original ELF 0x142f118; candidate only.
			foreach (var entity in filter)
			{
				var npc = npcStash.Get(entity);
				var marker = npc.ExclamationMarker;
				var chain = questModel.Popup.GetActiveChainForNpc(npc);
				bool visible = false;
				if (uiModel.IsWorldUiVisible && chain != null && chain.Role != QuestChainRole.Hub)
				{
					string activeStep = questModel.Chain.GetActiveStepKey(chain);
					if (activeStep != null && !questModel.IsStepGiven(activeStep))
						visible = true;
					else
						visible = questModel.Chain.HasUnclaimedReward(chain);
				}

				if (marker.activeSelf != visible)
					marker.SetActive(visible);
			}
		}
	}
}
