using AdjustSdk;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Codebase.Integrations
{
	public class AdjustInitializer : MonoBehaviour
	{
		[SerializeField]
		private Adjust adjust;

		public UniTask InitializeAsync()
		{
			return default;
		}
	}
}
