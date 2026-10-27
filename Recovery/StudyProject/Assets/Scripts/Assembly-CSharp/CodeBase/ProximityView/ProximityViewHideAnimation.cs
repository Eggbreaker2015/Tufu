using System;
using System.Threading;
using BrunoMikoski.AnimationSequencer;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Codebase.ProximityView
{
    public class ProximityViewHideAnimation : MonoBehaviour
    {
        [SerializeField] private AnimationSequencerController hideAnimation;
        private CancellationTokenSource cts;

        // APK ARM64 0x143c3b8 / 0x143c3d8.
        private void OnEnable() => hideAnimation.ResetToInitialState();

        public void Show()
        {
            CancelHide();
            hideAnimation.Kill(false);
            hideAnimation.ResetToInitialState();
        }

        public void Hide(Action onComplete)
        {
            // APK 0x143c448.
            CancelHide();
            cts = new CancellationTokenSource();
            HideRoutine(onComplete, cts.Token).Forget();
        }

        private async UniTaskVoid HideRoutine(Action onComplete, CancellationToken token)
        {
            // APK MoveNext 0x143c5d0: a canceled hide must never despawn a reused view.
            hideAnimation.ResetToInitialState();
            await hideAnimation.PlayAsync();
            if (!token.IsCancellationRequested) onComplete?.Invoke();
        }

        private void CancelHide()
        {
            if (cts == null) return;
            cts.Cancel();
            cts.Dispose();
            cts = null;
        }

        // Learning adaptation: no delayed pool callbacks after scene teardown.
        private void OnDisable() => CancelHide();
        private void OnDestroy() => CancelHide();
    }
}
