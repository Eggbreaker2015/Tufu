using Codebase.Core;
using Codebase.Pet;
using Codebase.Player.Models;
using Codebase.Unit;
using Scellecs.Morpeh;
using UnityEngine;
namespace Codebase.Rpg.Systems
{
 public class HeroReviveSystem : SystemBase
 {
  [Inject] private HeroRosterModel roster;
  [Inject] private PlayerModel playerModel;
  public override void OnAwake() { }
  public override void OnUpdate(float dt)
  {
   if (roster.InWipe || Dungeon.RpgDungeonSystem.BlocksRevive) return;
   var owner = RpgControl.ControlledUnitOrNull(playerModel);
   if (owner == null) return;
   foreach (var hero in roster.Heroes)
   {
    if (!hero.IsDown || hero.Unit == null || Time.time < hero.ReviveAt) continue;
    float yaw = owner.transform.eulerAngles.y;
    float fraction = PetConfig.Instance != null && PetConfig.Instance.ReviveHealthFraction > 0f ? PetConfig.Instance.ReviveHealthFraction : 0.2f;
    Vector3 position = owner.transform.position;
    if (hero.IsProtagonist) PetsModel.TrySamplePosition(PetsModel.GetFollowPosition(position, yaw, hero.SlotOrder), position, out position);
    GameContext.Get<HeroDeathService>().Revive(hero, position, yaw, hero.Unit.GetStatValue(StatType.Health) * fraction);
   }
  }
 }
}