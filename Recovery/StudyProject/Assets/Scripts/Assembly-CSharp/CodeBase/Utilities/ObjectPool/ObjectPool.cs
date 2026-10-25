using System.Collections.Generic;
using Codebase.Core;
using UnityEngine;

namespace Codebase.Utilities.ObjectPool
{
    [StaticResetIgnore]
    public class ObjectPool<T> where T : Component
    {
        private class SharedPool
        {
            public readonly List<T> Active = new List<T>();
            public readonly List<T> Inactive = new List<T>();
            public Transform Container;
            public string BaseName;
        }
        private static readonly Dictionary<int, SharedPool> _sharedPools = new Dictionary<int, SharedPool>();
        static ObjectPool()
        {
            // Native ELF VA 0x1a446e0.
            ObjectPoolStaticReset.Register(ResetStaticState);
        }
        public T Spawn(T prefab, Vector3 position, Transform parent = null)
        {
            // Native ELF VA 0x1a44868.
            return Spawn(prefab, position, Quaternion.identity, parent);
        }
        public T Spawn(T prefab, Vector3 position, Quaternion rotation, Transform parent)
        {
            // Native ELF VA 0x1a4490c: shared per T and source prefab.
            SharedPool pool = GetOrCreateSharedPool(GetKeyFromPrefab(prefab), prefab.name);
            T instance = GetInactiveObjectFromPool(pool);
            bool isRectTransform = prefab.transform is RectTransform;
            if (instance == null)
            {
                if (parent == null) { EnsureContainerExists(pool); parent = pool.Container; }
                instance = isRectTransform ? Object.Instantiate(prefab, parent, false) : Object.Instantiate(prefab, position, rotation, parent);
                AddPoolInstance(prefab.gameObject, instance.gameObject);
                pool.Active.Add(instance);
                if (isRectTransform)
                {
                    if (instance.transform is RectTransform rect) rect.anchoredPosition3D = position;
                    else instance.transform.localPosition = position;
                    instance.transform.localRotation = rotation;
                }
                return instance;
            }
            if (parent == null) { EnsureContainerExists(pool); parent = pool.Container; }
            instance.transform.SetParent(parent, false);
            if (isRectTransform)
            {
                if (instance.transform is RectTransform rect) rect.anchoredPosition3D = position;
                else instance.transform.localPosition = position;
                instance.transform.localRotation = rotation;
            }
            else { instance.transform.position = position; instance.transform.rotation = rotation; }
            instance.gameObject.SetActive(true);
            pool.Inactive.Remove(instance);
            pool.Active.Add(instance);
            return instance;
        }
        public void Despawn(T obj)
        {
            // Native ELF VA 0x1a44db4.
            obj.gameObject.SetActive(false);
            SharedPool pool = GetOrCreateSharedPool(GetKeyFromObject(obj));
            pool.Active.Remove(obj);
            pool.Inactive.Add(obj);
            EnsureContainerExists(pool);
            obj.transform.SetParent(pool.Container, false);
        }
        private void AddPoolInstance(GameObject prefab, GameObject obj)
        {
            // Native ELF VA 0x1a44e9c: original prefab GameObject hash.
            obj.AddComponent<PoolInstance>().SourcePrefabHash = prefab.GetHashCode();
        }
        private T GetInactiveObjectFromPool(SharedPool pool)
        {
            // Native ELF VA 0x1a44f14: FIFO, skip destroyed Unity objects.
            while (pool.Inactive.Count > 0)
            {
                T instance = pool.Inactive[0];
                pool.Inactive.RemoveAt(0);
                if (instance != null) return instance;
            }
            return null;
        }
        private static int GetKeyFromPrefab(T prefab)
        {
            // Native ELF VA 0x1a44fe4.
            int key = prefab.gameObject.GetHashCode();
            if (prefab.TryGetComponent(out PoolInstance instance)) key = instance.SourcePrefabHash;
            return key;
        }
        private static int GetKeyFromObject(T obj)
        {
            // Native ELF VA 0x1a45074.
            return obj.TryGetComponent(out PoolInstance instance) ? instance.SourcePrefabHash : obj.gameObject.GetHashCode();
        }
        private static SharedPool GetOrCreateSharedPool(int key, string baseNameIfNew = null)
        {
            // Native ELF VA 0x1a45100.
            if (_sharedPools.TryGetValue(key, out SharedPool pool)) return pool;
            pool = new SharedPool { BaseName = baseNameIfNew ?? "Pool" };
            _sharedPools[key] = pool;
            return pool;
        }
        private void EnsureContainerExists(SharedPool pool)
        {
            // Native ELF VA 0x1a45328: unnamed scene container in release.
            if (pool.Container != null) return;
            pool.Container = new GameObject().transform;
        }
        private void UpdateContainerName(SharedPool pool)
        {
            // Native ELF VA 0x1a453e8 is a single RET in the shipped build.
        }
        private static void ResetStaticState()
        {
            // Native ELF VA 0x1a453ec destroys retained containers only.
            foreach (SharedPool pool in _sharedPools.Values)
            {
                pool.Active.Clear();
                pool.Inactive.Clear();
                if (pool.Container != null) { Object.Destroy(pool.Container.gameObject); pool.Container = null; }
            }
            _sharedPools.Clear();
        }
    }
}
