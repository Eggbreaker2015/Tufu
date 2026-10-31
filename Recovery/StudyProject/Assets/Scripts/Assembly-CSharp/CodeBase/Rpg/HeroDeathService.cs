using Codebase.Core;
using Codebase.Pet;
using Codebase.Player;
using Codebase.Player.Models;
using Codebase.Skills;
using Codebase.Unit.Systems;
using UnityEngine;

namespace Codebase.Rpg
{
 public class HeroDeathService
 {
  public void OnHealthDepleted(HeroModel hero, string killer)
  {
   var roster = GameContext.Get<HeroRosterModel>();
   if (hero == null || hero.IsDown || roster.InWipe) return;
   hero.IsDown = true;
   var config = PetConfig.Instance;
   hero.ReviveAt = Time.time + (config != null && config.ReviveCooldown > 0f ? config.ReviveCooldown : 15f);
   bool anyAlive = roster.Heroes.Exists(h => h.IsAlive);
   if (hero.Pet != null) { GameContext.Get<PetsModel>().OnPetDied(hero.Pet); hero.Pet.ReviveAt = hero.ReviveAt; }
   else if (hero.Unit is PlayerUnit player && anyAlive) player.EnterDown();
   if (GameContext.Has<SkillService>()) GameContext.Get<SkillService>().CancelCast(hero.Unit);
   if (!anyAlive) Wipe(killer);
   else if (roster.Controlled == hero) roster.ForceControl(roster.FirstLiving());
  }
  public void Wipe(string killer)
  {
   var roster = GameContext.Get<HeroRosterModel>();
   var player = GameContext.Get<PlayerModel>().Unit;
   roster.ForceControl(roster.Get(player));
   GameContext.Get<UnitDamageSystem>().ProcessPlayerDeath(killer, player);
  }
  public void Revive(HeroModel hero, Vector3 position, float yaw, double health)
  {
   if (hero == null || !hero.IsDown || hero.Unit == null) return;
   if (hero.Pet != null) { GameContext.Get<PetsModel>().Revive(hero.Pet, position, yaw); hero.Unit.CurrentHealth = health; }
   else if (hero.Unit is PlayerUnit player) player.ReviveFromDown(position, yaw, health);
   hero.IsDown = false;
   hero.ReviveAt = 0f;
  }
 }
}
