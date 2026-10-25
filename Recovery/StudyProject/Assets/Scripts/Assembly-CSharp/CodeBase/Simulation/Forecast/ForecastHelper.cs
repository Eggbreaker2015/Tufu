using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Codebase.Simulation.Systems;
using Codebase.Unit;
using UnityEngine;

namespace Codebase.Simulation.Forecast
{
	public class ForecastHelper : MonoBehaviour
	{
		[CompilerGenerated]
		private sealed class _003CBuildRoutine_003Ed__23 : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			public ForecastHelper _003C_003E4__this;

			public SimulationSystem system;

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
			public _003CBuildRoutine_003Ed__23(int _003C_003E1__state)
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

		[SerializeField]
		private int minutesAhead;

		[SerializeField]
		private int stepMinutes;

		[SerializeField]
		private int snapshotsPerFrame;

		[SerializeField]
		private ForecastAutoBalanceInput autoBalanceInput;

		[SerializeField]
		private ForecastEnemyPresets enemyPresets;

		[SerializeField]
		private StatType[] rewardAllowedStats;

		public List<ForecastSnapshotExtended> Snapshots;

		[SerializeField]
		private ForecastEventPlan eventPlan;

		private Coroutine routine;

		public bool IsBuilding { get; private set; }

		public float Progress { get; private set; }

		public ForecastEventPlan EventPlan => default;

		public void SetEventSettings(float grandBossMinute, float lastBossMinute, int bossCount, float bossGamma)
		{
		}

		public void BuildForecast()
		{
		}

		public void BuildForecastAsync()
		{
		}

		public void Cancel()
		{
		}

		[IteratorStateMachine(typeof(_003CBuildRoutine_003Ed__23))]
		private IEnumerator BuildRoutine(SimulationSystem system)
		{
			return null;
		}

		private static SimulationSystem GetSimulationSystem()
		{
			return null;
		}
	}
}
