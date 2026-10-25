using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI
{
	public class ChargeView : MonoBehaviour
	{
		[SerializeField]
		private Image icon;

		public void SetColor(Color color)
		{
			// Original APK ARM64 ELF VA 0x15d24c4: icon -> Graphic.set_color virtual call.
			icon.color = color;
		}
	}
}
