using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.Map.Components
{
	public struct PointOfInterestComponent : IComponent
	{
		public Sprite Sprite;

		public Color Color;

		public float Size;

		public float HightlightAdditionalSize;

		public Vector3 WorldPosition;

		public int Priority;

		public float RemainingCooldownPercent;

		public PointOfInterest Reference;

		public bool IsDirty;

		public int SourceVersion;

		public bool isHighlighted;

		public Color highlightColor;
	}
}
