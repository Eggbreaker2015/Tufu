using Codebase.BossTower.Components;
using UnityEngine;

namespace Codebase.BossTower
{
	public class BoneEffectsAvatar : MonoBehaviour
	{
		[SerializeField]
		private BoneGroup[] groups;
		public BoneGroup[] Groups => this.groups;

		private void Awake()
		{
			foreach (BoneGroup group in groups)
				group.Instances = new ParticleSystem[group.Bones.Length];
		}

		public BoneGroup GetGroup(BoneGroupType type)
		{
			foreach (BoneGroup group in groups)
				if (group.Type == type)
					return group;
			return null;
		}
	}
}
