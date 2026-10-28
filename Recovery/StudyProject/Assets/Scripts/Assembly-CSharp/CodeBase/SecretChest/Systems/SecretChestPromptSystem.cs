using System;
using System.Collections.Generic;
using Codebase.Core;
using Codebase.ProximityView;
using Codebase.SecretChest.Components;
using Codebase.UI.SecretChest;
using Codebase.UI.World;
using Scellecs.Morpeh;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Codebase.SecretChest.Systems
{
    public class SecretChestPromptSystem : SystemBase
    {
        [Serializable]
        private struct SecretChestPromptState : ISystemStateComponent, IComponent
        {
            public Entity Entity;
            public SecretChestPromptView View;
            public Transform AnchorPoint;
        }

        private SystemStateProcessor<SecretChestPromptState> processor;
        [Inject] private Stash<SecretChestComponent> chestStash;
        [Inject] private DynamicWorldCanvas worldCanvas;
        private readonly Dictionary<string, SecretChestPromptView> prefabCache = new Dictionary<string, SecretChestPromptView>();
        private readonly Dictionary<Entity, SecretChestPromptView> hidingViews = new Dictionary<Entity, SecretChestPromptView>();
        private bool disposing;

        public override void OnAwake()
        {
            // APK ARM64 0x141184c: opened chests also keep the nearby found-count view.
            processor = Filter.With<SecretChestComponent>().With<SecretChestNearMarker>()
                .ToSystemStateProcessor<SecretChestPromptState>(Create, Remove);
        }

        public override void Dispose()
        {
            // Editor adaptation: finish recycling while the canvas still exists.
            disposing = true;
            processor.Dispose();
            foreach (var view in hidingViews.Values)
            {
                if (view == null) continue;
                if (view.TryGetComponent<ProximityViewHideAnimation>(out var animation)) animation.Show();
                view.Dispose();
                if (worldCanvas != null) worldCanvas.Despawn(view);
            }
            hidingViews.Clear();
        }

        public override void OnUpdate(float deltaTime) => processor.Process();

        private SecretChestPromptState Create(Entity entity)
        {
            // APK 0x1411a9c: re-entry cancels a pending hide and reuses that same view.
            ref var chest = ref chestStash.Get(entity);
            SecretChestPromptView view;
            if (hidingViews.TryGetValue(entity, out view) && view != null)
            {
                hidingViews.Remove(entity);
                if (view.TryGetComponent<ProximityViewHideAnimation>(out var animation)) animation.Show();
            }
            else
            {
                hidingViews.Remove(entity);
                view = worldCanvas.CreateNew(GetOrLoadPrefab(chest.ViewAddress));
                view.gameObject.SetActive(true);
                worldCanvas.AddTarget(chest.AnchorPoint, view.transform as RectTransform);
            }
            Injector.InjectInto(view);
            view.Setup(chest.ChestKey);
            return new SecretChestPromptState { Entity = entity, View = view, AnchorPoint = chest.AnchorPoint };
        }

        private void Remove(ref SecretChestPromptState state)
        {
            // APK 0x1411e74 / callback 0x1412160.
            SecretChestPromptView view = state.View;
            Transform anchor = state.AnchorPoint;
            Entity entity = state.Entity;
            if (view == null) return;
            if (!disposing && view.TryGetComponent<ProximityViewHideAnimation>(out var animation))
            {
                hidingViews[entity] = view;
                animation.Hide(() =>
                {
                    if (!hidingViews.TryGetValue(entity, out var current) || current != view) return;
                    hidingViews.Remove(entity);
                    view.Dispose();
                    Despawn(view, anchor);
                });
            }
            else
            {
                if (view.TryGetComponent<ProximityViewHideAnimation>(out var pending)) pending.Show();
                view.Dispose();
                Despawn(view, anchor);
            }
        }

        private void Despawn(SecretChestPromptView view, Transform anchor)
        {
            // APK 0x141200c.
            if (worldCanvas == null) return;
            worldCanvas.RemoveTarget(anchor);
            worldCanvas.Despawn(view);
        }

        private SecretChestPromptView GetOrLoadPrefab(string address)
        {
            // APK 0x1411d1c: preserve the original Addressables asset and synchronous load.
            if (prefabCache.TryGetValue(address, out var prefab)) return prefab;
            var asset = global::Codebase.Core.RecoveredAddressables.Load(address);
            if (asset == null || !asset.TryGetComponent<SecretChestPromptView>(out prefab))
                throw new InvalidOperationException("Secret chest prompt prefab is missing: " + address);
            prefabCache[address] = prefab;
            return prefab;
        }
    }
}
