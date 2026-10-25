using System;
using Codebase.Player.Inventory;
using TMPro;
using UnityEngine;

namespace Codebase.Player
{
	[Serializable]
	public class ItemRarityStyle
	{
		public Rarity Rarity;

		public Sprite Sprite;

		public Sprite EvolvedSprite;

		public bool UseLargeRarityBg;

		public bool HasGradient;

		public VertexGradient Gradient;
	}
}
