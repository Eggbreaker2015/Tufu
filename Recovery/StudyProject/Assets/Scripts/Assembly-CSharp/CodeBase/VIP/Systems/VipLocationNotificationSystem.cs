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
		}

		public override void Dispose()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
		}

		private void LoadPrefab()
		{
		}

		private VipLocationNotificationStateComponent Create(Entity entity)
		{
			return default;
		}

		private void Remove(ref VipLocationNotificationStateComponent state)
		{
		}
	}
}
