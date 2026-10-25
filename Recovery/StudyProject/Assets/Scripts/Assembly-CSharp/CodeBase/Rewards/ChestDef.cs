using System;
using System.Collections.Generic;
using Codebase.Utilities;
using UnityEngine;

namespace Codebase.Rewards
{
	[Serializable]
	public class ChestDef
	{
		[KeysDropdown(typeof(ChestKeys))]
		public string Key;

		public List<ChestDropDef> Drops;

		public Sprite ClosedSprite;

		public Sprite OpenedSprite;
	}
}
