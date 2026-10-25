using Codebase.Utilities.ObjectPool;
using DG.Tweening;
using Codebase.BossTower.Components;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.BossTower.Systems
{
	public class BoneEffectSlotsSystem : SystemBase
	{
		private const float ScaleAnimDuration = 0.6f;
		public override void OnAwake()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
		}

		public void ApplyEffect(BoneEffectsAvatar avatar, BoneGroupType groupType, ParticleSystem prefab, int stackCount, float scaleStep)
		{
            // Original ELF VA 0x15607a0, bounded ARM64/C agree. Missing group returns; null avatar is not silently ignored.
            BoneGroup group = avatar.GetGroup(groupType);
            if (group == null) return;
            Vector3 targetScale = Vector3.one * (1f + stackCount * scaleStep);
            for (int i = 0; i < group.Bones.Length; i++)
            {
                if (group.Instances[i] == null)
                {
                    ParticleSystem instance = ObjectPoolUtility<ParticleSystem>.Spawn(prefab, Vector3.zero, group.Bones[i]);
                    group.Instances[i] = instance;
                    instance.transform.localPosition = Vector3.zero;
                    instance.transform.localRotation = Quaternion.identity;
                    instance.transform.localScale = Vector3.one;
                    instance.Clear(true);
                    instance.Play(true);
                }
                // Original does not kill earlier scale tweens on repeated applications.
                group.Instances[i].transform.DOScale(targetScale, ScaleAnimDuration).SetEase((Ease)30);
            }
        }

		public void ClearAllEffects(BoneEffectsAvatar avatar)
		{
			if (avatar == null)
				return;
			foreach (var group in avatar.Groups)
			{
				for (int i = 0; i < group.Instances.Length; i++)
				{
					if (group.Instances[i] == null)
						continue;
					Codebase.Utilities.ObjectPool.ObjectPoolUtility<ParticleSystem>.Despawn(group.Instances[i]);
					group.Instances[i] = null;
				}
			}
		}
	}
}
