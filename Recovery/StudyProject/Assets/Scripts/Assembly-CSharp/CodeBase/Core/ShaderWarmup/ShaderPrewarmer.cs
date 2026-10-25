using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace Codebase.Core.ShaderWarmup
{
	public class ShaderPrewarmer : MonoBehaviour
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CPrewarm_003Ed__8 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder _003C_003Et__builder;

			public ShaderPrewarmer _003C_003E4__this;

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
		private struct _003CWarmGraphicsStateAsync_003Ed__9 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder _003C_003Et__builder;

			public ShaderPrewarmer _003C_003E4__this;

			private int _003Ctotal_003E5__2;

			private Cysharp.Threading.Tasks.YieldAwaitable.Awaiter _003C_003Eu__1;

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

		[Header("Recommended for Metal/Vulkan/DX12")]
		[SerializeField]
		private GraphicsStateCollection graphicsStateCollection;

		[Header("Settings")]
		[Tooltip("If true, warms up progressively over multiple frames to avoid a loading spike")]
		[SerializeField]
		private bool progressiveWarmup;

		[Tooltip("Number of PSOs to warm per frame when using progressive warmup")]
		[SerializeField]
		private int psosPerFrame;

		public bool IsComplete { get; private set; }

		private void Awake()
		{
		}

		[AsyncStateMachine(typeof(_003CPrewarm_003Ed__8))]
		public UniTask Prewarm()
		{
			return default;
		}

		[AsyncStateMachine(typeof(_003CWarmGraphicsStateAsync_003Ed__9))]
		private UniTask WarmGraphicsStateAsync()
		{
			return default;
		}
	}
}
