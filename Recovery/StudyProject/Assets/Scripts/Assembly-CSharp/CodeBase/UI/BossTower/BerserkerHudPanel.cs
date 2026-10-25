using Codebase.Unit.Components;
using DG.Tweening;
using Codebase.BossTower.Components;
using Codebase.Core;
using Scellecs.Morpeh;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.BossTower
{
	public class BerserkerHudPanel : MonoBehaviour
	{
		private const float BlinkSpeed = 4f;

		private const float BlinkMinAlpha = 0.6f;

		[SerializeField]
		private Image rageSlider;

		[SerializeField]
		private GameObject[] levelIcons;

		[SerializeField]
		private GameObject[] maxLevelParticles;

		[SerializeField]
		private CanvasGroup levelIconCanvasGroup;

		[SerializeField]
		private GameObject bigSizeDivider;

		[SerializeField]
		private GameObject smallSizeDivider;

		[SerializeField]
		private RectTransform viewBarRect;

		private int previousMaxRageLevel;

		[Inject]
		private Scellecs.Morpeh.World world;

		[Inject]
		private Stash<BerserkerBossComponent> berserkerStash;

		private Filter bossFilter;

		// Native ctor ELF VA 0x13bc250.
		private float lastTargetValue = -1f;

		private void Start()
		{
			// Native ELF VA 0x13bbd44; use the injected real world, including its current archetypes.
			berserkerStash = world.GetStash<BerserkerBossComponent>();
			bossFilter = world.Filter.With<BerserkerBossComponent>().With<UnitComponent>().Without<UnitDeadMarker>().Build();
		}

		private void LateUpdate()
		{
			// Native ELF VA 0x13bbea4. Only the first live Berserker contributes to this original panel.
			foreach (Entity entity in bossFilter)
			{
			    ref BerserkerBossComponent boss = ref berserkerStash.Get(entity);
			    int level = boss.RageLevel, maximumLevel = boss.MaxRageLevel;
			    float lower = level > 0 ? boss.RageThresholds[level - 1] : 0f;
			    float upper = boss.RageThresholds[level < maximumLevel ? level : maximumLevel - 1];
			    float span = Mathf.Max(.001f, upper - lower);
			    float progress = Mathf.Clamp01((boss.CurrentRage - lower) / span);
			    float target = (float)level / maximumLevel + progress / maximumLevel;
			    if (!Mathf.Approximately(lastTargetValue, target))
			    {
			        lastTargetValue = target;
			        rageSlider.DOFillAmount(target, .2f);
			    }
			    if (previousMaxRageLevel != maximumLevel)
			    {
			        previousMaxRageLevel = maximumLevel;
			        SetRagesMaxLevel(maximumLevel);
			    }
			    for (int i = 0; i < levelIcons.Length; i++) levelIcons[i].SetActive(level == i);
			    foreach (GameObject particles in maxLevelParticles) particles.SetActive(level >= maximumLevel);
			    levelIconCanvasGroup.alpha = 1f;
			    return;
			}
		}

		public void SetRagesMaxLevel(int level)
		{
			// Native ELF VA 0x13bc1c4; original constant 849 changes height while preserving width.
			Vector2 size = viewBarRect.sizeDelta;
			viewBarRect.sizeDelta = new Vector2(size.x, 849f);
			bigSizeDivider.gameObject.SetActive(level > 3);
			smallSizeDivider.gameObject.SetActive(level < 4);
		}
	}
}
