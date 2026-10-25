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
			return default;
		}
	}
}
