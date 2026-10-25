using System;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.Vfx.Components
{
	public struct WorldVfxTravelComponent : IComponent
	{
		public ParticleSystem ParticleSystem;

		public Transform Target;

		public Vector3 Offset;

		public float Speed;

		public Action OnArrived;
	}
}
