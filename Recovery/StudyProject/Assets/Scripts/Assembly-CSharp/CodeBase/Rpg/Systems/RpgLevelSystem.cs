using Codebase.Core;
using Codebase.Player;
using Codebase.Player.Models;
using Codebase.Vfx;
using Scellecs.Morpeh;

namespace Codebase.Rpg.Systems
{
	public class RpgLevelSystem : SystemBase
	{
		[Inject] private PlayerModel playerModel;
		[Inject] private HeroRosterModel roster;
		[Inject] private RpgProgressModel progress;
		private PlayerUnit lastUnit;
		private bool subscribed;
		public override void OnAwake() { }
		public override void OnUpdate(float deltaTime)
		{
			if (!RpgModeConfig.Enabled) return;
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
			foreach (HeroModel hero in roster.Heroes)
			{
				if (hero.Unit == null) continue;
				HeroStats.ApplyLevel(hero.Unit, level);
				if (!hero.IsAlive || hero.Unit is PlayerUnit player && player.IsReviving) continue;
				hero.Unit.CurrentHealth = hero.Unit.GetStatValue(Codebase.Unit.StatType.Health);
				LevelUpVfx.Play(hero.Unit.transform, visualRoot: hero.Unit.VisualRoot);
			}
		}
		public override void Dispose() { if (subscribed) progress.LevelChanged -= OnLevelChanged; }
	}
}
