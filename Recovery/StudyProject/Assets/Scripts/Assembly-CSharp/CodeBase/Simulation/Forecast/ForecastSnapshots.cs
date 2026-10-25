using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Codebase.Simulation.Systems;
using Codebase.Unit;

namespace Codebase.Simulation.Forecast
{
	public static class ForecastSnapshots
	{
		[CompilerGenerated]
		private sealed class _003CBuildAsync_003Ed__1 : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			public SimulationSystem simulationSystem;

			public ForecastAutoBalanceInput input;

			public ForecastEnemyPresets presets;

			public IReadOnlyList<StatType> rewardAllowedStats;

			public List<ForecastSnapshotExtended> output;

			public Action<float> setProgress;

			public int snapshotsPerFrame;

			private IEnumerator _003Ce_003E5__2;

			object IEnumerator<object>.Current
			{
				[DebuggerHidden]
				get
				{
					return this._003C_003E2__current;
				}
			}

			object IEnumerator.Current
			{
				[DebuggerHidden]
				get
				{
					return this._003C_003E2__current;
				}
			}

			[DebuggerHidden]
			public _003CBuildAsync_003Ed__1(int _003C_003E1__state)
			{
			}

			[DebuggerHidden]
			void IDisposable.Dispose()
			{
			}

			private bool MoveNext()
			{
				return false;
			}

			bool IEnumerator.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				return this.MoveNext();
			}

			[DebuggerHidden]
			void IEnumerator.Reset()
			{
			}
		}

		[CompilerGenerated]
		private sealed class _003CBuildCore_003Ed__2 : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			public List<ForecastSnapshotExtended> output;

			public SimulationSystem simulationSystem;

			public Action<float> setProgress;

			public int snapshotsPerFrame;

			public ForecastAutoBalanceInput input;

			public ForecastEnemyPresets presets;

			public IReadOnlyList<StatType> rewardAllowedStats;

			public bool yieldBetweenSteps;

			private List<float> _003CbossMinutes_003E5__2;

			private List<float> _003CtimePoints_003E5__3;

			private int _003CtotalSteps_003E5__4;

			private int _003CstepIndex_003E5__5;

			private int _003Ci_003E5__6;

			object IEnumerator<object>.Current
			{
				[DebuggerHidden]
				get
				{
					return this._003C_003E2__current;
				}
			}

			object IEnumerator.Current
			{
				[DebuggerHidden]
				get
				{
					return this._003C_003E2__current;
				}
			}

			[DebuggerHidden]
			public _003CBuildCore_003Ed__2(int _003C_003E1__state)
			{
			}

			[DebuggerHidden]
			void IDisposable.Dispose()
			{
			}

			private bool MoveNext()
			{
				return false;
			}

			bool IEnumerator.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				return this.MoveNext();
			}

			[DebuggerHidden]
			void IEnumerator.Reset()
			{
			}
		}

		public static List<ForecastSnapshotExtended> Build(SimulationSystem simulationSystem, int minutesAhead, int stepMinutes, ForecastAutoBalanceInput input, ForecastEnemyPresets presets, IReadOnlyList<StatType> rewardAllowedStats)
		{
			return null;
		}

		[IteratorStateMachine(typeof(_003CBuildAsync_003Ed__1))]
		public static IEnumerator BuildAsync(SimulationSystem simulationSystem, int minutesAhead, int stepMinutes, ForecastAutoBalanceInput input, ForecastEnemyPresets presets, IReadOnlyList<StatType> rewardAllowedStats, List<ForecastSnapshotExtended> output, Action<float> setProgress, int snapshotsPerFrame = 1)
		{
			return null;
		}

		[IteratorStateMachine(typeof(_003CBuildCore_003Ed__2))]
		private static IEnumerator BuildCore(SimulationSystem simulationSystem, ForecastAutoBalanceInput input, ForecastEnemyPresets presets, IReadOnlyList<StatType> rewardAllowedStats, List<ForecastSnapshotExtended> output, Action<float> setProgress, int snapshotsPerFrame, bool yieldBetweenSteps)
		{
			return null;
		}

		private static SimulationSnapshot GetForecastSnapshot(SimulationSystem simulationSystem, float dayTiming, float eventMinute)
		{
			return default;
		}

		private static List<float> BuildTimePoints(ForecastAutoBalanceInput input)
		{
			return null;
		}

		public static ForecastEventPlan BuildEventPlan(IReadOnlyList<ForecastSnapshotExtended> snapshots, ForecastAutoBalanceInput input)
		{
			return default;
		}

		private static List<float> GetBossMinutes(ForecastAutoBalanceInput input)
		{
			return null;
		}

		private static ForecastEnemyPresets TunePresetsForBoss(ForecastEnemyPresets presets, ForecastAutoBalanceInput input, float progress, int bossIndex)
		{
			return default;
		}

		private static ForecastAutoBalancedEnemy FindEnemyOrDefault(ForecastSnapshotExtended snap, ForecastEnemyKind kind)
		{
			return default;
		}

		private static ForecastEventEnemyInfo ToEnemyInfo(float minute, ForecastEnemyKind kind, ForecastAutoBalanceInput input, ForecastAutoBalancedEnemy enemy)
		{
			return default;
		}

		private static ForecastEventBossInfo ToBossInfo(float minute, ForecastEnemyKind kind, ForecastAutoBalanceInput input, ForecastSnapshotExtended snap, ForecastAutoBalancedEnemy enemy)
		{
			return default;
		}

		private static List<ForecastAutoBalancedEnemy> BuildAutoBalancedEnemies(SimulationSnapshot snapshot, ForecastAutoBalanceInput input, ForecastEnemyPresets presets, float enemyStatsMultiplier, double playerAbilityDpsMultiplier, double playerAblityEhpMultiplier)
		{
			return null;
		}

		private static double GetAverageDamageRampMultiplier(double fightTimeSeconds, double delaySeconds, double percentPerSecond)
		{
			return 0.0;
		}

		private static (bool, StatType, float, double) BuildReward(IReadOnlyDictionary<StatType, double> rpm, float rewardMinutes, IReadOnlyList<StatType> rewardAllowedStats)
		{
			return default;
		}

		private static float GetRewardMinutes(ForecastAutoBalanceInput input, float progress)
		{
			return 0f;
		}
	}
}
