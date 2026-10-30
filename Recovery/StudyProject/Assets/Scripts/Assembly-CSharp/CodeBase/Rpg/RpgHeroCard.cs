using System.Collections.Generic;
using Codebase.Core;
using Codebase.Pet;
using Codebase.Player.Inventory;
using Codebase.Unit;
using Codebase.Utilities;

namespace Codebase.Rpg
{
 public static class RpgHeroCard
 {
  public static string Role(HeroRole role)
  {
   switch (role) { case HeroRole.Tank: return "坦克"; case HeroRole.Bruiser: return "战士"; case HeroRole.Guard: return "守卫"; case HeroRole.CritDps: return "暴击输出"; case HeroRole.ReachDps: return "长柄输出"; case HeroRole.Protagonist: return "主角"; default: return "近战输出"; }
  }
  public static string Name(ItemModel item)
  {
   var pet = PetConfig.Instance.Find(item.Config);
   return pet != null && !string.IsNullOrEmpty(pet.DisplayName) ? pet.DisplayName : item.Key;
  }
  public static string Build(ItemModel item)
  {
   var definition = RpgModeConfig.Instance.ResolveHero(item.Config);
   int level = GameContext.Get<RpgProgressModel>().Level;
   int star = RpgStars.FromCopies(item.Copies);
   var hero = GameContext.Get<HeroRosterModel>().Heroes.Find(h => h.Pet != null && h.Pet.Item == item && h.Unit != null);
   List<Stat> stats;
   if (hero != null) stats = hero.Unit.Stats;
   else { stats = HeroStats.Build(definition); HeroStats.ApplyBase(stats, definition); HeroStats.ApplyLevel(stats, level); HeroStats.ApplyStar(stats, star); }
   string next = star <= RpgModeConfig.Instance.StarCopyThresholds.Count && star < 5 ? RpgModeConfig.Instance.StarCopyThresholds[star - 1].ToString() : "满级";
   return Name(item) + "（" + Role(definition.Role) + "）  Lv " + level + "  ★" + star + "\n碎片 " + item.Copies + " / " + next + "\n生命 " + Value(stats, StatType.Health).Format() + "   攻击 " + Value(stats, StatType.Damage).Format() + "\n防御 " + Value(stats, StatType.UniversalDefenceFlat).Format() + "   射程 " + Value(stats, StatType.AttackRange).Format() + "\n技能等级 " + star;
  }
  private static double Value(List<Stat> stats, StatType type) { var stat = stats.Find(s => s.Type == type); return stat != null ? stat.Value : 0; }
 }
}
