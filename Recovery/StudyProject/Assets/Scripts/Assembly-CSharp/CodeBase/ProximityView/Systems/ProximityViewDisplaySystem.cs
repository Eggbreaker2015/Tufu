using System;
using System.Collections.Generic;
using Codebase.Core;
using Codebase.ProximityView.Components;
using Codebase.UI.World;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.ProximityView.Systems
{
	public class ProximityViewDisplaySystem : SystemBase
	{
		[Serializable]
		private struct ProximityViewStateComponent : ISystemStateComponent, IComponent
		{
			public Entity Entity;

			public RectTransform View;

			public Transform AnchorPoint;
		}

		private SystemStateProcessor<ProximityViewStateComponent> processor;

		[Inject]
		private Stash<ProximityViewComponent> proximityViewStash;

		[Inject]
		private DynamicWorldCanvas worldCanvas;

		private readonly Dictionary<string, RectTransform> prefabCache;

		private readonly Dictionary<Entity, RectTransform> hidingViews;

		public override void OnAwake()
		{
		}

		public override void Dispose()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
		}

		private ProximityViewStateComponent Create(Entity entity)
		{
			return default;
		}

		private void Remove(ref ProximityViewStateComponent state)
		{
		}

		private void Despawn(RectTransform view, Transform anchor)
		{
		}

		private RectTransform GetOrLoadPrefab(string address)
		{
			return null;
		}
	}
}
