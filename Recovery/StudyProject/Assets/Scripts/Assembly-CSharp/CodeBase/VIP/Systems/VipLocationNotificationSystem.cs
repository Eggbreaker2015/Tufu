using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System;
using Codebase.Core;
using Codebase.UI.VIP;
using Codebase.UI.World;
using Codebase.VIP.Components;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.VIP.Systems
{
	public class VipLocationNotificationSystem : SystemBase
	{
		[Serializable]
		private struct VipLocationNotificationStateComponent : ISystemStateComponent, IComponent
		{
			public VipLocationNotificationView View;

			public Transform NotificationPoint;
		}

		private SystemStateProcessor<VipLocationNotificationStateComponent> processor;

		[Inject]
		private Stash<VipLocationComponent> vipLocationStash;

		[Inject]
		private DynamicWorldCanvas worldCanvas;

		private VipLocationNotificationView prefab;

		public override void OnAwake()
		{
            // Native 0x15bffa4.
            LoadPrefab();
            processor = Filter.With<VipLocationComponent>().Without<VipLocationUnlockedComponent>()
                .ToSystemStateProcessor<VipLocationNotificationStateComponent>(Create, Remove);
		}

		public override void Dispose()
		{
            // Native 0x15c0290 bounded tail is processor.Dispose().
            processor.Dispose();
		}

		public override void OnUpdate(float deltaTime)
		{
            // Native 0x15c02d8 skips until the async prefab has loaded.
            if (prefab == null) return;
            processor.Process();
		}

		private void LoadPrefab()
		{
            // Native 0x15c016c + callback0x15c0618.
            global::Codebase.Core.RecoveredAddressables.LoadAsync("Assets/Prefabs/UI/VipLocationNotification.prefab", result =>
            {
                if (result != null)
                    prefab = result.GetComponent<VipLocationNotificationView>();
            });
		}

		private VipLocationNotificationStateComponent Create(Entity entity)
		{
            // Native 0x15c0368: position at original notification point, then inject/set level/Initialize.
            if (prefab == null) return default;
            ref VipLocationComponent location = ref vipLocationStash.Get(entity);
            if (location.NotificationPoint == null) return default;
            VipLocationNotificationView view = worldCanvas.CreateNew(prefab);
            worldCanvas.AddTarget(location.NotificationPoint, view.transform as RectTransform);
            Injector.InjectInto(view);
            view.Level = location.LocationNumber;
            view.Initialize();
            return new VipLocationNotificationStateComponent { View = view, NotificationPoint = location.NotificationPoint };
		}

		private void Remove(ref VipLocationNotificationStateComponent state)
		{
            // Native 0x15c0540: Unity-null check, remove button listeners, remove tracking, despawn.
            if (state.View == null) return;
            state.View.Dispose();
            worldCanvas.RemoveTarget(state.NotificationPoint);
            worldCanvas.Despawn(state.View);
		}
	}
}
