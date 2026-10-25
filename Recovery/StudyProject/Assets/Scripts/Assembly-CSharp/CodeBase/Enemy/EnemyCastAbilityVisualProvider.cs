using Codebase.Enemy.Components;
using Scellecs.Morpeh.Providers;

namespace Codebase.Enemy
{
	public class EnemyCastAbilityVisualProvider : MonoProvider<EnemyCastAbilityVisualComponent>
	{
		protected override void OnEnable()
		{
			base.OnEnable();
			ref var visual = ref GetData();
			if (visual.VisualObject != null)
				visual.VisualObject.SetActive(false);
		}
	}
}
