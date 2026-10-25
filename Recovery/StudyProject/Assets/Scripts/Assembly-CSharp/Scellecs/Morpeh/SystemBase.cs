using System;

namespace Scellecs.Morpeh
{
	public abstract class SystemBase : ISystem, IInitializer, IDisposable
	{
		public World World { get; set; }

		public FilterBuilder Filter => World.Filter;

		public abstract void OnAwake();

		public abstract void OnUpdate(float deltaTime);

		public virtual void Dispose()
		{
		}
	}
}
