using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Codebase.Enemy;
using Codebase.Events.Models;
using Codebase.Player.Inventory;
using Codebase.Rewards;
using Codebase.Simulation.Forecast;
using Codebase.Simulation.Systems;
using Codebase.Unit;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;

namespace Codebase.Events.Services
{
	public class EventForecastService
	{
		[CompilerGenerated]
		private sealed class _003C_003Ec__DisplayClass15_0
		{
			public ForecastSnapshotExtended lastSnap;

			public Func<StatType, bool> _003C_003E9__0;

			internal bool _003CBuildForecastAndBakeRewardsIfNeededAsync_003Eb__0(StatType t)
			{
				return false;
			}
		}

		[CompilerGenerated]
		private sealed class _003C_003Ec__DisplayClass16_0
		{
			public ForecastAutoBalanceInput autoBalanceInput;

			public SimulationSystem simulationSystem;

			internal float _003CBuildEventPlanAsync_003Eb__0(int index)
			{
				return 0f;
			}
		}

		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CBuildEventPlanAsync_003Ed__16 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder<string> _003C_003Et__builder;

			public EventModel model;

			private _003C_003Ec__DisplayClass16_0 _003C_003E8__1;

			public EventForecastService _003C_003E4__this;

			private CrystalEventRosterConfig _003Croster_003E5__2;

			private bool _003CisRosterCrystal_003E5__3;

			private UniTask<ForecastBuildResult>.Awaiter _003C_003Eu__1;

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

		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CBuildForecastAndBakeRewardsIfNeededAsync_003Ed__15 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder _003C_003Et__builder;

			public EventModel model;

			public EventDifficulty difficulty;

			public EventForecastService _003C_003E4__this;

			private _003C_003Ec__DisplayClass15_0 _003C_003E8__1;

			private EventDifficultySettings _003CdifficultyDef_003E5__2;

			private UniTask<ForecastBuildResult>.Awaiter _003C_003Eu__1;

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

		private ForecastEventPlan? eventPlan;

		private EventsUd Ud => null;

		public string SnapshotDataValue
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		public bool HasEventPlan => false;

		public ForecastEventPlan? EventPlan => null;

		public bool TryApplyCrystalStages(EventModel model)
		{
			return false;
		}

		public ForecastEventEnemyInfo? GetEventEnemyInfo(EventUnitType unitType, EventModel currentEvent)
		{
			return null;
		}

		public ForecastEventRewardInfo? GetEventRewardInfo(EventUnitType unitType, EventModel currentEvent)
		{
			return null;
		}

		private ForecastEventEnemyInfo? GetMiniBossInfo(ForecastEventPlan plan, EventModel currentEvent)
		{
			return null;
		}

		public bool ShouldRecalculateRewards(EventModel model, EventDifficulty difficulty)
		{
			return false;
		}

		[AsyncStateMachine(typeof(_003CBuildForecastAndBakeRewardsIfNeededAsync_003Ed__15))]
		public UniTask BuildForecastAndBakeRewardsIfNeededAsync(EventModel model, EventDifficulty difficulty)
		{
			return default;
		}

		[AsyncStateMachine(typeof(_003CBuildEventPlanAsync_003Ed__16))]
		public UniTask<string> BuildEventPlanAsync(EventModel model)
		{
			return default;
		}

		private List<ForecastCrystalStagePlan> BuildCrystalStages(CrystalEventRosterConfig cfg, List<ForecastSnapshotExtended> snapshots, List<ForecastEventBossInfo> bosses, int rewardsLocationNumber, ItemsProvider itemsProvider)
		{
			return null;
		}

		private List<ForecastCrystalMilestonePlan> BuildCrystalMilestones(CrystalEventRosterConfig cfg, ForecastSnapshotExtended snap, ItemsProvider itemsProvider)
		{
			return null;
		}

		private static ForecastSnapshotExtended FindNearestSnapshot(List<ForecastSnapshotExtended> snapshots, float minute)
		{
			return default;
		}

		private List<ForecastCrystalRewardEntry> BuildCrystalRewards(CrystalEventRosterConfig cfg, int stageIndex, ForecastSnapshotExtended snap, ItemsProvider itemsProvider)
		{
			return null;
		}

		private List<ForecastCrystalRewardEntry> BuildCrystalRewards(IReadOnlyList<RewardBase> rewards, ForecastSnapshotExtended snap, ItemsProvider itemsProvider)
		{
			return null;
		}

		private void ProcessStatReward(StatReward statReward, ForecastSnapshotExtended snap, List<ForecastCrystalRewardEntry> results)
		{
		}

		private void ProcessItemReward(ItemReward itemReward, ItemsProvider itemsProvider, List<ForecastCrystalRewardEntry> results)
		{
		}

		public void ApplyBakedRewards(IReadOnlyList<RewardBase> rewards)
		{
		}

		public void ClearSnapshotData()
		{
		}

		private static double GetGpm(ForecastSnapshotExtended snap, StatType statType)
		{
			return 0.0;
		}
	}
}
