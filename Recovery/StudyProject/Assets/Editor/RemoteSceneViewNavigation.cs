using UnityEditor;
using UnityEngine;

/// <summary>
/// Workaround for remote desktop input that changes mousePosition but
/// reports a zero MouseDrag delta. SceneView navigation consumes that delta.
/// </summary>
[InitializeOnLoad]
public static class RemoteSceneViewNavigation
{
    private const string MenuPath = "Tools/Editor/Remote Scene View Navigation";
    private const string PreferenceKey = "Joyfort.RemoteSceneViewNavigation.Enabled";
    private static bool enabled;
    private static SceneView dragView;
    private static Vector2 previousPosition;
    private static int dragButton = -1;

    static RemoteSceneViewNavigation()
    {
        enabled = EditorPrefs.GetBool(PreferenceKey, true);
        // Camera drawing between beforeSceneGui and duringSceneGui resets event
        // deltas in Unity 2022. Apply the correction immediately before navigation.
        SceneView.duringSceneGui += DuringSceneGui;
    }

    public static bool Enabled
    {
        get { return enabled; }
        set
        {
            enabled = value;
            EditorPrefs.SetBool(PreferenceKey, value);
            ResetDrag();
        }
    }

    [MenuItem(MenuPath)]
    private static void Toggle()
    {
        Enabled = !Enabled;
    }

    [MenuItem(MenuPath, true)]
    private static bool ValidateToggle()
    {
        Menu.SetChecked(MenuPath, Enabled);
        return true;
    }

    private static void DuringSceneGui(SceneView view)
    {
        if (!enabled) return;
        Event evt = Event.current;
        if (evt == null) return;

        if (evt.rawType == EventType.MouseUp ||
            (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Escape))
        {
            ResetDrag();
            return;
        }

        if (evt.type == EventType.MouseDown)
        {
            ResetDrag();
            bool navigationGesture = evt.button == 2 || evt.button == 1 ||
                (evt.button == 0 && (Tools.current == Tool.View || evt.alt));
            if (!navigationGesture || GUIUtility.hotControl != 0) return;
            dragView = view;
            dragButton = evt.button;
            previousPosition = evt.mousePosition;
            return;
        }

        if (evt.type != EventType.MouseDrag || dragView != view || evt.button != dragButton) return;
        Vector2 position = evt.mousePosition;
        Vector2 displacement = position - previousPosition;
        var bounds = new Rect(Vector2.zero, view.position.size);
        bool insideView = bounds.Contains(previousPosition) && bounds.Contains(position);
        previousPosition = position;

        // Leave normal mouse deltas and consumed/uncaptured editor gestures alone.
        // Invalid positions can occur when macOS captures or warps the cursor.
        if (GUIUtility.hotControl != 0 && evt.delta.sqrMagnitude == 0 && insideView &&
            !float.IsNaN(displacement.x) && !float.IsNaN(displacement.y) &&
            !float.IsInfinity(displacement.x) && !float.IsInfinity(displacement.y))
            evt.delta = displacement;
    }

    private static void ResetDrag()
    {
        dragView = null;
        dragButton = -1;
    }
}
