using System;
using Codebase.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI
{
    public class PendingOrderView : PopupView
    {
        private const float Timeout = 25f;
        [SerializeField] private GameObject loadingState;
        [SerializeField] private GameObject pendingState;
        [SerializeField] private GameObject failedState;
        [SerializeField] private Button closeButton;
        private Action timeoutCallback;
        private float remainingTime;
        private bool timerActive;

        // Original ARM64 ELF VA 0x15f8234 through 0x15f84b8.
        protected override void Awake()
        {
            base.Awake();
            closeButton.onClick.AddListener(Close);
        }
        public override void Initialize()
        {
            base.Initialize();
            ShowLoading();
            remainingTime = Timeout;
            timerActive = true;
        }
        public override void Cleanup()
        {
            timerActive = false;
            timeoutCallback = null;
            base.Cleanup();
        }
        private void Update()
        {
            if (!timerActive) return;
            remainingTime -= Mathf.Min(Time.unscaledDeltaTime, 1f);
            if (remainingTime > 0f) return;
            timerActive = false;
            timeoutCallback?.Invoke();
        }
        public void SetTimeoutCallback(Action callback) => timeoutCallback = callback;
        public void ShowLoading() => SetActiveState(loadingState);
        public void ShowPending() => SetActiveState(pendingState);
        public void ShowFailed() => SetActiveState(failedState);
        private void SetActiveState(GameObject active)
        {
            loadingState.SetActive(loadingState == active);
            pendingState.SetActive(pendingState == active);
            failedState.SetActive(failedState == active);
            closeButton.gameObject.SetActive(active != loadingState);
        }
    }
}
