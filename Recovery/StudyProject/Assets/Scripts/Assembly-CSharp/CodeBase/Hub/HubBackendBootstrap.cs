using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Codebase.Integrations.AppMetrica;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using Multicast.Backend;
using UnityEngine;

namespace Codebase.Hub
{
	public static class HubBackendBootstrap
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CLogin_003Ed__2 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder<bool> _003C_003Et__builder;

			public AppMetricaAnalyticsWrapper analytics;

			private Awaitable<bool>.Awaiter _003C_003Eu__1;

			private Awaitable.Awaiter _003C_003Eu__2;

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
		private struct _003CTryLogin_003Ed__1 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder<bool> _003C_003Et__builder;

			public AppMetricaAnalyticsWrapper analytics;

			private UniTask<bool>.Awaiter _003C_003Eu__1;

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

		public static (BackendUrl, string) GetBackendInfo()
		{
			return default;
		}

		[AsyncStateMachine(typeof(_003CTryLogin_003Ed__1))]
		public static UniTask<bool> TryLogin(AppMetricaAnalyticsWrapper analytics)
		{
			return default;
		}

		[AsyncStateMachine(typeof(_003CLogin_003Ed__2))]
		public static UniTask<bool> Login(AppMetricaAnalyticsWrapper analytics)
		{
			return default;
		}
	}
}
