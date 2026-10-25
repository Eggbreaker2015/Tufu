using System;
using Codebase.Utilities;
using UnityEngine;

namespace Codebase.Meta
{
	[Serializable]
	public class MetaStatDef
	{
		[KeysDropdown(typeof(MetaStatKeys))]
		public string Key;

		public double BaseValue;

		public bool ShowInCalculator;

		public string NameKey;

		public MetaStatDisplayFormat Format;

		public Sprite Icon;
	}
}
