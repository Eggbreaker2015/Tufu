using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Codebase.ABTests;
using Codebase.Amulet;
using Codebase.Amulet.Upgrades;
using Codebase.BossTower;
using Codebase.Core;
using Codebase.Crystals;
using Codebase.Economy;
using Codebase.Enemy;
using Codebase.Events.Models;
using Codebase.Location;
using Codebase.Player.Models;
using Codebase.Services.Cheats;
using Codebase.Services.Time;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

namespace CodeBase.Services.Cheats
{
	public class CheatsPanel : MonoBehaviour
	{
		[Serializable]
		public struct CheatTabIconEntry
		{
			public string TabName;

			public Sprite Icon;
		}

		private static class TabName
		{
			public const string Debug = "Debug";

			public const string Player = "Player";

			public const string Items = "Items";

			public const string Quests = "Quests";

			public const string Events = "Events";

			public const string Amulet = "Amulet";

			public const string Crystals = "Crystals";

			public const string BossTower = "Boss Tower";

			public const string PlaytestMode = "Playtest Mode";
		}

		public readonly struct TabBuilder
		{
			private readonly CheatsPanel panel;

			private readonly Transform container;

			public TabBuilder(CheatsPanel panel, Transform container)
			{
				this.panel = null;
				this.container = null;
			}

			public TabBuilder Button(string name, Action action)
			{
				return default;
			}

			public TabBuilder InputField(string name, int defaultValue, Action<int> onValueChanged)
			{
				return default;
			}

			public TabBuilder InputField(string name, float defaultValue, Action<float> onValueChanged)
			{
				return default;
			}

			public TabBuilder TimeOffsetInputField(int defaultValue, Action<int> onValueChanged)
			{
				return default;
			}
		}

		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CUploadCloudSaves_003Ed__118 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder _003C_003Et__builder;

			private Awaitable<Multicast.CloudSave.CloudSaveWriteOutcome>.Awaiter _003C_003Eu__1;

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

		private const string TimeOffsetKey = "TimeSkipSeconds";

		[SerializeField]
		private Button panelButton;

		[SerializeField]
		private CheatButton buttonPrefab;

		[SerializeField]
		private CheatInputField inputFieldPrefab;

		[SerializeField]
		private CheatTabButton tabButtonPrefab;

		[SerializeField]
		private Transform tabsRoot;

		[SerializeField]
		private Transform contentRoot;

		[SerializeField]
		private CheatTabIconEntry[] tabIcons;

		[SerializeField]
		private ABTestDebugView abTestDebugView;

		[SerializeField]
		private SpawnBossCheatPanel spawnBossCheatPanel;

		[Inject]
		private CheatsModel cheatsModel;

		[Inject]
		private ITimeService timeService;

		[Inject]
		private EnemiesModel enemiesModel;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private EventsRepository eventsRepository;

		[Inject]
		private AmuletModel amuletModel;

		[Inject]
		private AmuletUpgradesModel amuletUpgradesModel;

		[Inject]
		private AmuletUpgradeConfig amuletUpgradeConfig;

		[Inject]
		private BossSpawnService bossSpawnService;

		[Inject]
		private BossTowerConfig bossTowerConfig;

		[Inject]
		private CurrencyModel currencyModel;

		[Inject]
		private CrystalModel crystalModel;

		[Inject]
		private BossTowerModel bossTowerModel;

		[Inject]
		private IslandMasteryModel islandMasteryModel;

		private readonly List<(Action<TabBuilder> configure, Action onOpened, CheatTabButton button)> tabs;

		private int activeTabIndex;

		private int playerTabIndex;

		private bool isInitialized;

		private float playtestOnlineTimerSeconds;

		private CheatInputField timeOffsetInputField;

		public void Awake()
		{
		}

		private void Setup()
		{
		}

		private void RegisterCheats()
		{
		}

		private void AddMinibossesKilled()
		{
		}

		private void GetNextPortalGem()
		{
		}

		private void IncreaseAllBossFloors()
		{
		}

		private void UnlockAllBosses()
		{
		}

		private void Update()
		{
		}

		private void UpdatePlaytestTimeScaleOffset()
		{
		}

		private void UpdatePlaytestOfflineSessions()
		{
		}

		private void SkipPlaytestOfflineSession()
		{
		}

		private bool CanSkipPlaytestOfflineSession()
		{
			return false;
		}

		private void SkipPlaytestOfflineSeconds(float seconds)
		{
		}

		private void RegisterTab(string name, Action<TabBuilder> configure)
		{
		}

		private void RegisterTab(string name, Action<TabBuilder> configure, Action onOpened)
		{
		}

		private void RegisterTab(string name, Sprite icon, Action<TabBuilder> configure, Action onOpened = null)
		{
		}

		private void RegisterTab(string name, Sprite icon, Action<TabBuilder> configure)
		{
		}

		private Sprite GetTabIcon(string name)
		{
			return null;
		}

		private void SelectTab(int index)
		{
		}

		private void RebuildContent(Action<TabBuilder> configure)
		{
		}

		private void ClearContent()
		{
		}

		private void RefreshPlayerTabTimeOffset()
		{
		}

		private void UpdateTimeOffsetFormattedValue()
		{
		}

		private void HideAllTabs()
		{
		}

		private void SwitchPanel()
		{
		}

		private void UnlockAmulet()
		{
		}

		private void AddAmuletLevel()
		{
		}

		private void CreateMail()
		{
		}

		private void CreatePurchaseRecoveryMails()
		{
		}

		private void SkipEnemyTimers()
		{
		}

		private void FullClearCurrentIsland()
		{
		}

		private void AddEventCurrencies(int amount)
		{
		}

		private void WinEvent()
		{
		}

		private void FinishEventWithRewards()
		{
		}

		private void ExportSaveFile()
		{
		}

		private void EndCurrentEventInTenSeconds()
		{
		}

		private void StartCrystalRosterEvent()
		{
		}

		private void SetRosterLeagueIndex(int leagueIndex)
		{
		}

		private void AddBossTowerTickets()
		{
		}

		private void SpawnBoss()
		{
		}

		private void ShowABMenu()
		{
		}

		private void IncreaseMovementSpeed()
		{
		}

		private void UnlockAllGates()
		{
		}

		private void TeleportToFirstUnopenedSecretChest()
		{
		}

		private void UpgradeAllItems()
		{
		}

		private void AddCopiesToAllItems()
		{
		}

		private void EvolveAllItems()
		{
		}

		private void UnlockItemSlots()
		{
		}

		private void GiveAllItems()
		{
		}

		private void SwitchGodMode()
		{
		}

		private void SwitchBossTowerTimer()
		{
		}

		private void CollectNextPortalGem()
		{
		}

		private void UnlockPortal()
		{
		}

		private void LockPortal()
		{
		}

		private void SwitchOneHitMode()
		{
		}

		private void SwitchOneDamageMode()
		{
		}

		private void SetHpToTenPercent()
		{
		}

		private void GiveSouls()
		{
		}

		private void MaxAllAmuletUpgrades()
		{
		}

		private void ResetSouls()
		{
		}

		private void ResetUpgrades()
		{
		}

		private void RevealAllAmuletNodes()
		{
		}

		private void GenerateRandomCrystals()
		{
		}

		private void GenerateRandomRarityCrystals()
		{
		}

		private void GenerateCrystalsWithSpecialStats()
		{
		}

		private void EnchantAllCrystalsCheat()
		{
		}

		private void AddCrystalGeodes()
		{
		}

		private void AddCrystalEnchantmentStones()
		{
		}

		private void AddCrystalMagicDust()
		{
		}

		private void UnlockAllCrystalSlots()
		{
		}

		private void UnlockCrystalObjective()
		{
		}

		private void ResetCrystalObjective()
		{
		}

		private void EquipAllRandomCrystals()
		{
		}

		private void ToggleSkipLocalPurchaseDelivery()
		{
		}

		private void CompleteLevelPassTiers()
		{
		}

		private void SetPlaytestOnlineDuration(int minutes)
		{
		}

		private void SetPlaytestOfflineDuration(int minutes)
		{
		}

		private void SetPlaytestTimeScale(float timeScale)
		{
		}

		private void SkipOneHour()
		{
		}

		private void ShowAdDebugger()
		{
		}

		private void ResetQuestProgress()
		{
		}

		private void SkipCurrentQuestToLastStep()
		{
		}

		private void TeleportToQuestMob()
		{
		}

		private void OpenHub()
		{
		}

		private void PrintCloudSavesStatus()
		{
		}

		[AsyncStateMachine(typeof(_003CUploadCloudSaves_003Ed__118))]
		private UniTask UploadCloudSaves()
		{
			return default;
		}
	}
}
