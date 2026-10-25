using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.BossTower.Components
{
	public struct ScaleComponent : IComponent
	{
		public Vector3 OriginalScale;

		public bool keepScaleAfterDeath;
	}
}
