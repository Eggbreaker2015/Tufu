using System.Collections.Generic;
using Codebase.Core;
using Codebase.Simulation.Systems;
using Codebase.Utilities.ObjectPool;
using UnityEngine;

namespace Codebase.UI
{
	public class EventCurrencyRewardContainer : MonoBehaviour
	{
		[SerializeField]
		private StatRewardView prefab;

		[SerializeField]
		private float verticalSpacing;

		[SerializeField]
		private int maxActiveRewards;

		[Inject]
		private SimulationSystem simulationSystem;

		private readonly List<StatRewardView> activeRewards;

		private readonly Queue<string> pending;

		private readonly ObjectPool<StatRewardView> pool;

		private void Awake()
		{
            global::Codebase.Core.GameContext.Set<EventCurrencyRewardContainer>(this);
        }

		public void Create(string value)
		{
		}

		private void Reposition()
		{
		}

		private void SpawnReward(string value)
		{
		}

		private void TrySpawnFromQueue()
		{
		}
	}
}
