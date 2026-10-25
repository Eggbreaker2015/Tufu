using Codebase.Unit.Components;
using Codebase.Unit;
using Codebase.BossTower;
using Codebase.BossTower.Components;
using Codebase.Core;
using Codebase.Core.UI;
using Scellecs.Morpeh;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.BossTower
{
	public class BossRaidProgressView : View
	{
		[SerializeField]
		private CanvasGroup canvasGroup;

		[SerializeField]
		private TMP_Text bossNameText;

		[SerializeField]
		private TMP_Text timerText;

		[SerializeField]
		private Image healthBar;

		[Inject]
		private BossTowerModel bossTowerModel;

		[Inject]
		private Scellecs.Morpeh.World world;

		[Inject]
		private Stash<BossRaidComponent> raidStash;

		[Inject]
		private Stash<BossFightState> fightStateStash;

		private Filter raidFilter;

		protected override void Awake()
		{
			// Native ELF 0x13bfbf8.
			base.Awake();
			raidStash = world.GetStash<BossRaidComponent>(); fightStateStash = world.GetStash<BossFightState>();
			raidFilter = world.Filter.With<BossRaidComponent>().With<PlayerTag>().Build();
		}

		protected override void Render()
		{
			// Native ELF 0x13bfd74; the compact progress view uses the raw def.Key.
			foreach (Entity entity in raidFilter) {
			    ref BossRaidComponent raid = ref raidStash.Get(entity);
			    if (bossTowerModel.TryGetBoss(raid.BossKey, out BossModel boss)) bossNameText.text = boss.Def.Key;
			    timerText.text = fightStateStash.Has(entity)
			        ? string.Format("{0:00}:{1:00}", (int)(raid.RemainingTime / 60f), (int)(raid.RemainingTime % 60f)) : "--:--";
			    healthBar.fillAmount = raid.BossUnit != null && raid.BossUnit.CurrentHealth > 0.0
			        ? (float)(raid.BossUnit.CurrentHealth / raid.BossUnit.GetStatValue(StatType.Health, true)) : 0f;
			    if (canvasGroup != null) canvasGroup.alpha = 1f;
			    return;
			}
			if (canvasGroup != null) canvasGroup.alpha = 0f;
		}
	}
}
