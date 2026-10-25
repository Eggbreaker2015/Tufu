using System.Collections.Generic;
using UnityEngine;

namespace Codebase.In_apps
{
	[CreateAssetMenu(menuName = "Codebase/In-apps/IAP Products Config")]
	public class IAPProductsConfig : ScriptableObject
	{
		[SerializeField]
		private List<ProductConfig> products;

		public IReadOnlyList<ProductConfig> Products => products;
	}
}
