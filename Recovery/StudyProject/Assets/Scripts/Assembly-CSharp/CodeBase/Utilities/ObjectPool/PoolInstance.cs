using UnityEngine;

namespace Codebase.Utilities.ObjectPool
{
	public class PoolInstance : MonoBehaviour
	{
		[field: SerializeField]
		public int SourcePrefabHash { get; set; }
	}
}
