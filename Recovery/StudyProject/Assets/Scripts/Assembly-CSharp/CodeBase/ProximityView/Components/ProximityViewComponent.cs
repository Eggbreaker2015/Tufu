using System;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.ProximityView.Components
{
	[Serializable]
	public struct ProximityViewComponent : IComponent
	{
		public Transform Root;

		public Transform AnchorPoint;

		public string ViewAddress;

		public float ShowDistance;
	}
}
