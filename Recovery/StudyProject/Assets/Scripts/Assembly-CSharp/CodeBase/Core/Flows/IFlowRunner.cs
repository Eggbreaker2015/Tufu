using Cysharp.Threading.Tasks;

namespace Codebase.Core.Flows
{
	public interface IFlowRunner<in T> : IFlowHandlerBase where T : struct, IFlow
	{
		UniTask Execute(T flow);
	}
	public interface IFlowRunner<in T, TResult> : IFlowHandlerBase where T : struct, IFlow<TResult>
	{
		UniTask<TResult> Execute(T flow);
	}
}
