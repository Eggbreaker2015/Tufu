using Codebase.Core;
using Codebase.Player;
using Codebase.Player.Models;
using Codebase.UI;
using Codebase.Unit;
using Codebase.Utilities;
using Codebase.Vfx;
using System.Linq;
using Scellecs.Morpeh;

namespace Codebase.Rpg.Systems
{
	public class RpgLevelSystem : SystemBase
	{
		[Inject] private PlayerModel playerModel;
		[Inject] private HeroRosterModel roster;
		[Inject] private RpgProgressModel progress;
		[Inject] private IconStyleConfig iconStyleConfig;
		private PlayerUnit lastUnit;
		private bool subscribed;
		public override void OnAwake() { }
		public override void OnUpdate(float deltaTime)
		{
			if (!subscribed) { progress.LevelChanged += OnLevelChanged; subscribed = true; }
			if (playerModel.Unit == null || playerModel.Unit == lastUnit) return;
			lastUnit = playerModel.Unit;
			HeroModel hero = roster.Get(lastUnit);
			HeroStats.ApplyBase(lastUnit, hero != null ? hero.Definition : null);
			HeroStats.ApplyLevel(lastUnit, progress.Level);
			if (!lastUnit.IsDead && !lastUnit.IsDying && !lastUnit.IsReviving) lastUnit.CurrentHealth = lastUnit.GetStatValue(Codebase.Unit.StatType.Health);
		}
		private void OnLevelChanged(int level)
		{
			PlayerUnit controlled = playerModel.Unit;
			double damageBefore = controlled != null ? controlled.GetStatValue(StatType.Damage) : 0;
			double healthBefore = controlled != null ? controlled.GetStatValue(StatType.Health) : 0;
			foreach (HeroModel hero in roster.Heroes)
			{
				if (hero.Unit == null) continue;
				HeroStats.ApplyLevel(hero.Unit, level);
				if (!hero.IsAlive || hero.Unit is PlayerUnit player && player.IsReviving) continue;
				hero.Unit.CurrentHealth = hero.Unit.GetStatValue(Codebase.Unit.StatType.Health);
				PlayLevelUpVfx(hero);
			}
			if (controlled == null || !GameContext.Has<StatRewardContainer>()) return;
			StatRewardContainer container = GameContext.Get<StatRewardContainer>();
			ShowGain(container, StatType.Damage, controlled.GetStatValue(StatType.Damage) - damageBefore);
			ShowGain(container, StatType.Health, controlled.GetStatValue(StatType.Health) - healthBefore);
		}
		private static void PlayLevelUpVfx(HeroModel hero)
		{
			float radius = -1f;
			if (hero.Definition != null && hero.Definition.IsProtagonist && CharacterVfxScale.TryGetBody(hero.Unit.transform, out float bodyRadius, out _))
				radius = bodyRadius * RpgModeConfig.Instance.ProtagonistLevelUpVfxScale;
			LevelUpVfx.Play(hero.Unit.transform, radius);
		}
		private void ShowGain(StatRewardContainer container, StatType type, double amount)
		{
			if (amount <= 0) return;
			StatIconStyle style = iconStyleConfig.Styles.FirstOrDefault(value => value.StatType == type);
			container.Create("<color=green>+" + amount.Format() + (style != null ? "<sprite name=" + style.Sprite.name + ">" : ""));
		}
		public override void Dispose() { if (subscribed) progress.LevelChanged -= OnLevelChanged; }
	}
}
