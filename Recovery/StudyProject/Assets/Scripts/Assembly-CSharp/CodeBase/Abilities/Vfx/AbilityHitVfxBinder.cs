using UnityEngine;

namespace Codebase.Abilities.Vfx
{
	public class AbilityHitVfxBinder : MonoBehaviour
	{
		[SerializeField]
		private GameObject empoweredObject;

		public void SetEmpowered(bool empowered)
		{
			// Native ELF VA 0x15e3c2c.
			empoweredObject.SetActive(empowered);
		}
	}
}
