using Codebase.VIP.Features;
using Codebase.Utilities;
using System.Linq;
using CodeBase.Audio;
using Codebase.Cashback;
using Codebase.Core;
using Codebase.Player.Models;
using Codebase.Services.Cheats;
using Codebase.UI;
using Codebase.Unit;
using Codebase.VIP;
using JetBrains.Annotations;
using Scellecs.Morpeh;

namespace Codebase.Player.Systems
{
	public class PlayerRewardSystem : SystemBase
	{
		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private CheatsModel cheatsModel;

		[Inject]
		private StatRewardContainer statRewardContainer;

		[Inject]
		private IconStyleConfig iconStyleConfig;

		[Inject]
		private IAudioPlayer audioPlayer;

		[Inject]
		private VipModel vipModel;

		[Inject]
		private CashbackModel cashbackModel;

		public override void OnAwake()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
		}

		[PublicAPI]
		public void GrantStatReward(StatModifier modifier, double displayOverride = -1.0, int displayMultiplier = 1)
		{
// Original ELF VA 0x1456084 and three bounded predicate bodies 0x145704c/78/38.
            Stat stat = playerModel.Unit.Stats.First(value => value.Type == modifier.StatType);
            StatIconStyle style = iconStyleConfig.Styles.First(value => value.StatType == stat.Type);
            modifier.Value *= cheatsModel.DropMultiplier;
            bool abilityDouble = false;
            if (modifier.Source == ModifierSource.Enemy)
                foreach (var item in playerModel.EquippedItems)
                    foreach (var ability in item.AbilityStates.Where(value => value.DoubleLoot))
                    {
                        ability.DoubleLoot = false;
                        modifier.Value *= 1f + ability.GetValue();
                        abilityDouble = true;
                    }
            bool vipDouble = false;
            if (modifier.Source == ModifierSource.Enemy)
            {
                var feature = vipModel.GetFeature<StatMultiplierFeature>("stat_multiplier");
                if (feature != null)
                {
                    var level = feature.GetLevelConfig(vipModel.Level);
                    if (((double)level.Chance).Roll()) { modifier.Value *= level.Multiplier; vipDouble = true; }
                }
            }
            stat.AddModifier(modifier);
            cashbackModel.AddEarnedStat(modifier);
            bool percent = modifier.ModifierType == ModifierType.PercentAdditive || modifier.ModifierType == ModifierType.PercentMultiplicative || modifier.ModifierType == ModifierType.PreAdditivePostMultiplicative || modifier.ModifierType == ModifierType.PostFormulaMultiplicative;
            if (displayOverride < 0.0) { displayOverride = modifier.Value; if (displayMultiplier > 1) displayOverride /= displayMultiplier; }
            if (percent) displayOverride *= 100.0;
            else if (stat.Type == StatType.Health && !playerModel.Unit.IsDying) playerModel.Unit.CurrentHealth += modifier.Value;
            string color = vipDouble ? "" : modifier.Source == ModifierSource.AbilityPermanent || abilityDouble ? "<color=yellow>" : "<color=green>";
            string count = displayMultiplier > 1 ? string.Format(" <color=#FF8A00>x{0}</color>",displayMultiplier) : "";
            statRewardContainer.Create(color + "+" + displayOverride.Format() + (percent ? "%" : "") + "<sprite name=" + style.Sprite.name + ">" + count, vipDouble);
            audioPlayer.PlaySoundOnce("devour");
		}
	}
}
