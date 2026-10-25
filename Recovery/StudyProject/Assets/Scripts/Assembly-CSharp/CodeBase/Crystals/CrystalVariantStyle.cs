using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Codebase.Crystals
{
	[Serializable]
	public class CrystalVariantStyle
	{
		[FormerlySerializedAs("Variant")]
		public CrystalColor Color;

		public List<Sprite> SpritesByLevel;
	}
}
