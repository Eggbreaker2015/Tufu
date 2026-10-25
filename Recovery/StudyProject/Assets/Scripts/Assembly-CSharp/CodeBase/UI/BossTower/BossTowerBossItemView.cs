using Codebase.UI.Flows;
using Codebase.UI.Amulet;
using Codebase.Localization;
using Codebase.BossTower.Commands;
using UniMob;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Codebase.Amulet.Upgrades;
using Codebase.BossTower;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Rewards;
using Codebase.UI.Events;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.BossTower
{
	public class BossTowerBossItemView : BaseView
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CSetScrollValue_003Ed__38 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder _003C_003Et__builder;

			public BossTowerBossItemView _003C_003E4__this;

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
		private Image icon;

		[SerializeField]
		private TMP_Text nameText;

		[SerializeField]
		private TMP_Text nameLockedText;

		[SerializeField]
		private TMP_Text descriptionText;

		[SerializeField]
		private TMP_Text attackText;

		[SerializeField]
		private TMP_Text defenceText;

		[SerializeField]
		private TMP_Text costText;

		[SerializeField]
		private Transform rewardsContainer;

		[SerializeField]
		private RewardView rewardPrefab;

		[SerializeField]
		private Button raidButton;

		[SerializeField]
		private CanvasGroup raidButtonCanvasGroup;

		[SerializeField]
		private Button raidLimitedButton;

		[SerializeField]
		private Button fightButton;

		[SerializeField]
		private Button viewButton;

		[SerializeField]
		private Button lockedButton;

		[SerializeField]
		private TMP_Text unlockLevelText;

		[SerializeField]
		private GameObject unlockedPanel;

		[SerializeField]
		private GameObject lockedPanel;

		[SerializeField]
		private GameObject weakenedMarker;

		[SerializeField]
		private GameObject weakenedGlow;

		[SerializeField]
		private GameObject bgLocked;

		[SerializeField]
		private GameObject bgUnlocked;

		[SerializeField]
		private GameObject bgFadeLocked;

		[SerializeField]
		private GameObject bgFadeUnlocked;

		[SerializeField]
		private ScrollRect scrollbar;

		[Inject]
		private BossTowerModel bossTowerModel;

		[Inject]
		private AmuletUpgradesModel upgradesModel;

		[Inject]
		private AmuletUpgradeConfig upgradeConfig;

		[Inject]
		private UINavigator navigator;

		[SerializeField]
		private Color costNotEnoughColor;

		private ViewContainer<RewardView, RewardBase> rewardsViewContainer;

		private BossModel bossModel;

		private Action closeHubAction;

		protected override void Awake()
		{
			// Native ELF VA 0x13c5bfc; original attaches exactly these three listeners.
			base.Awake();
			raidButton.onClick.AddListener(Raid);
			fightButton.onClick.AddListener(Fight);
			viewButton.onClick.AddListener(OpenUnlockGraph);
		}

		private void OpenDefeatFirstMessage()
		{
			// Native ELF VA 0x13c5d40.
			App.InitiateFlow(OpenDialogFlow.WithMessage("BOSS_TOWER_DEFEAT_FIRST_TITLE", "BOSS_TOWER_DEFEAT_FIRST_MESSAGE")).Forget();
		}

		protected override void Start()
		{
			// Native ELF VA 0x13c5e1c; original source is RewardsBaked, not previous-floor rewards.
			base.Start();
			rewardsViewContainer = new ViewContainer<RewardView, RewardBase>(rewardsContainer, rewardPrefab)
			    .BindTo(Lifetime, () => bossModel.RewardsBaked).Setup(SetupView);
		}

		private static void SetupView(RewardView view, RewardBase reward)
		{
			// Native ELF VA 0x13c6018.
			view.Setup(reward, RewardViewStyle.Simplified);
			view.SetInfoBtnVisible(false);
		}

		public void Setup(BossModel bossModel, Action closeHubAction)
		{
			// Native ELF VA 0x13c6050; reaction runs immediately and owns reactive dependencies.
			this.bossModel = bossModel;
			this.closeHubAction = closeHubAction;
			Atom.Reaction(Lifetime, Render);
			SetScrollValue().Forget();
		}

		
		private async UniTask SetScrollValue()
		{
			// Native MoveNext ELF VA 0x13c6e38; exactly fifty NextFrame resets, destroy cancellation.
			for (int i = 0; i < 50; i++)
			{
			    scrollbar.horizontalNormalizedPosition = 0f;
			    await UniTask.NextFrame(destroyCancellationToken);
			}
		}

		protected override void Render()
		{
			// Native ELF VA 0x13c61ac; native does not render attackText or defenceText.
			if (bossModel == null) return;
			_ = bossModel.RewardsVersion.Value;
			_ = upgradesModel.Version;
			BossDef def = bossModel.Def;
			bool unlocked = bossTowerModel.IsBossUnlocked(def.Key);
			lockedPanel.SetActive(!unlocked); unlockedPanel.SetActive(unlocked);
			bgLocked.SetActive(!unlocked); bgUnlocked.SetActive(unlocked);
			bgFadeLocked.SetActive(!unlocked); bgFadeUnlocked.SetActive(unlocked);
			icon.sprite = def.FullPortrait;
			icon.transform.localScale = Vector3.one * def.PortraitScale;
			icon.GetComponent<RectTransform>().anchoredPosition = (Vector2)def.PortraitItemOffset;
			string bossName = LocalizationService.Get("BOSS_NAME_" + def.DisplayName);
			if (!unlocked)
			{
			    nameLockedText.text = string.Format("{0} - <color=#FFD500>{1}", bossName, bossModel.Floor);
			    bool hasUpgrade = !string.IsNullOrEmpty(def.UnlockUpgradeKey);
			    bool levelLocked = hasUpgrade && upgradesModel.IsAmuletLevelLocked(def.UnlockUpgradeKey);
			    viewButton.gameObject.SetActive(hasUpgrade && !levelLocked);
			    lockedButton.gameObject.SetActive(levelLocked);
			    if (levelLocked) unlockLevelText.text = string.Format(LocalizationService.Get("BOSS_TOWER_AMULET_REQUIREMENT"), upgradeConfig.GetUpgrade(def.UnlockUpgradeKey)?.RequiredAmuletLevel.ToString() ?? "");
			    return;
			}
			string title = string.Format("{0} - <color=#FFD500>{1}{2}", bossName, bossModel.Floor, bossModel.IsWeakened ? " <sprite name=SkullWithArrow>" : "");
			nameText.text = title; nameLockedText.text = title;
			bool defeated = bossTowerModel.IsBossDefeated(def.Key);
			raidButton.gameObject.SetActive(bossModel.HasRemainingDailyClaims || !defeated);
			raidLimitedButton.gameObject.SetActive(defeated && !bossModel.HasRemainingDailyClaims);
			raidButtonCanvasGroup.alpha = defeated && bossModel.HasRemainingDailyClaims ? 1f : .5f;
			descriptionText.text = LocalizationService.Get("BOSS_DESCRIPTION_" + def.Key);
			Color costColor = bossTowerModel.HasEnoughTickets(1) ? Color.white : costNotEnoughColor;
			costText.text = string.Format("{0} - <color=#{1}>{2}<sprite name=atlas_92>", LocalizationService.Get("BTN_RAID"), ColorUtility.ToHtmlStringRGB(costColor), 1);
			weakenedMarker.SetActive(bossModel.IsWeakened); weakenedGlow.SetActive(bossModel.IsWeakened);
		}

		private void Raid()
		{
			// Native ELF VA 0x13c6a50.
			if (bossTowerModel.CanGrantBossClaimReward(bossModel.Def.Key) && bossTowerModel.HasEnoughTickets(1))
			    App.ExecuteCommand(new ClaimBossRaidRewardCommand { BossKey = bossModel.Def.Key });
		}

		private void Fight()
		{
			// Native ELF VA 0x13c6b3c; actual details popup owns the arena command.
			navigator.Open<BossTowerBossDetailsView>("Assets/Prefabs/UI/BossTower/BossTowerBossDetailsView.prefab").Setup(bossModel, Close);
		}

		private void OpenUnlockGraph()
		{
			// Native ELF VA 0x13c6c14.
			navigator.Open<AmuletUpgradeGraphView>("Assets/Prefabs/UI/Amulet/AmuletUpgradesGraph.prefab").FocusOnUpgrade(bossModel.Def.UnlockUpgradeKey);
		}

		private void Close()
		{
			// Native ELF VA 0x13c6d64.
			if (closeHubAction == null) return;
			closeHubAction.Invoke();
			closeHubAction = null;
		}
	}
}
