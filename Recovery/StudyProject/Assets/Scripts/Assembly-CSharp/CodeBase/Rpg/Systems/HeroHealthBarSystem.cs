using Codebase.Core;
using Codebase.Player;
using Scellecs.Morpeh;
namespace Codebase.Rpg.Systems
{
 public class HeroHealthBarSystem : SystemBase
 {
  [Inject] private HeroRosterModel roster;
  [Inject] private PlayerConfig playerConfig;
  public override void OnAwake() { }
  public override void OnUpdate(float dt)
  {
   foreach (var hero in roster.Heroes)
   {
    if (hero.Unit == null) continue;
    if (hero.IsAlive && hero.Unit.HealthBar == null) hero.Unit.SetupHealthBar(playerConfig.HealthBarPrefab);
    if (hero.Unit.HealthBar != null) hero.Unit.HealthBar.gameObject.SetActive(hero.IsAlive);
   }
  }
 }
}