using Codebase.Core;
using UniMob;
using UnityEngine;

namespace Codebase.Services.Time
{
    public class Ticker : MonoBehaviour
    {
        private static MutableAtom<int> FrameTicker = Atom.Value(0);
        private static Ticker instance;
        public static void TickEveryFrame() => FrameTicker.Get();
        [RuntimeInitializeOnLoadMethod]
        private static void CreateTicker()
        {
            if (instance != null) return;
            var go = new GameObject("Ticker");
            instance = go.AddComponent<Ticker>();
            DontDestroyOnLoad(go);
            DontDestroyOnLoad(instance);
        }
        private void Update() => FrameTicker.Invalidate();
        private void OnDestroy() => instance = null;
        [StaticReset]
        private static void ResetStaticState()
        {
            if (instance) Destroy(instance.gameObject);
            instance = null;
            FrameTicker = Atom.Value(0);
        }
    }
}
