using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Codebase.Simulation.Systems;
using Codebase.Unit;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;

namespace Codebase.Simulation.Forecast
{
	public static class ForecastApi
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CBuildAsync_003Ed__1 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder<ForecastBuildResult> _003C_003Et__builder;

			public Action<float> onProgress;

			public SimulationSystem simulationSystem;

			public int minutesAhead;

			public int stepMinutes;

			public ForecastAutoBalanceInput input;

			public ForecastEnemyPresets presets;

			public IReadOnlyList<StatType> rewardAllowedStats;

			public int snapshotsPerFrame;

			public CancellationToken cancellationToken;

			private List<ForecastSnapshotExtended> _003Csnapshots_003E5__2;

			private IEnumerator _003Ce_003E5__3;

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

		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CBuildEventPlanAsync_003Ed__0 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder<ForecastEventPlan> _003C_003Et__builder;

			public SimulationSystem simulationSystem;

			public int minutesAhead;

			public int stepMinutes;

			public ForecastAutoBalanceInput input;

			public ForecastEnemyPresets presets;

			public IReadOnlyList<StatType> rewardAllowedStats;

			public int snapshotsPerFrame;

			public Action<float> onProgress;

			public CancellationToken cancellationToken;

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

		[AsyncStateMachine(typeof(_003CBuildEventPlanAsync_003Ed__0))]
		public static UniTask<ForecastEventPlan> BuildEventPlanAsync(SimulationSystem simulationSystem, int minutesAhead, int stepMinutes, ForecastAutoBalanceInput input, ForecastEnemyPresets presets, IReadOnlyList<StatType> rewardAllowedStats, int snapshotsPerFrame = 1, Action<float> onProgress = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			return default;
		}

		[AsyncStateMachine(typeof(_003CBuildAsync_003Ed__1))]
		public static UniTask<ForecastBuildResult> BuildAsync(SimulationSystem simulationSystem, int minutesAhead, int stepMinutes, ForecastAutoBalanceInput input, ForecastEnemyPresets presets, IReadOnlyList<StatType> rewardAllowedStats, int snapshotsPerFrame = 1, Action<float> onProgress = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			return default;
		}
	}
}
