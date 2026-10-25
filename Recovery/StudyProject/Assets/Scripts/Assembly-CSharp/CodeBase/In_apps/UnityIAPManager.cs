using System.Collections.Generic;
using Codebase.Core;
using UnityEngine;

namespace Codebase.In_apps
{
	[DisallowMultipleComponent]
	public class UnityIAPManager : MonoBehaviour
	{
		[SerializeField]
		private IAPProductsConfig productsConfig;

		private Dictionary<ProductKey, ProductConfig> productsByKey;

		private IReadOnlyList<ProductConfig> ProductList => productsConfig.Products;

		private void Awake()
		{
			BuildProductsByKeyLookup();
			GameContext.Set(this);
		}

		private void BuildProductsByKeyLookup()
		{
			productsByKey = new Dictionary<ProductKey, ProductConfig>();
			foreach (ProductConfig product in ProductList)
			{
				if (product.key != ProductKey.None) productsByKey[product.key] = product;
			}
		}

		public ProductConfig? GetProductConfig(ProductKey key)
		{
			return productsByKey.TryGetValue(key, out ProductConfig product) ? product : null;
		}
	}
}
