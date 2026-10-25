using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Codebase.Events;
using Codebase.In_apps;
using Codebase.LevelPass;
using Codebase.Player;
using Codebase.Services.Purchases.Handlers;
using Codebase.UI.Shop;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using Multicast.Payments;
using UnityEngine;

namespace Codebase.Services.Purchases
{
	public class PurchaseProcessingService
	{
		[CompilerGenerated]
		private sealed class _003C_003Ec__DisplayClass18_0
		{
			public ProductKey productKey;

			internal bool _003CStartPurchaseInternalAsync_003Eb__0(IProductHandler h)
			{
				return false;
			}
		}

		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CContinueInterruptedPayments_003Ed__16 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder _003C_003Et__builder;

			public PurchaseProcessingService _003C_003E4__this;

			private IEnumerator<PurchaseArgs> _003C_003E7__wrap1;

			private Awaitable<IReadOnlyList<PurchaseArgs>>.Awaiter _003C_003Eu__1;

			private UniTask.Awaiter _003C_003Eu__2;

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

		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CDeliverOrderAsync_003Ed__19 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder _003C_003Et__builder;

			public PurchaseArgs purchaseArgs;

			public PurchaseProcessingService _003C_003E4__this;

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

		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CStartPurchaseInternalAsync_003Ed__18 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder _003C_003Et__builder;

			public ProductKey productKey;

			public PurchaseProcessingService _003C_003E4__this;

			private _003C_003Ec__DisplayClass18_0 _003C_003E8__1;

			private UniTask.Awaiter _003C_003Eu__1;

			private Awaitable<PurchaseArgs>.Awaiter _003C_003Eu__2;

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

		private readonly List<IProductHandler> handlers;

		private readonly IAPManager iapManager;

		private readonly IPaymentsService paymentsService;

		[CompilerGenerated]
		private Action m_OnPurchaseStarted;

		[CompilerGenerated]
		private Action<PurchaseDeliveryInfo, PurchaseRewardInfo> m_OnPurchaseDelivered;

		[CompilerGenerated]
		private Action m_OnPurchaseFinished;

		[CompilerGenerated]
		private Action<(bool userCancelled, ProductKey productKey)> m_OnPurchaseFailed;

		public event Action OnPurchaseStarted
		{
			[CompilerGenerated]
			add
			{
			}
			[CompilerGenerated]
			remove
			{
			}
		}

		public event Action<PurchaseDeliveryInfo, PurchaseRewardInfo> OnPurchaseDelivered
		{
			[CompilerGenerated]
			add
			{
			}
			[CompilerGenerated]
			remove
			{
			}
		}

		public event Action OnPurchaseFinished
		{
			[CompilerGenerated]
			add
			{
			}
			[CompilerGenerated]
			remove
			{
			}
		}

		public event Action<(bool userCancelled, ProductKey productKey)> OnPurchaseFailed
		{
			[CompilerGenerated]
			add
			{
			}
			[CompilerGenerated]
			remove
			{
			}
		}

		public PurchaseProcessingService(ShopSectionsConfig shopConfig, EventsProvider eventsProvider, LevelPassConfig levelPassConfig, IAPManager iapManager, IconStyleConfig iconStyleConfig, IPaymentsService paymentsService)
		{
		}

		[AsyncStateMachine(typeof(_003CContinueInterruptedPayments_003Ed__16))]
		public UniTask ContinueInterruptedPayments()
		{
			return default;
		}

		public void StartPurchase(ProductKey productKey)
		{
		}

		[AsyncStateMachine(typeof(_003CStartPurchaseInternalAsync_003Ed__18))]
		private UniTask StartPurchaseInternalAsync(ProductKey productKey)
		{
			return default;
		}

		[AsyncStateMachine(typeof(_003CDeliverOrderAsync_003Ed__19))]
		private UniTask DeliverOrderAsync(PurchaseArgs purchaseArgs)
		{
			return default;
		}

		private void SendRevenueToFirebase(PurchaseArgs purchaseArgs)
		{
		}
	}
}
