using System;
using System.Collections.Generic;
using Codebase.Core;
using Codebase.Player.Models;
using Codebase.Utilities.ObjectPool;
using UnityEngine;

namespace Codebase.UI.World
{
	public class DynamicWorldCanvas : MonoBehaviour
	{
		[Serializable]
		public class CanvasTarget
		{
			public Transform target;

			public RectTransform uiElement;
		}

		[Inject]
		private PlayerModel playerModel;

		[SerializeField]
		private UnitDamageView prefab;

		public UnityEngine.Camera mainCamera;

		public RectTransform canvasRect;

		private CanvasGroup canvasGroup;

		private bool appliedVisible = true;

		private readonly List<CanvasTarget> targets = new List<CanvasTarget>();

		private readonly List<CanvasTarget> elementsToRemove = new List<CanvasTarget>();

		private readonly Dictionary<Component, object> componentToPrefabMap = new Dictionary<Component, object>();

		private readonly Dictionary<object, object> pools = new Dictionary<object, object>();

		private void Awake()
		{
			// Native ELF VA 0x1600188.
			GameContext.Set(this);
			canvasGroup = GetComponent<CanvasGroup>();
			if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
		}

		private void LateUpdate()
		{
			// Native ELF VA 0x16002b8. Invalid targets are removed after enumeration.
			bool visible = GameContext.Get<UiModel>().IsWorldUiVisible;
			if (appliedVisible != visible)
			{
				ApplyVisible(visible);
				appliedVisible = visible;
			}
			if (!visible) return;
			foreach (CanvasTarget target in targets)
			{
				if (target.uiElement == null || target.target == null)
				{
					elementsToRemove.Add(target);
					continue;
				}
				Vector3 screen = mainCamera.WorldToScreenPoint(target.target.position);
				target.uiElement.position = new Vector3(screen.x, screen.y, 0f);
			}
			foreach (CanvasTarget target in elementsToRemove) targets.Remove(target);
			elementsToRemove.Clear();
		}

		public T CreateNew<T>(T prefab) where T : Component
		{
			// Native ELF VA 0x17508d8 (shared generic body).
			if (!pools.TryGetValue(prefab, out object value))
			{
				value = new ObjectPool<T>();
				pools[prefab] = value;
			}
			T instance = ((ObjectPool<T>)value).Spawn(prefab, Vector3.zero, Quaternion.identity, transform);
			componentToPrefabMap[instance] = prefab;
			return instance;
		}

		public void Despawn<T>(T instance) where T : Component
		{
			// Native ELF VA 0x1750b58.
			if (!componentToPrefabMap.TryGetValue(instance, out object prefabKey))
			{
				Destroy(instance.gameObject);
				return;
			}
			if (!pools.TryGetValue(prefabKey, out object value)) return;
			((ObjectPool<T>)value).Despawn(instance);
			componentToPrefabMap.Remove(instance);
		}

		public void AddTarget(Transform targetTransform, RectTransform uiElement)
		{
			// Native ELF VA 0x1600804.
			targets.Add(new CanvasTarget { target = targetTransform, uiElement = uiElement });
		}

		public void RemoveTarget(Transform targetTransform)
		{
			// Native ELF VA 0x15fbe40; remove all matches in reverse order.
			for (int i = targets.Count - 1; i >= 0; i--)
				if (targets[i].target == targetTransform) targets.RemoveAt(i);
		}

		public void ClearAll()
		{
			// Native ELF VA 0x16008c0 only clears the follow list, retaining pools.
			targets.Clear();
		}

		private void ApplyVisible(bool visible)
		{
			// Native ELF VA 0x16007a0.
			canvasGroup.alpha = visible ? 1f : 0f;
			canvasGroup.interactable = visible;
			canvasGroup.blocksRaycasts = visible;
		}
	}
}
