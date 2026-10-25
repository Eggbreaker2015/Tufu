using System;
using System.Collections.Generic;
using Codebase.Utilities.ObjectPool;
using UniMob;
using UnityEngine;
using Codebase.Core;
using Codebase.Localization;

namespace Codebase.Core.UI
{
	public class ViewContainer<TView, TData> where TView : MonoBehaviour
	{
		private readonly Transform container;

		private readonly TView prefab;

		private readonly ObjectPool<TView> pool = new ObjectPool<TView>();

		private readonly List<TView> active = new List<TView>();

		private readonly List<TData> lastData = new List<TData>();

		private Lifetime lifetime;

		private LifetimeController reactionLifetimeController;

		private Func<IEnumerable<TData>> sourceFunc;

		private Func<object> triggerFunc;

		private Action<TView, TData> setupAction;

		private Action<TView, TData, int> setupActionWithIndex;

		private Action<TView> cleanupAction;

		private Func<TData, TData, bool> equalityComparer;

		private bool usePooling = true;

		private bool isStarted;

		private bool wasActive = true;

		private ViewContainerHelper helper;

		public IReadOnlyList<TView> Active => active;

		public ViewContainer(Transform container, TView prefab)
		{
			// Native 0x1953dbc and following original generic methods.
			this.container = container;
			this.prefab = prefab;
		}

		public ViewContainer<TView, TData> BindTo(Lifetime lifetime, Func<IEnumerable<TData>> source)
		{
			this.lifetime = lifetime;
			sourceFunc = source;
			triggerFunc = null;
			lifetime.Register(Clear);
			if (isStarted) RestartReaction();
			return this;
		}

		public ViewContainer<TView, TData> WatchFor<TTrigger>(Func<TTrigger> trigger)
		{
			triggerFunc = () => trigger();
			return this;
		}

		public ViewContainer<TView, TData> Setup(Action<TView, TData> setup)
		{
			setupAction = setup;
			StartIfReady();
			return this;
		}

		public ViewContainer<TView, TData> Setup(Action<TView, TData, int> setup)
		{
			setupActionWithIndex = setup;
			StartIfReady();
			return this;
		}

		private void StartIfReady()
		{
			if (isStarted || (setupAction == null && setupActionWithIndex == null) || sourceFunc == null) return;
			isStarted = true;
			helper = container.gameObject.AddComponent<ViewContainerHelper>();
			helper.OnEnabled += OnContainerEnabled;
			lifetime.Register(() =>
			{
				if (helper != null) UnityEngine.Object.Destroy(helper);
				if (reactionLifetimeController != null && !reactionLifetimeController.IsDisposed) reactionLifetimeController.Dispose();
			});
			StartReaction();
		}

		private void StartReaction()
		{
			reactionLifetimeController = new LifetimeController();
			Atom.Reaction(reactionLifetimeController.Lifetime, RebuildIfChanged);
		}

		private void RestartReaction()
		{
			if (reactionLifetimeController != null && !reactionLifetimeController.IsDisposed) reactionLifetimeController.Dispose();
			lastData.Clear();
			StartReaction();
		}

		private void OnContainerEnabled()
		{
			lastData.Clear();
			RebuildIfChanged();
		}

		public ViewContainer<TView, TData> OnCleanup(Action<TView> cleanup)
		{
			cleanupAction = cleanup;
			return this;
		}

		public ViewContainer<TView, TData> WithoutPooling()
		{
			usePooling = false;
			return this;
		}

		public ViewContainer<TView, TData> WithEqualityCheck(Func<TData, TData, bool> check)
		{
			equalityComparer = check;
			return this;
		}

		private void RebuildIfChanged()
		{
			List<TData> items;
			if (triggerFunc != null)
			{
				triggerFunc();
				items = GetCurrentItemsNoWatch();
			}
			else items = GetCurrentItems();
			if (!container.gameObject.activeInHierarchy)
			{
				wasActive = false;
				return;
			}
			if (!wasActive)
			{
				wasActive = true;
				lastData.Clear();
			}
			if (IsSameAsLast(items)) return;
			Rebuild(items);
			CacheLastData(items);
		}

		private List<TData> GetCurrentItems()
		{
			var items = new List<TData>();
			IEnumerable<TData> current = sourceFunc?.Invoke();
			if (current != null) foreach (TData item in current) items.Add(item);
			return items;
		}

		private List<TData> GetCurrentItemsNoWatch()
		{
			using (Atom.NoWatch) return GetCurrentItems();
		}

		private bool IsSameAsLast(List<TData> current)
		{
			if (current.Count != lastData.Count) return false;
			for (int index = 0; index < current.Count; index++)
				if (!AreEqual(current[index], lastData[index])) return false;
			return true;
		}

		private bool AreEqual(TData a, TData b)
		{
			// Native 0x1954ff8 boxes generic values then compares object identity.
			return equalityComparer != null ? equalityComparer(a, b) : ReferenceEquals(a, b);
		}

		private void CacheLastData(List<TData> items)
		{
			lastData.Clear();
			lastData.AddRange(items);
		}

		private void Rebuild(List<TData> items)
		{
			Clear();
			using (Atom.NoWatch)
			{
				for (int index = 0; index < items.Count; index++)
				{
					TView view = SpawnView();
					view.transform.SetSiblingIndex(index);
					Injector.InjectInto(view);
					LocalizationFontApplier.Register(view.gameObject);
					if (setupActionWithIndex != null) setupActionWithIndex(view, items[index], index);
					else setupAction?.Invoke(view, items[index]);
					active.Add(view);
				}
			}
		}

		private TView SpawnView()
		{
			return usePooling ? pool.Spawn(prefab, Vector3.zero, container) : UnityEngine.Object.Instantiate(prefab, container);
		}

		private void DespawnView(TView view)
		{
			cleanupAction?.Invoke(view);
			if (view is View reactiveView) reactiveView.Cleanup();
			else if (view is BaseView baseView) baseView.Cleanup();
			if (usePooling) pool.Despawn(view);
			else if (view != null) UnityEngine.Object.Destroy(view.gameObject);
		}

		public void Clear()
		{
			foreach (TView view in active) DespawnView(view);
			active.Clear();
			lastData.Clear();
		}

		public void ForceRebuild()
		{
			lastData.Clear();
			RebuildIfChanged();
		}

		public void Refresh()
		{
			RebuildIfChanged();
		}
	}
}
