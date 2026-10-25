using System.Linq;
using Codebase.Core;
using Codebase.Core.Flows;
using Codebase.Enemy;
using Codebase.Events.Models;
using Cysharp.Threading.Tasks;

namespace Codebase.Events.Flows
{
	public class RosterSpawnNextMiniBossFlowRunner : IFlowRunner<RosterSpawnNextMiniBossFlow>, IFlowHandlerBase
	{
		[Inject]
		private EnemiesModel enemiesModel;

		[Inject]
		private EventsRepository eventsRepository;

		public UniTask Execute(RosterSpawnNextMiniBossFlow flow)
		{
			// Original APK ARM64 ELF VA 0x14fb720.
			var model = eventsRepository.CurrentEvent;
			if (model != null && model.Def.Key == flow.EventKey && model.Progress < model.ProgressMax)
			{
			    var spawner = enemiesModel.EventSpawners.FirstOrDefault(item => item.Config.EventDef != null &&
			        item.Config.EventDef.Key == flow.EventKey && item.Config.EventDef.UnitType == EventUnitType.MiniBoss);
			    if (spawner != null) spawner.TrySpawn();
			}
			return UniTask.CompletedTask;
		}
	}
}
