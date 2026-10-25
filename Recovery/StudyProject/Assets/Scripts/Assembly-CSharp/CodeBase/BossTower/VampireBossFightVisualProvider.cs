using Codebase.BossTower.Components;
using Scellecs.Morpeh.Providers;
using UnityEngine;

namespace Codebase.BossTower
{
	public class VampireBossFightVisualProvider : MonoProvider<VampireBossFightVisualComponent>
	{
		protected override void OnEnable()
		{
			// Original ELF VA 0x155da8c: publish the original serialized component first.
			base.OnEnable();
			ref VampireBossFightVisualComponent visual = ref GetData();
			visual.CenterVisual.SetActive(false);
			visual.CenterVisual.transform.localScale = Vector3.one;
			foreach (VampireSpawnPointEntry point in visual.SpawnPoints)
			{
				point.Vfx.SetActive(false);
				point.RotationEffect.Stop();
			}
		}
	}
}
