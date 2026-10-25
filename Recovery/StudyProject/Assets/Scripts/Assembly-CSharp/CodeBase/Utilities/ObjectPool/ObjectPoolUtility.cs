using System.Collections.Generic;
using Codebase.Core;
using UnityEngine;

namespace Codebase.Utilities.ObjectPool
{
	[StaticResetIgnore]
	public static class ObjectPoolUtility<T> where T : Component
	{
		private static Dictionary<int, object> _pools;

		static ObjectPoolUtility()
		{
			// Native ELF VA 0x1a43ddc; reset registration is separate from ObjectPool<T>.
			_pools = new Dictionary<int, object>();
			ObjectPoolStaticReset.Register(ResetStaticState);
		}

		public static T Spawn(T prefab, Transform parent = null)
		{
			return GetPool(prefab).Spawn(prefab, Vector3.zero, parent);
		}

		public static T Spawn(T prefab, Vector3 position, Transform parent = null)
		{
			return GetPool(prefab).Spawn(prefab, position, parent);
		}

		public static T Spawn(T prefab, Vector3 position, Quaternion rotation, Transform parent = null)
		{
			return GetPool(prefab).Spawn(prefab, position, rotation, parent);
		}

		public static void Despawn(T obj)
		{
			GetPool(obj).Despawn(obj);
		}

		private static ObjectPool<T> GetPool(T prefab)
		{
			// Native ELF VA 0x1a442fc uses the source prefab hash for pooled instances.
			int key = prefab.gameObject.GetHashCode();
			if (prefab.TryGetComponent(out PoolInstance instance)) key = instance.SourcePrefabHash;
			if (!_pools.ContainsKey(key)) _pools[key] = new ObjectPool<T>();
			return (ObjectPool<T>)_pools[key];
		}

		private static void ResetStaticState()
		{
			_pools.Clear();
		}
	}
}
