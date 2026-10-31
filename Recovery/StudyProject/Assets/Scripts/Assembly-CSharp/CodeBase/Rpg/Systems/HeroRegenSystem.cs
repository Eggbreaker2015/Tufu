using System;
using Codebase.Core;
using Codebase.Unit;
using Scellecs.Morpeh;
namespace Codebase.Rpg.Systems
{
 public class HeroRegenSystem : SystemBase
 {
  [Inject] private HeroRosterModel roster;
  public override void OnAwake() { }
  public override void OnUpdate(float dt)
  {
   foreach (var hero in roster.Heroes) if (hero.IsAlive)
    hero.Unit.CurrentHealth = Math.Min(hero.Unit.GetStatValue(StatType.Health), hero.Unit.CurrentHealth + hero.Unit.GetStatValue(StatType.HealthRegen) * dt);
  }
 }
}