using System;
using Codebase.Core;
using Codebase.Core.UserData;
using UniMob;

namespace Codebase.Rpg
{
	public class RpgProgressModel : Model
	{
		private RpgUd data;
		public int Level => data == null ? 1 : data.TeamLevel.Value;
		public double Xp => data == null ? 0 : data.Xp.Value;
		public double XpToNext => Level >= RpgModeConfig.Instance.LevelCap ? 0 : RpgNumbers.Need(Level);
		public event Action<int> LevelChanged;
		public RpgProgressModel(Lifetime lifetime) : base(lifetime) { }
		public override void Initialize()
		{
			data = GameContext.Get<GameDataService>().Data.Rpg;
		}
		public double AddXp(double amount)
		{
			if (data == null || amount <= 0 || double.IsNaN(amount) || Level >= RpgModeConfig.Instance.LevelCap) return 0;
			int level = Level;
			int old = level;
			double previousXp = Xp;
			double xp = previousXp + amount;
			double consumed = 0;
			int cap = RpgModeConfig.Instance.LevelCap;
			while (level < cap && xp >= RpgNumbers.Need(level)) { double need = RpgNumbers.Need(level); xp -= need; consumed += need; level++; }
			data.TeamLevel.Value = level;
			data.Xp.Value = level >= cap ? 0 : xp;
			if (level != old) LevelChanged?.Invoke(level);
			return level >= cap ? consumed - previousXp : amount;
		}
	}
}
