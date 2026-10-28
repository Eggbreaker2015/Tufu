#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Threading;
using Codebase.Enemy;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RecoveryCompatibility
{
    // The APK already batches spawner bootstrap and shares pools by source prefab.
    // Exported modular characters also carry hundreds of unused renderer leaves.
    // Cloning these is especially expensive in the Editor. Keep a transient, lean
    // template there; source assets, the original pool keys and Players are intact.
    public static class EditorEnemySpawnTemplates
    {
        private static readonly Dictionary<EnemyUnit, EnemyUnit> Templates = new Dictionary<EnemyUnit, EnemyUnit>();
        private static readonly Dictionary<RuntimeAnimatorController, HashSet<string>> AnimatedPaths = new Dictionary<RuntimeAnimatorController, HashSet<string>>();
        private static Transform container;

        public static int PreparedCount { get; private set; }
        public static int RemovedRendererCount { get; private set; }

        public static async UniTask PrepareAsync(Action<float> progress, CancellationToken cancellationToken)
        {
            // Called once the async scene preload has loaded its prefab dependencies,
            // before activation and before the loading screen gives control to play.
            var prefabs = Resources.FindObjectsOfTypeAll<EnemyUnit>();
            for (int i = 0; i < prefabs.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var prefab = prefabs[i];
                if (prefab != null && EditorUtility.IsPersistent(prefab)) Resolve(prefab);
                progress?.Invoke((i + 1f) / prefabs.Length);
                // A single exported character may cost tens of ms to clone. Do
                // that once behind loading, with an actual frame between templates.
                await UniTask.NextFrame(cancellationToken: cancellationToken);
            }
        }

        public static EnemyUnit Resolve(EnemyUnit prefab)
        {
            EnemyUnit key = prefab;
            if (Templates.TryGetValue(key, out var cached) && cached != null) return cached;
            var paths = FindUnusedRendererPaths(prefab);
            if (paths.Count == 0)
            {
                Templates[key] = prefab;
                return prefab;
            }

            if (container == null)
            {
                var root = new GameObject("Editor enemy spawn templates");
                root.SetActive(false);
                Object.DontDestroyOnLoad(root);
                container = root.transform;
            }
            // Inactive parent prevents script Awake/OnEnable and ECS registration.
            // Clone refs are remapped by Unity before any unused leaves are removed.
            var template = Object.Instantiate(prefab, container, false);
            template.name = prefab.name;
            foreach (string path in paths)
            {
                var leaf = template.transform.Find(path);
                if (leaf != null) Object.DestroyImmediate(leaf.gameObject);
            }
            Templates[key] = template;
            PreparedCount++;
            RemovedRendererCount += paths.Count;
            return template;
        }

        private static List<string> FindUnusedRendererPaths(EnemyUnit prefab)
        {
            var result = new List<string>();
            // Dedicated boss, crystal and effect components may enable initially
            // hidden objects dynamically. Only plain authored enemy rigs qualify.
            if (prefab.GetType() != typeof(EnemyUnit)) return result;
            var behaviours = prefab.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var behaviour in behaviours)
            {
                if (behaviour == null) return result;
                Type type = behaviour.GetType();
                if (type != typeof(EnemyUnit) && type != typeof(Codebase.Unit.UnitMovement) &&
                    type != typeof(Scellecs.Morpeh.Providers.EntityProvider)) return result;
            }

            var protectedObjects = new HashSet<Object>();
            foreach (var behaviour in behaviours)
            {
                using (var serialized = new SerializedObject(behaviour))
                {
                    var property = serialized.GetIterator();
                    while (property.Next(true))
                        if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue != null)
                            protectedObjects.Add(property.objectReferenceValue);
                }
            }
            foreach (var skin in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                foreach (var bone in skin.bones) if (bone != null) protectedObjects.Add(bone);
                if (skin.rootBone != null) protectedObjects.Add(skin.rootBone);
            }

            var animators = prefab.GetComponentsInChildren<Animator>(true);
            var candidates = prefab.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in candidates)
            {
                Transform leaf = renderer.transform;
                if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;
                if (leaf == prefab.transform || leaf.childCount != 0 || protectedObjects.Contains(leaf) ||
                    protectedObjects.Contains(leaf.gameObject) || protectedObjects.Contains(renderer)) continue;
                if (leaf.gameObject.activeSelf || HasOtherComponents(leaf, protectedObjects)) continue;

                bool animated = false;
                foreach (var animator in animators)
                {
                    if (animator.runtimeAnimatorController == null || !leaf.IsChildOf(animator.transform)) continue;
                    string animationPath = AnimationUtility.CalculateTransformPath(leaf, animator.transform);
                    if (GetAnimatedPaths(animator.runtimeAnimatorController).Contains(animationPath)) { animated = true; break; }
                }
                string path = AnimationUtility.CalculateTransformPath(leaf, prefab.transform);
                if (!animated && prefab.transform.Find(path) == leaf) result.Add(path);
            }
            return result;
        }

        private static bool HasOtherComponents(Transform leaf, HashSet<Object> protectedObjects)
        {
            foreach (var component in leaf.GetComponents<Component>())
                if (component == null || protectedObjects.Contains(component) ||
                    (!(component is Transform) && !(component is MeshFilter) &&
                     !(component is MeshRenderer) && !(component is SkinnedMeshRenderer))) return true;
            return false;
        }

        private static HashSet<string> GetAnimatedPaths(RuntimeAnimatorController controller)
        {
            if (AnimatedPaths.TryGetValue(controller, out var paths)) return paths;
            paths = new HashSet<string>();
            foreach (var clip in controller.animationClips)
            {
                foreach (var binding in AnimationUtility.GetCurveBindings(clip)) paths.Add(binding.path);
                foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip)) paths.Add(binding.path);
            }
            AnimatedPaths[controller] = paths;
            return paths;
        }
    }
}
#endif
