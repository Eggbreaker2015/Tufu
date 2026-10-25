using Codebase.BossTower.Components;
using Scellecs.Morpeh.Providers;

namespace Codebase.BossTower
{
	public class VikingBuffVisualProvider : MonoProvider<VikingBuffVisualComponent>
	{
		protected override void OnEnable()
		{
			base.OnEnable();
			ref VikingBuffVisualComponent data = ref GetData();
			foreach (UnityEngine.GameObject point in data.Objects)
				point.SetActive(false);
		}
	}
}
