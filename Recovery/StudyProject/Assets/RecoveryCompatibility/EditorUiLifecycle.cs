#if UNITY_EDITOR
// Editor compatibility for the recovered player-form UGUI/TMP DLLs.
// Mirrors the installed com.unity.ugui 2.6.0 ResetStaticsOnLoad implementations.
// Those official editor-only initializers were excluded when producing the DLLs.
// This file is excluded from Players and does not modify original game methods/assets.
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace RecoveryCompatibility
{
    public static class EditorUiLifecycle
    {
        public static string LastResetUtc { get; private set; }
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void ResetRebuildRegistries()
        {
            Reset(typeof(UnityEngine.UI.CanvasUpdateRegistry), "PerformUpdate");
            Reset(typeof(TMPro.TMP_UpdateManager), "DoRebuilds");
            Reset(typeof(TMPro.TMP_UpdateRegistry), "PerformUpdateForCanvasRendererObjects");
            ResetMaterialManager();
            LastResetUtc = DateTime.UtcNow.ToString("o");
        }

        private static void Reset(Type type, string rebuildMethod)
        {
            // A future genuine Editor-form library already owns this initializer.
            if (type.GetMethod("ResetStaticsOnLoad", Flags) != null) return;
            FieldInfo singleton = type.GetField("s_Instance", Flags);
            MethodInfo update = type.GetMethod(rebuildMethod, Flags);
            if (singleton == null || update == null)
                throw new MissingMemberException(type.FullName, "Official UGUI/TMP singleton/rebuild contract changed");
            object current = singleton.GetValue(null);
            if (current == null) return;
            var callback = (Canvas.WillRenderCanvases)Delegate.CreateDelegate(typeof(Canvas.WillRenderCanvases), current, update);
            Canvas.willRenderCanvases -= callback;
            singleton.SetValue(null, null);
        }

        private static void ResetMaterialManager()
        {
            Type type = typeof(TMPro.TMP_MaterialManager);
            if (type.GetMethod("ResetStaticsOnLoad", Flags) != null) return;
            // Calling the official static reset first initializes its declaring type.
            // Reflection must preserve that ordering before rebinding its ctor callback.
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(type.TypeHandle);
            MethodInfo update = type.GetMethod("OnPreRender", Flags);
            if (update == null) throw new MissingMethodException(type.FullName, "OnPreRender");
            var callback = (Canvas.WillRenderCanvases)Delegate.CreateDelegate(typeof(Canvas.WillRenderCanvases), update);
            Canvas.willRenderCanvases -= callback;
            Canvas.willRenderCanvases += callback;

            // Same temporary masking/fallback material cleanup as the official Editor reset.
            var masking = (IList)StaticField(type, "m_materialList").GetValue(null);
            if (masking.Count > 0) TMPro.TMP_MaterialManager.ClearMaterials();
            var fallback = (IDictionary)StaticField(type, "m_fallbackMaterials").GetValue(null);
            foreach (DictionaryEntry entry in fallback)
            {
                FieldInfo materialField = entry.Value.GetType().GetField("fallbackMaterial", Flags);
                if (materialField == null) throw new MissingFieldException(entry.Value.GetType().FullName, "fallbackMaterial");
                var material = (Material)materialField.GetValue(entry.Value);
                if (material != null) UnityEngine.Object.DestroyImmediate(material);
            }
            fallback.Clear();
            ((IDictionary)StaticField(type, "m_fallbackMaterialLookup").GetValue(null)).Clear();
            ((IList)StaticField(type, "m_fallbackCleanupList").GetValue(null)).Clear();
            StaticField(type, "isFallbackListDirty").SetValue(null, false);
        }

        private static FieldInfo StaticField(Type type, string name)
        {
            return type.GetField(name, Flags) ?? throw new MissingFieldException(type.FullName, name);
        }
    }
}
#endif
