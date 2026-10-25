using Scellecs.Morpeh;

namespace Codebase.Utilities
{
	public static class StashExtensions
	{
		public static void SetIf<T>(this Stash<T> stash, Entity entity, bool condition)
			where T : struct, IComponent
		{
			if (condition)
				stash.Set(entity);
			else
				stash.Remove(entity);
		}

		public static void SetIf<T>(this Stash<T> stash, Entity entity, bool condition, in T data)
			where T : struct, IComponent
		{
			if (condition)
				stash.Set(entity, data);
			else
				stash.Remove(entity);
		}
	}
}
