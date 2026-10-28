using Codebase.Utilities.ObjectPool;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;
using UnityEngine;

namespace Codebase.Core.UI
{
	public class UINavigator : MonoBehaviour
	{
		private class PopupEntry
		{
			public PopupBackground BackgroundInstance;
		}

		[SerializeField]
		private Transform popupContainer;

		private readonly string backgroundKey = "Assets/Prefabs/UI/Background.prefab";

		private readonly Dictionary<string, PopupView> prefabCache = new Dictionary<string, PopupView>();

		private readonly Dictionary<PopupView, PopupEntry> activePopups = new Dictionary<PopupView, PopupEntry>();

		private readonly HashSet<int> injectedInstances = new HashSet<int>();

		private PopupBackground backgroundPrefab;

		public bool HasActivePopups => activePopups.Count > 0;

		public int ActivePopupCount => activePopups.Count;

		public bool TryGetActive<T>(out T view) where T : PopupView
		{
			// Native ELF VA 0x17d2220; inspect keys in original dictionary order.
			foreach (PopupView popup in activePopups.Keys)
			{
			    if (popup is T match)
			    {
			        view = match;
			        return true;
			    }
			}
			view = null;
			return false;
		}

		private void Awake()
		{
            global::Codebase.Core.GameContext.Set<UINavigator>(this);
        }

		public T Open<T>(string key, bool withBackground = false) where T : PopupView
		{
			// Native ELF VA 0x17d2124.
			T prefab = GetOrLoadPrefab<T>(key);
			if (prefab == null) return null;
			return (T)OpenInternal(prefab, withBackground);
		}

		public PopupView Open(string key, bool withBackground = false)
		{
			// Native ELF VA 0x1549064; bounded body tail invokes OpenInternal.
			PopupView prefab = GetOrLoadPrefab(key);
			if (prefab == null) return null;
			return OpenInternal(prefab, withBackground);
		}

		private PopupView OpenInternal(PopupView prefab, bool withBackground)
		{
			// Native ELF VA 0x154936c; original order is background, pooled popup, injection, tracking, Initialize.
			PopupBackground background = withBackground ? SpawnBackground() : null;
			if (background != null) background.transform.SetAsLastSibling();
			PopupView instance = ObjectPoolUtility<PopupView>.Spawn(prefab, popupContainer);
			ResetRectTransform(instance.transform as RectTransform);
			instance.transform.SetAsLastSibling();
			InjectIfNeeded(instance);
			activePopups[instance] = new PopupEntry { BackgroundInstance = background };
			instance.SetCloseCallback(() => ClosePopup(instance));
			instance.Initialize();
			return instance;
		}

		private void InjectIfNeeded(PopupView instance)
		{
			// Native ELF VA 0x1549ba0. Unity 6000.6 replaces old GetInstanceID with EntityId hashing.
			int id = instance.GetEntityId().GetHashCode();
			if (injectedInstances.Contains(id)) return;
			Injector.InjectInto(instance);
			injectedInstances.Add(id);
		}

		private T GetOrLoadPrefab<T>(string key) where T : PopupView
		{
			// Native ELF VA 0x17d1e44; cache hit casts to T, preserving native invalid-cast behavior.
			if (prefabCache.TryGetValue(key, out PopupView cached)) return (T)cached;
			GameObject obj = RecoveredAddressables.Load(key);
			if (obj == null)
			{
			    Debug.LogError("[UINavigator] Failed to load prefab: " + key);
			    return null;
			}
			T prefab = obj.GetComponent<T>();
			if (prefab == null)
			{
			    Debug.LogError("[UINavigator] Prefab '" + key + "' does not have component " + typeof(T).Name);
			    return null;
			}
			prefabCache[key] = prefab;
			return prefab;
		}

		private PopupView GetOrLoadPrefab(string key)
		{
			// Native ELF VA 0x154910c; exact original error strings resolved through ELF RELA.
			if (prefabCache.TryGetValue(key, out PopupView prefab)) return prefab;
			GameObject obj = RecoveredAddressables.Load(key);
			if (obj == null)
			{
			    Debug.LogError("[UINavigator] Failed to load prefab: " + key);
			    return null;
			}
			prefab = obj.GetComponent<PopupView>();
			if (prefab == null)
			{
			    Debug.LogError("[UINavigator] Prefab '" + key + "' does not have PopupView component");
			    return null;
			}
			prefabCache[key] = prefab;
			return prefab;
		}

		private PopupBackground SpawnBackground()
		{
			// Native ELF VA 0x1549610.
			if (backgroundPrefab == null)
			{
			    GameObject obj = RecoveredAddressables.Load(backgroundKey);
			    if (obj == null)
			    {
			        Debug.LogError("[UINavigator] Failed to load background prefab: " + backgroundKey);
			        return null;
			    }
			    backgroundPrefab = obj.GetComponent<PopupBackground>();
			    if (backgroundPrefab == null)
			    {
			        Debug.LogError("[UINavigator] Background prefab does not have PopupBackground component");
			        return null;
			    }
			}
			PopupBackground instance = ObjectPoolUtility<PopupBackground>.Spawn(backgroundPrefab, popupContainer);
			ResetRectTransform(instance.transform as RectTransform);
			return instance;
		}

		private static void ResetRectTransform(RectTransform rect)
		{
			// Native ELF VA 0x15498d0; retain anchor/pivot and clear stretch-axis offsets only.
			if (rect == null) return;
			rect.localScale = Vector3.one;
			rect.localRotation = Quaternion.identity;
			rect.anchoredPosition = Vector2.zero;
			if (!Mathf.Approximately(rect.anchorMin.x, rect.anchorMax.x))
			{
			    rect.offsetMin = new Vector2(0f, rect.offsetMin.y);
			    rect.offsetMax = new Vector2(0f, rect.offsetMax.y);
			}
			if (!Mathf.Approximately(rect.anchorMin.y, rect.anchorMax.y))
			{
			    rect.offsetMin = new Vector2(rect.offsetMin.x, 0f);
			    rect.offsetMax = new Vector2(rect.offsetMax.x, 0f);
			}
		}

		private void ClosePopup(PopupView instance)
		{
			// Native ELF VA 0x1549c84.
			if (!activePopups.TryGetValue(instance, out PopupEntry entry)) return;
			instance.Cleanup();
			ObjectPoolUtility<PopupView>.Despawn(instance);
			if (entry.BackgroundInstance != null) ObjectPoolUtility<PopupBackground>.Despawn(entry.BackgroundInstance);
			activePopups.Remove(instance);
		}
	}
}
