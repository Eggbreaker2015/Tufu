#if UNITY_EDITOR
// Learning-project Editor teardown bridge. Clear the exact restored activity
// containers while their scenes still exist; never close a popup or claim rewards.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Codebase.Core.UI;
using Codebase.Events.Models;
using Codebase.Rewards;
using Codebase.Simulation.Forecast;
using Codebase.UI.Events;
using UnityEditor;
using UnityEngine;

namespace RecoveryCompatibility
{
    [InitializeOnLoad]
    public static class EditorEventViewLifecycle
    {
        private sealed class ContainerBinding
        {
            public Type OwnerType;
            public FieldInfo Field;
            public Action<object> Clear;
            public Func<object, int> Count;
        }

        private sealed class ClearJob
        {
            public MonoBehaviour Owner;
            public ContainerBinding Binding;
            public object Container;
            public int Depth;
        }

        // Explicit whitelist, with exact generic field type checked at startup.
        // No discovery or invocation of unknown fields, Cleanup, Close or claims.
        private static readonly ContainerBinding[] Bindings =
        {
            Bind<RosterEventStartView, RosterStageView, int>("stagesViewContainer"),
            Bind<RosterEventStartView, RewardView, RewardBase>("rewardsViewContainer"),
            Bind<RosterEventProgressView, EventCurrencyView, string>("currenciesViewContainer"),
            Bind<RosterEventProgressView, EventBoosterView, EventBoosterModel>("boostersViewContainer"),
            Bind<RosterEventProgressView, RosterStageView, int>("stagesViewContainer"),
            Bind<RosterEventEndView, RosterStageView, int>("stagesViewContainer"),
            Bind<RosterEventRewardsView, RosterStageView, int>("stagesViewContainer"),
            Bind<RosterEventTimeIsOverView, RosterStageView, int>("stagesViewContainer"),
            Bind<RosterStageView, RewardView, RewardBase>("rewardsViewContainer"),
            Bind<RosterEventMilestoneView, RewardView, RewardBase>("rewardsViewContainer"),
            Bind<RosterMilestoneChestsPanel, RosterEventMilestoneView, ForecastCrystalMilestonePlan>("milestoneChestViewsContainer"),
            Bind<RosterMilestoneChestsPanel, RosterEventMilestoneNavButton, ForecastCrystalMilestonePlan>("milestoneNavButtonsContainer"),
        };

        private static bool quitting;
        public static int LastMatchedOwners { get; private set; }
        public static int LastClearedContainers { get; private set; }
        public static int LastClearedItems { get; private set; }
        public static string LastQuitUtc { get; private set; }

        static EditorEventViewLifecycle()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            Subscribe();
        }

        private static ContainerBinding Bind<TOwner, TView, TData>(string fieldName)
            where TOwner : MonoBehaviour where TView : MonoBehaviour
        {
            var field = typeof(TOwner).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                ?? throw new MissingFieldException(typeof(TOwner).FullName, fieldName);
            var expected = typeof(ViewContainer<TView, TData>);
            if (field.FieldType != expected)
                throw new InvalidOperationException(typeof(TOwner).FullName + "." + fieldName + " must be " + expected.FullName);
            return new ContainerBinding
            {
                OwnerType = typeof(TOwner),
                Field = field,
                Clear = value => ((ViewContainer<TView, TData>)value).Clear(),
                Count = value => ((ViewContainer<TView, TData>)value).Active.Count,
            };
        }

        private static void Subscribe()
        {
            Application.quitting -= OnQuitting;
            Application.quitting += OnQuitting;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                quitting = false;
                Subscribe();
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
                Application.quitting -= OnQuitting;
        }

        private static int DepthOf(Transform transform)
        {
            int depth = 0;
            while (transform != null) { depth++; transform = transform.parent; }
            return depth;
        }

        private static void OnQuitting()
        {
            if (quitting) return;
            quitting = true;
            LastMatchedOwners = LastClearedContainers = LastClearedItems = 0;
            LastQuitUtc = DateTime.UtcNow.ToString("o");
            var jobs = new List<ClearJob>();
            // This includes inactive pooled views, but excludes prefab assets and
            // unloaded scenes. Capture every job before clearing any parent.
            foreach (var owner in Resources.FindObjectsOfTypeAll<MonoBehaviour>())
            {
                if (owner == null || !owner.gameObject.scene.IsValid() || !owner.gameObject.scene.isLoaded) continue;
                bool matched = false;
                foreach (var binding in Bindings)
                {
                    if (!binding.OwnerType.IsInstanceOfType(owner)) continue;
                    var container = binding.Field.GetValue(owner);
                    if (container == null) continue;
                    matched = true;
                    jobs.Add(new ClearJob { Owner = owner, Binding = binding, Container = container, Depth = DepthOf(owner.transform) });
                }
                if (matched) LastMatchedOwners++;
            }
            // Nested reward views must be cleared before their owning stage or
            // milestone is despawned. Sort by actual scene hierarchy depth.
            foreach (var job in jobs.OrderByDescending(job => job.Depth))
            {
                if (job.Owner == null || !job.Owner.gameObject.scene.IsValid() || !job.Owner.gameObject.scene.isLoaded) continue;
                LastClearedItems += job.Binding.Count(job.Container);
                job.Binding.Clear(job.Container);
                LastClearedContainers++;
            }
        }
    }
}
#endif
