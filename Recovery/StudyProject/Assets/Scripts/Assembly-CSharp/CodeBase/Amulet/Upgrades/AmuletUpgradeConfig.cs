using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Codebase.Amulet.Upgrades
{
	public class AmuletUpgradeConfig : ScriptableObject
	{
		public int Seed;

		[FormerlySerializedAs("NodeSpacingY")]
		[Tooltip("Желаемое расстояние между связанными нодами")]
		public float TargetEdgeLength;

		[FormerlySerializedAs("NodeFootprint")]
		[Tooltip("Минимальное расстояние между центрами любых двух нод")]
		public float MinimumNodeDistance;

		[Min(1f)]
		[Tooltip("Количество итераций стабилизации layout")]
		public int LayoutIterations;

		[FormerlySerializedAs("RandomOffsetX")]
		[Min(0f)]
		[Tooltip("Небольшое стартовое смещение, создающее органичную форму без изменения результата между открытиями")]
		public float InitialLayoutJitter;

		public float PathThickness;

		public float PathTileScale;

		public List<AmuletUpgradeDef> Upgrades;

		private void OpenGraphEditor()
		{
		}

		private void OpenUpgradesBuilder()
		{
		}

		private void SortUpgrades()
		{
		}

		public AmuletUpgradeDef GetUpgrade(string key)
		{
foreach (AmuletUpgradeDef definition in Upgrades) if (definition.Key == key) return definition;
            return null;
		}

		public List<AmuletUpgradeDef> GetRootUpgrades()
		{
var result = new List<AmuletUpgradeDef>();
            foreach (AmuletUpgradeDef definition in Upgrades) if (definition.RequiredUpgradeKeys == null || definition.RequiredUpgradeKeys.Count == 0) result.Add(definition);
            return result;
		}

		public List<AmuletUpgradeDef> GetChildUpgrades(string parentKey)
		{
var result = new List<AmuletUpgradeDef>();
            foreach (AmuletUpgradeDef definition in Upgrades) if (definition.RequiredUpgradeKeys != null && definition.RequiredUpgradeKeys.Contains(parentKey)) result.Add(definition);
            return result;
		}

		private static (int, int) ParseUpgradeOrder(string key)
		{
			return default;
		}
	}
}
