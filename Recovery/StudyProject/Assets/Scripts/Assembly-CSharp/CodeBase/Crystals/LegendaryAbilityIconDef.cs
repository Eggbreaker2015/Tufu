using System;
using UnityEngine;

namespace Codebase.Crystals
{
	[Serializable]
	public class LegendaryAbilityIconDef
	{
		public LegendaryAbilityType Ability;

		public Sprite Sprite;

		public bool HideInGlobalStats;

		public bool HideInOwnStats;
	}
}
