using Codebase.Core.Flows;
using Cysharp.Threading.Tasks;

namespace Codebase.Services.Purchases
{
	public class PurchaseRecoveryFlowRunner : IFlowRunner<PurchaseRecoveryFlow>, IFlowHandlerBase
	{
		public UniTask Execute(PurchaseRecoveryFlow flow)
		{
			return default;
		}
	}
}
