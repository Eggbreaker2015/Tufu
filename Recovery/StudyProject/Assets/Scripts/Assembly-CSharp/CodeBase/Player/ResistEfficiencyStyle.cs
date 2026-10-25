using System;
using Codebase.Enemy;
using UnityEngine;

namespace Codebase.Player
{
	[Serializable]
	public class ResistEfficiencyStyle
	{
		[Serializable]
		public class EfficiencyGroup
		{
			public Sprite Low;

			public Sprite Medium;

			public Sprite High;

			public Sprite Neutral;
		}

		public WeaponType WeaponType;

		public EfficiencyGroup Effective;

		public EfficiencyGroup Ineffective;
	}
}
