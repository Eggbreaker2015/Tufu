using System;
using System.Collections.Generic;
using Codebase.Rewards;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.SecretChest.Components
{
	[Serializable]
	public struct SecretChestComponent : IComponent
	{
		public string ChestKey;

		public Transform Root;

		public Transform AnchorPoint;

		public Transform CameraTarget;

		public Animator Animator;

		public GameObject ContentCube;

		public ParticleSystem GlowVfx;

		public float ShowDistance;

		public string ViewAddress;

		[SerializeReference]
		public List<RewardBase> Rewards;
	}
}
