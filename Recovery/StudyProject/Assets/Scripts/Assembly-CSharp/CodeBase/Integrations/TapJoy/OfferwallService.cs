using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Codebase.Analytics;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using TapjoyUnity;
using UniMob;
using UnityEngine;

namespace Codebase.Integrations.TapJoy
{
	public class OfferwallService : MonoBehaviour, IOfferwallService
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CRequestCurrencyBalanceDelayedAsync_003Ed__18 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskVoidMethodBuilder _003C_003Et__builder;

			public OfferwallService _003C_003E4__this;

			private UniTask.Awaiter _003C_003Eu__1;

			private void MoveNext()
			{
			}

			void IAsyncStateMachine.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				this.MoveNext();
			}

			[DebuggerHidden]
			private void SetStateMachine(IAsyncStateMachine stateMachine)
			{
			}

			void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
			{
				//ILSpy generated this explicit interface implementation from .override directive in SetStateMachine
				this.SetStateMachine(stateMachine);
			}
		}

		[SerializeField]
		private string androidSdkKey;

		[SerializeField]
		private string iosSdkKey;

		[SerializeField]
		private string androidPlacementName;

		[SerializeField]
		private string iosPlacementName;

		[SerializeField]
		private bool debugMode;

		private const string LogPrefix = "[Offerwall]";

		private readonly MutableAtom<bool> isContentReady;

		private TJPlacement placement;

		private string lastError;

		private UniTaskCompletionSource<string> showTcs;

		private IAnalyticsWrapper analytics;

		private int pendingSpend;

		public Atom<bool> IsContentReady => null;

		private void Awake()
		{
            global::Codebase.Core.GameContext.Set<IOfferwallService>(this);
        }

		private void Start()
		{
		}

		private void OnDestroy()
		{
		}

		private void RequestCurrencyBalance()
		{
		}

		[AsyncStateMachine(typeof(_003CRequestCurrencyBalanceDelayedAsync_003Ed__18))]
		private UniTaskVoid RequestCurrencyBalanceDelayedAsync()
		{
			return default;
		}

		private void SpendCurrency(int amount)
		{
		}

		private void OnGetCurrencyBalanceResponse(string currencyName, int balance)
		{
		}

		private void OnGetCurrencyBalanceResponseFailure(string error)
		{
		}

		private void OnSpendCurrencyResponse(string currencyName, int balance)
		{
		}

		private void OnSpendCurrencyResponseFailure(string error)
		{
		}

		public UniTask<string> ShowContent()
		{
			return default;
		}

		private void OnConnectSuccess()
		{
		}

		private void OnConnectFailed(int code, string message)
		{
		}

		private void CreateAndRequest()
		{
		}

		private void RequestContent()
		{
		}

		private void OnRequestSuccess(TJPlacement requested)
		{
		}

		private void OnRequestFailure(TJPlacement requested, string error)
		{
		}

		private void OnContentReady(TJPlacement requested)
		{
		}

		private void OnContentShow(TJPlacement requested)
		{
		}

		private void OnContentDismiss(TJPlacement requested)
		{
		}

		private string ResolveSdkKey()
		{
			return this.androidSdkKey;
		}

		private string ResolvePlacementName()
		{
			return this.androidPlacementName;
		}
	}
}
