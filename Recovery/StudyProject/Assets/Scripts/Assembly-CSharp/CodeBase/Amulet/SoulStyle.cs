using System;
using Codebase.Utilities;
using UnityEngine;

namespace Codebase.Amulet
{
	[Serializable]
	public class SoulStyle
	{
		[KeysDropdown(typeof(SoulKeys))]
		public string Key;

		public Sprite Icon;

		public Color Color;

		public bool HasGlow;
	}
}
