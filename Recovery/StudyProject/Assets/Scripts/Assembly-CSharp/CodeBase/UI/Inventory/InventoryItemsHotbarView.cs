using UnityEngine;

namespace Codebase.UI.Inventory
{
	public class InventoryItemsHotbarView : MonoBehaviour
	{
		[SerializeField]
		private float height;

		private RectTransform rectTransform;

		private void Start()
		{
			// Native ELF VA 0x164f088; x size remains unchanged.
			rectTransform = GetComponent<RectTransform>();
			rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, height);
		}
	}
}
