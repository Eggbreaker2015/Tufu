using System;
using System.Runtime.InteropServices;
using Codebase.Core;
using Codebase.SecretChest.Components;
using Scellecs.Morpeh;

namespace Codebase.SecretChest.Systems
{
	public class SecretChestSpawnSystem : SystemBase
	{
		[Serializable]
		[StructLayout((LayoutKind)0, Size = 1)]
		private struct SecretChestSpawnState : ISystemStateComponent, IComponent
		{
		}

		private SystemStateProcessor<SecretChestSpawnState> processor;

		[Inject]
		private Stash<SecretChestComponent> chestStash;

		[Inject]
		private Stash<SecretChestOpenedMarker> openedStash;

		[Inject]
		private SecretChestModel secretChestModel;

		private Filter openedFilter;

		public override void OnAwake()
		{
		}

		public override void Dispose()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
		}

		private SecretChestSpawnState Create(Entity entity)
		{
			return default;
		}

		private void Remove(ref SecretChestSpawnState state)
		{
		}
	}
}
