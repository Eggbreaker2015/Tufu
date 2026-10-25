using Codebase.BossTower.Components;
using Codebase.Core;
using Scellecs.Morpeh;

namespace Codebase.BossTower.Systems
{
	public class BossArenaSystem : SystemBase
	{
		[Inject]
		private Stash<BossArenaComponent> arenaStash;
		[Inject]
		private Stash<BossRaidComponent> raidStash;
		private Filter arenaFilter;
		private Filter raidFilter;
		private string activeArenaKey;
		public override void OnAwake()
		{
			arenaFilter = Filter.With<BossArenaComponent>().Build();
			raidFilter = Filter.With<BossRaidComponent>().With<Codebase.Unit.Components.PlayerTag>().Build();
		}

		public override void OnUpdate(float deltaTime)
		{
			activeArenaKey = null;
			foreach (var entity in raidFilter)
			{
				activeArenaKey = raidStash.Get(entity).ArenaKey;
				break;
			}
		}

		public bool TryGet(string key, out BossArenaComponent arena)
		{
			foreach (var entity in arenaFilter)
			{
				BossArenaComponent data = arenaStash.Get(entity);
				if (data.ArenaKey == key)
				{
					arena = data;
					return true;
				}
			}

			arena = default;
			return false;
		}

		public bool TryGetActive(out BossArenaComponent arena)
		{
			return TryGet(activeArenaKey, out arena);
		}
	}
}
