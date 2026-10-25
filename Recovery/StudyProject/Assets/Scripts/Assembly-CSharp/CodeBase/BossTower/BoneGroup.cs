using System;
using Codebase.BossTower.Components;
using UnityEngine;

namespace Codebase.BossTower
{
	[Serializable]
	public class BoneGroup
	{
		public BoneGroupType Type;

		public Transform[] Bones;

		[NonSerialized]
		public ParticleSystem[] Instances;
	}
}
