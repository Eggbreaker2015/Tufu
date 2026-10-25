using Codebase.Core;
using UnityEngine;

namespace Codebase.VIP.Components
{
	public class VipLocationTrigger : MonoBehaviour
	{
		[SerializeField]
		private VipLocationProvider locationProvider;

		[Inject]
		private VipModel vipModel;

		private void OnTriggerEnter(Collider other)
		{
		}
	}
}
