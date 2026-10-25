using Codebase.Core;
using Scellecs.Morpeh;

namespace Codebase.Quests.Systems
{
	public class QuestPrologueWorldEnsureSystem : SystemBase
	{
		[Inject]
		private QuestModel questModel;

		private bool ensured;

		public override void OnAwake()
		{
		}

		public override void OnUpdate(float deltaTime)
		{ // Original ELF 0x14318d8; candidate only.
			if (ensured)
				return;
			ensured = true;
			questModel.Prologue.EnsurePrologueWorldState();
		}
	}
}
