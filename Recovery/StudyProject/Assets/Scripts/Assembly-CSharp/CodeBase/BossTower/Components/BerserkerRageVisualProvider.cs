using Scellecs.Morpeh.Providers;

namespace Codebase.BossTower.Components
{
	public class BerserkerRageVisualProvider : MonoProvider<BerserkerRageVisualComponent>
	{
		protected override void OnEnable()
		{
			base.OnEnable();
			ref BerserkerRageVisualComponent data = ref GetData();
			foreach (UnityEngine.GameObject point in data.RageLevelObjects)
				point.SetActive(false);
			data.BlinkEffect.Disable();
		}
	}
}
