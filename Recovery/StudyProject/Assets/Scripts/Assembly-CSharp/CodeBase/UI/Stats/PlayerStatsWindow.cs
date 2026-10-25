using System.Collections.Generic;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Meta;
using Codebase.Player.Models;
using Codebase.Unit;
using Codebase.Utilities.ObjectPool;
using UnityEngine;

namespace Codebase.UI.Stats
{
	public class PlayerStatsWindow : View
	{
		[SerializeField]
		private PlayerStatView statView;

		[SerializeField]
		private Transform container;

		[SerializeField]
		private GameObject separator;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private MetaStatsModel metaStatsModel;

		[Inject]
		private MetaStatsConfig metaStatsConfig;

		private readonly ObjectPool<PlayerStatView> statViewPool;

		private readonly List<PlayerStatView> spawnedViews;

		private readonly List<GameObject> spawnedSeparators;

		private string visibleRowsSignature;

		private static readonly HashSet<StatType> ExcludeStats;

		private static readonly HashSet<StatType> SpecialStats;

		private void OnEnable()
		{
		}

		protected override void Render()
		{
		}

		private void RebuildContainer()
		{
		}

		private void SpawnSeparator()
		{
		}

		private void ClearContainer()
		{
		}

		private List<StatRowData> BuildRows()
		{
			return null;
		}

		private List<(MetaStat, MetaStatDef)> BuildMetaRows()
		{
			return null;
		}

		private bool IsMetaStatAllowed(string key)
		{
			return false;
		}

		private Dictionary<StatType, int> BuildStyleOrder()
		{
			return null;
		}

		private string BuildVisibleSignature()
		{
			return null;
		}

		private bool HasVisibleMetaValue(MetaStat stat, MetaStatDef def)
		{
			return false;
		}

		private double GetStatDisplayValue(Stat stat)
		{
			return 0.0;
		}

		private double GetStatValue(StatType type)
		{
			return 0.0;
		}
	}
}
