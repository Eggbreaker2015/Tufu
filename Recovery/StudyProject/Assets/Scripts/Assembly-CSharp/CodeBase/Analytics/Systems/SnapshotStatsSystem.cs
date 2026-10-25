using System.Collections.Generic;
using Codebase.Core;
using Codebase.Player.Models;
using Codebase.Unit;
using Scellecs.Morpeh;

namespace Codebase.Analytics.Systems
{
	public class SnapshotStatsSystem : SystemBase
	{
		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private IAnalyticsWrapper analyticsWrapper;

		private const float Interval = 300f;

		private float elapsed;

		public override void OnAwake()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
		}

		private void SendSnapshot()
		{
		}

		private static string ResolveSourcePath(StatModifier modifier)
		{
			return null;
		}

		private static void InsertValue(Dictionary<string, object> root, string path, string statName, double value)
		{
		}
	}
}
