using System;
using Codebase.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI
{
	public class PendingOrderView : PopupView
	{
		private const float Timeout = 25f;

		[SerializeField]
		private GameObject loadingState;

		[SerializeField]
		private GameObject pendingState;

		[SerializeField]
		private GameObject failedState;

		[SerializeField]
		private Button closeButton;

		private Action timeoutCallback;

		private float remainingTime;

		private bool timerActive;

		protected override void Awake()
		{
		}

		public override void Initialize()
		{
		}

		public override void Cleanup()
		{
		}

		private void Update()
		{
		}

		public void SetTimeoutCallback(Action callback)
		{
		}

		public void ShowLoading()
		{
		}

		public void ShowPending()
		{
		}

		public void ShowFailed()
		{
		}

		private void SetActiveState(GameObject active)
		{
		}
	}
}
