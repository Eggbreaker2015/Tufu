using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using UnityEngine;

namespace Codebase.UserConsent
{
	public class MaxUserConsentService : MonoBehaviour, IUserConsentService
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CInitializeAndShowUserConsent_003Ed__8 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder _003C_003Et__builder;

			public MaxUserConsentService _003C_003E4__this;

			private UniTask<MaxSdkBase.SdkConfiguration>.Awaiter _003C_003Eu__1;

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

		private bool gdprApplies;

		private bool hasUserConsent;

		private bool isDoNotSell;

		private bool isAppTrackingAllowed;

		public bool HasUserConsent()
		{
			return this.hasUserConsent;
		}

		public bool IsDoNotSell()
		{
			return this.isDoNotSell;
		}

		public bool IsAppTrackingAllowed()
		{
			return this.isAppTrackingAllowed;
		}

		private void Awake()
		{
            global::Codebase.Core.GameContext.Set<IUserConsentService>(this);
        }

		[AsyncStateMachine(typeof(_003CInitializeAndShowUserConsent_003Ed__8))]
		public UniTask InitializeAndShowUserConsent()
		{
			return default;
		}

		private void CacheConsentValues(MaxSdkBase.SdkConfiguration configuration)
		{
		}
	}
}
