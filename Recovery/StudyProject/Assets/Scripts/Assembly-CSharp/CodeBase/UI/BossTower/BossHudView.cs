using Codebase.Unit.Components;
using Codebase.Localization;
using System.Collections.Generic;
using BrunoMikoski.AnimationSequencer;
using Codebase.BossTower;
using Codebase.BossTower.Components;
using Codebase.BossTower.Systems;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Player.Models;
using Codebase.Utilities.ObjectPool;
using Scellecs.Morpeh;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.BossTower
{
	public class BossHudView : View
	{
		private static readonly Color MaxStacksColor = new Color(1f, .9215686321258545f, .01568627543747425f, 1f);

		private static readonly Color DefaultColor = Color.white;

		[SerializeField]
		private CanvasGroup canvasGroup;

		[SerializeField]
		private TMP_Text timerText;

		[SerializeField]
		private Transform barsContainer;

		[SerializeField]
		private BossHealthBarView barPrefab;

		[SerializeField]
		private BerserkerHudPanel berserkerHudPanel;

		[SerializeField]
		private LavaCrabHudPanel lavaCrabHudPanel;

		[SerializeField]
		private VampireMinionHudPanel vampireMinionHudPanel;

		[SerializeField]
		private GameObject marksRoot;

		[SerializeField]
		private TMP_Text marksText;

		[SerializeField]
		private AnimationSequencerController marksAnimation;

		[SerializeField]
		private AnimationSequencerController marksZeroedAnimation;

		[SerializeField]
		private GameObject singleHpBarFooterBg;

		[SerializeField]
		private TMP_Text bossNameTxt;

		[SerializeField]
		private VisibilityAnimator visibilityAnimator;

		[SerializeField]
		private Button leaveButton;

		[Inject]
		private Stash<VampireBossComponent> vampireBossStash;

		[Inject]
		private Stash<BerserkerBossComponent> berserkerBossStash;

		[Inject]
		private Stash<LavaCrabBossComponent> lavaCrabBossStash;

		[Inject]
		private BossTowerModel bossTowerModel;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private UINavigator uiNavigator;

		[Inject]
		private UiModel uiModel;

		[Inject]
		private Scellecs.Morpeh.World world;

		[Inject]
		private Stash<BossRaidComponent> raidStash;

		[Inject]
		private BossHealthBarSystem bossHealthBarSystem;

		[Inject]
		private HydraMarkSyncSystem hydraMarkSyncSystem;

		private Filter raidFilter;

		private int previousMarks;

		private readonly ObjectPool<BossHealthBarView> pool = new ObjectPool<BossHealthBarView>();

		private readonly Dictionary<string, BossHealthBarView> bars = new Dictionary<string, BossHealthBarView>();

		private readonly HashSet<string> activeIds = new HashSet<string>();

		private readonly HashSet<string> ghostParentGuids = new HashSet<string>();

		private readonly List<string> toRemove = new List<string>();

		private new void Start()
		{
            Initialize();
            raidStash = world.GetStash<BossRaidComponent>();
            raidFilter = world.Filter.With<BossRaidComponent>().With<Codebase.Unit.Components.PlayerTag>().Build();
            leaveButton.onClick.AddListener(OnLeavePressed);
            global::Codebase.Core.GameContext.Set(this);
            visibilityAnimator.SetVisibleImmediate(false);
        }

		private void OnLeavePressed()
		{
			// Native ELF 0x13bc9e4.
			uiNavigator.Open<BossTowerLeaveView>("Assets/Prefabs/UI/BossTower/BossTowerLeaveView.prefab");
		}

		public void ForceToShow()
		{
			// Native ELF 0x13bca50.
			uiModel.BossHudVisible = true; previousMarks = -1; visibilityAnimator.SetVisible(true, true);
		}

		protected override void Render()
		{
			// Native ELF 0x13bca94; original first raid controls the displayed name/panels.
			bool fighting = bossTowerModel.IsFighting;
			visibilityAnimator.SetVisible(fighting && uiModel.BossHudVisible);
			leaveButton.gameObject.SetActive(playerModel.InBossTowerArena);
			if (!fighting) {
			    berserkerHudPanel.gameObject.SetActive(false); lavaCrabHudPanel.gameObject.SetActive(false); vampireMinionHudPanel.gameObject.SetActive(false); return;
			}
			Entity bossEntity = default;
			foreach (Entity entity in raidFilter) {
			    ref BossRaidComponent raid = ref raidStash.Get(entity);
			    bossEntity = raid.BossUnit.EntityProvider.Entity;
			    if (raid.BossKey != null) {
			        BossModel model = bossTowerModel.GetBoss(raid.BossKey);
			        bossNameTxt.text = string.Format("{0} - <color=#FFD500>{1}", LocalizationService.Get("BOSS_NAME_" + model.Def.DisplayName), model.Floor);
			    }
			    break;
			}
			berserkerHudPanel.gameObject.SetActive(berserkerBossStash.Has(bossEntity));
			lavaCrabHudPanel.gameObject.SetActive(lavaCrabBossStash.Has(bossEntity));
			vampireMinionHudPanel.gameObject.SetActive(vampireBossStash.Has(bossEntity));
		}

		private void LateUpdate()
		{
			// Native ELF 0x13bceb8; preserve parent-GUID transfer before pooling old bars.
			RenderMarks();
			if (!bossTowerModel.IsFighting) {
			    if (bars.Count > 0) { foreach (BossHealthBarView view in bars.Values) pool.Despawn(view); bars.Clear(); }
			    return;
			}
			foreach (Entity entity in raidFilter) {
			    float rawRemaining = raidStash.Get(entity).RemainingTime;
			    float remaining = rawRemaining >= 0f ? rawRemaining : 0f;
			    timerText.text = string.Format("{0:00}:{1:00}", (int)(remaining / 60f), (int)(remaining % 60f));
			    break;
			}
			IReadOnlyList<BossHealthBarData> data = bossHealthBarSystem.Data;
			activeIds.Clear(); ghostParentGuids.Clear();
			foreach (BossHealthBarData datum in data) {
			    activeIds.Add(datum.UnitGuid);
			    if (datum.OriginalUnitGuid != null) ghostParentGuids.Add(datum.OriginalUnitGuid);
			}
			toRemove.Clear();
			foreach (string key in bars.Keys) if (!activeIds.Contains(key) && !ghostParentGuids.Contains(key)) toRemove.Add(key);
			foreach (string key in toRemove) { pool.Despawn(bars[key]); bars.Remove(key); }
			foreach (BossHealthBarData datum in data) {
			    if (!bars.TryGetValue(datum.UnitGuid, out BossHealthBarView view)) {
			        if (datum.OriginalUnitGuid != null && bars.TryGetValue(datum.OriginalUnitGuid, out BossHealthBarView previous)) {
			            bars.Remove(datum.OriginalUnitGuid); view = previous;
			        } else view = pool.Spawn(barPrefab, Vector3.zero, barsContainer);
			        if (datum.OriginalUnitGuid != null) view.StartGhostAnimation();
			        bars[datum.UnitGuid] = view;
			    }
			    view.SetData(datum);
			}
			if (singleHpBarFooterBg.activeSelf != (bars.Count == 1)) singleHpBarFooterBg.SetActive(bars.Count == 1);
		}

		private void RenderMarks()
		{
			// Native ELF 0x13bdf84; the original successful zero/zero sample retains its previous display.
			if (marksRoot == null) return;
			if (!hydraMarkSyncSystem.TryGetSharedMark(out int stacks, out int maxStacks, out int hydraCount)) {
			    if (previousMarks != 0) { previousMarks = 0; marksRoot.SetActive(false); }
			    return;
			}
			if ((stacks != 0 || hydraCount != 0) && stacks != previousMarks) {
			    previousMarks = stacks; marksRoot.SetActive(true);
			    marksText.text = string.Format("{0}<sprite name=atlas_110>", stacks);
			    marksText.color = stacks > 0 && stacks >= maxStacks ? MaxStacksColor : DefaultColor;
			    if (marksAnimation != null) {
			        marksRoot.transform.localPosition = Vector2.zero;
			        (stacks < 1 ? marksZeroedAnimation : marksAnimation).Play();
			    }
			}
		}
	}
}
