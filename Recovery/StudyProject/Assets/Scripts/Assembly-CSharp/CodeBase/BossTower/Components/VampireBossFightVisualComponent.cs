using System;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.BossTower.Components
{
	[Serializable]
	public struct VampireBossFightVisualComponent : IComponent
	{
		public VampireSpawnPointEntry[] SpawnPoints;

		public Transform CenterPoint;

		public GameObject CenterVisual;
	}
}
