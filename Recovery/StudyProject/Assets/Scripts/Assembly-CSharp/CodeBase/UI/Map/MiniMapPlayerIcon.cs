using UnityEngine;

namespace Codebase.UI.Map
{
	public class MiniMapPlayerIcon : MonoBehaviour
	{
		public RectTransform RectTransform { get; private set; }

		private void Awake()
		{
			RectTransform = transform as RectTransform;
		}
	}
}
