using Codebase.Unit.Components;
using Scellecs.Morpeh.Providers;

namespace Codebase.Enemy
{
	public class PositionProvider : MonoProvider<PositionComponent>
	{
		private void Start()
		{
			Stash.Get(Entity).Position = transform.position;
		}
	}
}
