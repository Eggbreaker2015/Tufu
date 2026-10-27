using System;
using Codebase.Core;
using Codebase.SecretChest.Components;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.SecretChest.Systems
{
    public class SecretChestSpawnSystem : SystemBase
    {
        [Serializable]
        private struct SecretChestSpawnState : ISystemStateComponent, IComponent { }

        private SystemStateProcessor<SecretChestSpawnState> processor;
        [Inject] private Stash<SecretChestComponent> chestStash;
        [Inject] private Stash<SecretChestOpenedMarker> openedStash;
        [Inject] private SecretChestModel secretChestModel;
        private Filter chestFilter;
        private Filter openedFilter;

        public override void OnAwake()
        {
            // APK ARM64 0x14127e4.
            processor = Filter.With<SecretChestComponent>()
                .ToSystemStateProcessor<SecretChestSpawnState>(Create, Remove);
            chestFilter = Filter.With<SecretChestComponent>().Build();
            openedFilter = Filter.With<SecretChestComponent>().With<SecretChestOpenedMarker>().Build();
        }

        public override void Dispose() => processor.Dispose();

        public override void OnUpdate(float deltaTime)
        {
            // APK 0x1412a4c: include opened chests in the scene total.
            processor.Process();
            int count = 0;
            foreach (var entity in chestFilter) count++;
            secretChestModel.SetTotalCount(count);
            foreach (var entity in openedFilter)
            {
                ref var chest = ref chestStash.Get(entity);
                if (chest.Animator != null && !chest.Animator.GetBool(SecretChestAnimatorParams.Open))
                    chest.Animator.SetBool(SecretChestAnimatorParams.Open, true);
                if (chest.GlowVfx != null && chest.GlowVfx.isPlaying)
                    chest.GlowVfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private SecretChestSpawnState Create(Entity entity)
        {
            // APK 0x1412c70: restore visuals without granting rewards again.
            ref var chest = ref chestStash.Get(entity);
            bool opened = secretChestModel.IsOpened(chest.ChestKey);
            if (chest.Animator != null) chest.Animator.SetBool(SecretChestAnimatorParams.Open, opened);
            if (opened)
            {
                if (chest.GlowVfx != null) chest.GlowVfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                if (chest.ContentCube != null) chest.ContentCube.SetActive(false);
                openedStash.Set(entity);
            }
            else if (chest.GlowVfx != null) chest.GlowVfx.Play();
            return default;
        }

        private void Remove(ref SecretChestSpawnState state) { }
    }
}
