using System.Collections.Generic;
using UnityEngine;

namespace Codebase.UI.Shop
{
	public class ShopItemIconView : MonoBehaviour
	{
		[SerializeField]
		private List<ShopItemIconSet> iconSets;

		public void Render(ShopLotIconType iconType, Sprite sprite)
		{
            // Native 0x1626a3c: only selected icon-set images are modified.
            for (int i = 0; i < iconSets.Count; i++) {
                ShopItemIconSet set = iconSets[i];
                set.Root.SetActive(i == (int)iconType);
                if (i == (int)iconType) foreach (var image in set.Images) image.sprite = sprite;
            }
		}
	}
}
