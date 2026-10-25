using Multicast.Payments;

namespace Codebase.In_apps
{
	public class IAPManager
	{
		private readonly UnityIAPManager iapManager;

		private readonly IPaymentsService _paymentsService;

		public IAPManager(UnityIAPManager iapManager, IPaymentsService paymentsService)
		{
			this.iapManager = iapManager;
			_paymentsService = paymentsService;
		}

		public ProductConfig GetProductConfig(ProductKey key)
		{
			return iapManager.GetProductConfig(key);
		}

		public string GetStoreId(ProductKey key)
		{
			_paymentsService.OnChanged.ObserveAsAtom();
			return _paymentsService.GetStoreSpecificProductId(key.ToString());
		}

		public bool IsAvailableToPurchase(ProductKey key)
		{
			_paymentsService.OnChanged.ObserveAsAtom();
			return _paymentsService.IsAvailableToPurchase(key.ToString());
		}

		public string GetLocalizedPrice(ProductKey key)
		{
			_paymentsService.OnChanged.ObserveAsAtom();
			return _paymentsService.GetLocalizedPrice(key.ToString());
		}
	}
}
