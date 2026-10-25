using Scellecs.Morpeh.Providers;
using UnityEngine;

namespace Codebase.Unit.Components
{
	public class UnitHasSpawnAnimProvider : MonoProvider<UnitHasSpawnAnimComponent>
	{
		[SerializeField]
		private string triggerName;

		private void Start()
		{
		}
	}
}
