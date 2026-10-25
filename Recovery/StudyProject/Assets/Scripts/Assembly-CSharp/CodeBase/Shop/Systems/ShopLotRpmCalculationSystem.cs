using System.Collections.Generic;
using Codebase.Core;
using Codebase.Simulation.Systems;
using Codebase.UI.Shop;
using Codebase.Unit;
using Scellecs.Morpeh;

namespace Codebase.Shop.Systems
{
	public class ShopLotRpmCalculationSystem : SystemBase
	{
		[Inject]
		private ShopLotRepository shopLotRepository;

		[Inject]
		private SimulationSystem simulationSystem;

		private float timer;

		private const float RECALC_INTERVAL = 30f;

		private List<StatType> statTypesToProcess;

		private List<string> soulKeysToProcess;

		private int currentIndex;

		private bool isProcessing;

		public override void OnAwake()
		{
			timer = 0.5f;
			statTypesToProcess = new List<StatType>();
			soulKeysToProcess = new List<string>();
			currentIndex = 0;
			isProcessing = false;
			CollectStatTypes();
		}

		public override void OnUpdate(float deltaTime)
		{
			timer -= deltaTime;
			if (timer <= 0f)
			{
				timer = RECALC_INTERVAL;
				isProcessing = true;
				InitializeRecalculation();
			}
			if (isProcessing) ProcessStatCalculation();
		}

		private void CollectStatTypes()
		{
			var statTypes = new HashSet<StatType>();
			var soulKeys = new HashSet<string>();
			foreach (ShopLotModel lot in shopLotRepository.Values)
			{
				if (lot.Def.LotType == ShopLotType.Stat) statTypes.Add(lot.Def.StatType);
				if (lot.Def.LotType == ShopLotType.Soul) soulKeys.Add(lot.Def.SoulKey);
			}
			statTypesToProcess.AddRange(statTypes);
			soulKeysToProcess.AddRange(soulKeys);
		}

		private void InitializeRecalculation()
		{
			simulationSystem.CalculateSimulationData();
			currentIndex = 0;
		}

		private void ProcessStatCalculation()
		{
			if (currentIndex < statTypesToProcess.Count)
			{
				StatType statType = statTypesToProcess[currentIndex++];
				simulationSystem.CalculateRPMValue(statType);
				double value = simulationSystem.RPMCalculatedValue.GetValueOrDefault(statType, 0.0);
				foreach (ShopLotModel lot in shopLotRepository.Values)
					if (lot.Def.StatType == statType && lot.Def.LotType == ShopLotType.Stat) lot.RPM = value;
				return;
			}
			int soulIndex = currentIndex - statTypesToProcess.Count;
			if (soulIndex >= soulKeysToProcess.Count)
			{
				isProcessing = false;
				return;
			}
			string soulKey = soulKeysToProcess[soulIndex];
			simulationSystem.CalculateSoulRPMValue(soulKey);
			currentIndex++;
			double soulValue = simulationSystem.SoulRPMCalculatedValue[soulKey];
			foreach (ShopLotModel lot in shopLotRepository.Values)
				if (lot.Def.SoulKey == soulKey && lot.Def.LotType == ShopLotType.Soul) lot.RPM = soulValue;
		}
	}
}
