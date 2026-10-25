using System;
using System.Collections.Generic;
using Codebase.Core;
using Codebase.SecretChest.Components;
using Codebase.UI.SecretChest;
using Codebase.UI.World;
using Scellecs.Morpeh;
using UnityEngine;

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

		[Inject]
		private Stash<SecretChestComponent> chestStash;

		[Inject]
		private DynamicWorldCanvas worldCanvas;

		private readonly Dictionary<string, SecretChestPromptView> prefabCache;

		private readonly Dictionary<Entity, SecretChestPromptView> hidingViews;

		public override void OnAwake()
		{
		}

		public override void Dispose()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
		}

		private SecretChestPromptState Create(Entity entity)
		{
			return default;
		}

		private void Remove(ref SecretChestPromptState state)
		{
		}

		private void Despawn(SecretChestPromptView view, Transform anchor)
		{
		}

		private SecretChestPromptView GetOrLoadPrefab(string address)
		{
			return null;
		}
	}
}
