using System.Collections.Generic;
using Codebase.Core;
using Codebase.Meta;
using Scellecs.Morpeh;

namespace Codebase.Crystals.Systems
{
	public class CrystalPassBonusSystem : SystemBase
	{
		[Inject]
		private CrystalModel crystalModel;

		[Inject]
		private CrystalConfig config;

		[Inject]
		private MetaStatsModel metaStatsModel;

		private bool prevHasPass;

		private readonly List<(string statKey, string modName, float value)> bonusDefs;

		public override void OnAwake()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
		}

		private void Apply(bool active)
		{
		}

		private static void RemoveByName(MetaStat stat, string name)
		{
		}
	}
}
