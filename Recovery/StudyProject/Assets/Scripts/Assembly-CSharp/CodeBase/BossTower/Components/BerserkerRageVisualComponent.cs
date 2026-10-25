using System;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.BossTower.Components
{
	[Serializable]
	public struct BerserkerRageVisualComponent : IComponent
	{
		public GameObject[] RageLevelObjects;

		public MeshBlinkEffect BlinkEffect;
	}
}
