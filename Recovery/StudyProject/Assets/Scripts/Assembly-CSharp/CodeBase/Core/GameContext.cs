using System;
using System.Collections.Generic;

namespace Codebase.Core
{
	public static class GameContext
	{
		private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

		public static void Set<T>(T instance, bool useRuntimeType = false) where T : class
		{
			Type type = useRuntimeType ? instance.GetType() : typeof(T);
			_services[type] = instance;
			foreach (Type interfaceType in type.GetInterfaces())
			{
				_services[interfaceType] = instance;
			}
		}

		public static T Get<T>() where T : class
		{
			if (_services.TryGetValue(typeof(T), out object instance))
			{
				return (T)instance;
			}
			throw new Exception("[GameContext] No service found for type " + typeof(T).Name);
		}

		public static bool Has<T>() where T : class
		{
			return _services.ContainsKey(typeof(T));
		}

		public static void Clear()
		{
			_services.Clear();
		}

		[StaticReset]
		private static void ResetStaticState()
		{
			_services.Clear();
		}
	}
}
