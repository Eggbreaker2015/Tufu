using Codebase.BossTower.Flows;
using Codebase.BossTower.Commands;
using Codebase.Localization;
using Codebase.Utilities;
using Codebase.Unit;
using System.Linq;
using UniMob;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Codebase.Amulet.Upgrades;
using Codebase.BossTower;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Enemy;
using Codebase.Meta;
using Codebase.Player.Models;
using Codebase.Rewards;
using Codebase.UI.Events;
using Codebase.UI.IslandNotification;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using TMPro;
using UniMob.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.BossTower
{
	public class BossTowerBossDetailsView : PopupView
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CSetScrollValue_003Ed__50 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder _003C_003Et__builder;

			public BossTowerBossDetailsView _003C_003E4__this;

			private int _003Ci_003E5__2;

			private UniTask.Awaiter _003C_003Eu__1;

			private void MoveNext()
			{
			}

			void IAsyncStateMachine.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				this.MoveNext();
			}

			[DebuggerHidden]
			private void SetStateMachine(IAsyncStateMachine stateMachine)
			{
			}

			void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
			{
				//ILSpy generated this explicit interface implementation from .override directive in SetStateMachine
				this.SetStateMachine(stateMachine);
			}
		}

		[SerializeField]
		private Button closeButton;

		[SerializeField]
		private Image icon;

		[SerializeField]
		private TMP_Text nameText;

		[SerializeField]
		private TMP_Text abilityDescriptionText;

		[SerializeField]
		private TMP_Text abilityEliteDescriptionTxt;

		[SerializeField]
		private GameObject nonEliteDescriptionPanel;

		[SerializeField]
		private TMP_Text weaknessDescrTxt;

		[SerializeField]
		private IslandNotificationMapInfoView attackInfo;

		[SerializeField]
		private TMP_Text floorText;

		[SerializeField]
		private Transform rewardsContainer;

		[SerializeField]
		private RewardView rewardPrefab;

		[SerializeField]
		private Button raidButton;

		[SerializeField]
		private CanvasGroup raidButtonCanvasGroup;

		[SerializeField]
		private Button raidLimitReachedButton;

		[SerializeField]
		private Button fightButton;

		[SerializeField]
		private Button fightButtonBlocked;

		[SerializeField]
		private Color attackColor;

		[SerializeField]
		private TMP_Text damageText;

		[SerializeField]
		private TMP_Text healthText;

		[SerializeField]
		private TMP_Text costText;

		[SerializeField]
		private HitCellsBar hitCellsBar;

		[SerializeField]
		private GameObject weakenedMarker;

		[SerializeField]
		private GameObject weakenedEffect;

		[SerializeField]
		private GameObject regularEffect;

		[SerializeField]
		private TextMeshProUGUI timerTxt;

		[SerializeField]
		private TextMeshProUGUI ticketsTxt;

		[SerializeField]
		private GameObject noSoulMsgTxt;

		[SerializeField]
		private ScrollRect scrollbar;

		[SerializeField]
		private Color costNotEnoughColor;

		[SerializeField]
		private Transform upgradesContainer;

		[SerializeField]
		private BossTowerUpgradeDescriptionView upgradeDescriptionPrefab;

		[Inject]
		private BossTowerModel bossTowerModel;

		[Inject]
		private BossSpawnService bossSpawnService;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private MetaStatsModel metaStatsModel;

		[Inject]
		private AmuletUpgradesModel upgradesModel;

		[CompilerGenerated]
		private BossModel __ActiveBossModel__BackingField;

		private ViewContainer<RewardView, RewardBase> rewardsViewContainer;

		private ViewContainer<BossTowerUpgradeDescriptionView, string> upgradeDescriptionsViewContainer;

		private BossModel bossModel;

		private EnemySpawnerConfig spawnerConfig;

		private Action onClose;

		private StringBuilder descriptionBuilder;

		private ComputedAtom<BossModel> __ActiveBossModel;

		private BossModel ActiveBossModel
		{
			[CompilerGenerated]
			get
			{
			// Native generated atom getter ELF VA 0x13c36e8.
			LifetimeScopeExtension.ThrowIfDisposed(this);
			if (__ActiveBossModel == null) __ActiveBossModel = CodeGenAtom.CreatePooled(this, null, () => ActiveBossModel, false);
			return __ActiveBossModel.DirectEvaluate() ? __ActiveBossModel__BackingField : __ActiveBossModel.Value;
		}
			[CompilerGenerated]
			set
			{
			// Native generated atom setter ELF VA 0x13c3804.
			LifetimeScopeExtension.ThrowIfDisposed(this);
			if (__ActiveBossModel != null && !__ActiveBossModel.CompareAndInvalidate(value)) return;
			__ActiveBossModel__BackingField = value;
		}
		}

		protected override void Awake()
		{
			// Native ELF VA 0x13c3884.
			base.Awake(); closeButton.onClick.AddListener(Close); raidButton.onClick.AddListener(Raid); fightButton.onClick.AddListener(Fight);
			descriptionBuilder = new StringBuilder();
		}

		protected override void Start()
		{
			// Native ELF VA 0x13c3a00; source 0x13c5728.
			base.Start();
			rewardsViewContainer = new ViewContainer<RewardView, RewardBase>(rewardsContainer,rewardPrefab)
			    .BindTo(Lifetime, () => ActiveBossModel.RewardsBaked).Setup(SetupView);
		}

		private static void SetupView(RewardView view, RewardBase reward)
		{
			// Native ELF VA 0x13c3bfc.
			view.Setup(reward, RewardViewStyle.Simplified);
			view.SetInfoBtnVisible(false);
		}

		public void Setup(BossModel bossModel, Action onClose)
		{
			// Native ELF VA 0x13c3c34; genuine native preview stats are supplied by BossSpawnService.
			this.bossModel = bossModel; ActiveBossModel = bossModel;
			spawnerConfig = bossSpawnService.CreateSpawnerConfig(bossModel.Def,bossModel.IsSuperBoss);
			bossSpawnService.BakeStatsForFloor(spawnerConfig,bossModel.GetOptimumTime(),bossModel.IsWeakened);
			this.onClose = onClose; SetupBossInfo(); SetScrollValue().Forget();
		}

		
		private async UniTask SetScrollValue()
		{
			// Native MoveNext ELF VA 0x13c57f8.
			for (int i = 0; i < 50; i++) { scrollbar.horizontalNormalizedPosition = 0f; await UniTask.NextFrame(destroyCancellationToken); }
		}

		protected override void Render()
		{
			// Native ELF VA 0x13c3f04; original uses baked BaseValue and banker's rounding for weakness labels.
			if (bossModel == null) return;
			BossModel active = ActiveBossModel; _ = active.RewardsVersion.Value; _ = upgradesModel.Version; _ = active.IsSuperBoss;
			nameText.text = string.Format("{0} - <color=#FFD500>{1}",LocalizationService.Get("BOSS_NAME_"+bossModel.Def.DisplayName),bossModel.Floor);
			icon.sprite = bossModel.Def.FullPortrait;
			floorText.text = string.Format("{0} {1}",LocalizationService.Get("BOSS_TOWER_FLOOR"),bossModel.Floor);
			abilityDescriptionText.text = LocalizationStrings.Get("BOSS_ABILITY_"+active.Def.Key);
			abilityEliteDescriptionTxt.text = LocalizationStrings.Get("BOSS_ELITE_ABILITY_"+active.Def.Key);
			abilityEliteDescriptionTxt.gameObject.SetActive(active.IsSuperBoss); nonEliteDescriptionPanel.gameObject.SetActive(!active.IsSuperBoss);
			IEnumerable<string> upgrades = GetActiveUpgradeDescriptions(); descriptionBuilder.Clear();
			descriptionBuilder.Append(LocalizationService.Get("BOSS_WEAKNESS_"+active.Def.Key));
			foreach (string description in upgrades) { descriptionBuilder.Append("<br>"); descriptionBuilder.Append(description); }
			weaknessDescrTxt.text = descriptionBuilder.ToString();
			bool unlocked = bossTowerModel.IsBossUnlocked(active.Def.Key), defeated = bossTowerModel.IsBossDefeated(active.Def.Key);
			fightButton.gameObject.SetActive(unlocked && !playerModel.InBossTowerArena);
			fightButtonBlocked.gameObject.SetActive(unlocked && playerModel.InBossTowerArena);
			raidButton.gameObject.SetActive(active.HasRemainingDailyClaims || !defeated);
			raidLimitReachedButton.gameObject.SetActive(defeated && unlocked && !active.HasRemainingDailyClaims);
			raidButtonCanvasGroup.alpha = defeated ? 1f : .5f;
			Color color = bossTowerModel.Tickets > 0 ? Color.white : costNotEnoughColor;
			double weakness = metaStatsModel.Get("tower_boss_weakness").Value;
			double damageReduction = metaStatsModel.Get("tower_damage_reduction").Value;
			int damagePercent = (int)Math.Round((weakness+damageReduction)*100.0);
			int healthPercent = (int)Math.Round(weakness*100.0);
			string damageSuffix = damagePercent > 0 && bossModel.IsWeakened ? string.Format(" <size=80%><color=#FF3668>-{0}%",damagePercent) : "";
			string healthSuffix = healthPercent > 0 && bossModel.IsWeakened ? string.Format(" <size=80%><color=#FF3668>-{0}%",healthPercent) : "";
			damageText.text = spawnerConfig.Stats.Find(stat => (int)stat.Type == 3).BaseValue.Format() + damageSuffix;
			healthText.text = spawnerConfig.Stats.Find(stat => (int)stat.Type == 1).BaseValue.Format() + healthSuffix;
			costText.text = string.Format("{0} - <color=#{1}>{2}<sprite name=atlas_92>",LocalizationService.Get("BTN_RAID"),ColorUtility.ToHtmlStringRGB(color),1);
			hitCellsBar.SetValue(bossModel.IsSuperBoss ? 10 : bossModel.Floor % 10);
			weakenedEffect.SetActive(bossModel.IsWeakened); regularEffect.SetActive(!bossModel.IsWeakened); weakenedMarker.gameObject.SetActive(bossModel.IsWeakened);
			ticketsTxt.text = string.Format("{0}/{1}",bossTowerModel.Tickets,bossTowerModel.MaxTickets);
			timerTxt.text = TimeLocalizer.GetFormatTime((float)bossTowerModel.TimeToReset.TotalSeconds,true);
			noSoulMsgTxt.SetActive(!bossModel.IsSuperBoss);
		}

		private void SetupBossInfo()
		{
			// Native ELF VA 0x13c3d2c.
			if (bossModel == null || attackInfo == null) return;
			Injector.InjectInto(attackInfo);
			attackInfo.Setup(bossModel.Def.WeaponType, LocalizationStrings.Get(string.Format("WEAPON_TYPE_{0}",bossModel.Def.WeaponType)),attackColor);
		}

		private WeaponType GetArmorWeaponType()
		{
			// Native ELF VA 0x13c5328; strict greater-than picks the first positive maximum.
			if (bossModel.Def.ArmorConfig == null || bossModel.Def.ArmorConfig.Armors.Count == 0) return 0;
			WeaponType result = 0; float maximum = 0f;
			foreach (Armor armor in bossModel.Def.ArmorConfig.Armors) if (armor.Resist > maximum) { maximum = armor.Resist; result = armor.Type; }
			return result;
		}

		private void Raid()
		{
			// Native ELF VA 0x13c557c; the genuine command enforces eligibility.
			App.ExecuteCommand(new ClaimBossRaidRewardCommand { BossKey = bossModel.Def.Key });
		}

		private IEnumerable<string> GetActiveUpgradeDescriptions()
		{
			// Native ELF VA 0x13c4f90.
			BossModel active = ActiveBossModel; if (active == null) return Array.Empty<string>();
			var result = new List<string>();
			foreach (string key in active.Def.BossUpgradeKeys)
			    if (upgradesModel.GetLevel(key) > 0)
			        result.Add(new AmuletUpgradeDescriptionContext(key,upgradesModel,true)
			            .Resolve(LocalizationStrings.Get("UPGRADE_DESCRIPTION_" + key.ToUpperInvariant())));
			return result;
		}

		private void Fight()
		{
			// Native ELF VA 0x13c5624; callback absence prevents starting a duplicate flow.
			if (onClose == null) return;
			Close(); onClose.Invoke(); onClose = null;
			App.InitiateFlow(new StartBossArenaFloorFlow { BossKey = bossModel.Def.Key }).Forget();
		}
	}
}
