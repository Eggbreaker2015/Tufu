using System;
using UnityEngine;

namespace Codebase.Events
{
	[Serializable]
	public class CrystalRosterLeagueDef
	{
		public string Name;

		public string TitleColor;

		public string TextIcon;

		public int SnapshotIslandNumber;

		public int BossesToAdvance;

		public Sprite Icon;

		public Color innerBgColor;

		public Color frameBgColor;

		public int ForecastLocationNumber => this.SnapshotIslandNumber;
	}
}
