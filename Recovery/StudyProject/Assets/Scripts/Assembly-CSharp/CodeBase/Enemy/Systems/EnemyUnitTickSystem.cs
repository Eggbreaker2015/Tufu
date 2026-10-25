using Codebase.Core;
using Codebase.Unit.Components;
using Codebase.Enemy.Components;
using Scellecs.Morpeh;

namespace Codebase.Enemy.Systems
{
    public class EnemyUnitTickSystem : SystemBase
    {
        [Inject] private Stash<UnitComponent> unitStash;
        private Filter filter;
        public override void OnAwake() => filter = Filter.With<UnitComponent>().With<EnemyTag>().Build();
        public override void OnUpdate(float deltaTime)
        {
            foreach (var entity in filter)
            {
                var unit = (EnemyUnit)unitStash.Get(entity).Core;
                if (unit.isActiveAndEnabled) unit.Tick(deltaTime);
            }
        }
    }
}
