using Codebase.Core;
using Codebase.Enemy.Components;
using Scellecs.Morpeh;
using Scellecs.Morpeh.Providers;

namespace Codebase.Enemy
{
    public class EnemySpawnerProvider : MonoProvider<EnemySpawnerComponent>
    {
        [Inject] private Stash<EventSpawnerTag> eventSpawnerStash;
        [Inject] private Stash<EventSpawnerInactiveTag> eventSpawnerInactiveStash;
        private void Start()
        {
            var spawner = GetComponent<EnemySpawner>();
            GetData().Spawner = spawner;
            if (spawner.Config.IsEvent && spawner.Config.EventDef != null)
            {
                eventSpawnerStash.Set(Entity, new EventSpawnerTag { EventKey = spawner.Config.EventDef.Key });
                eventSpawnerInactiveStash.Set(Entity);
            }
        }
    }
}
