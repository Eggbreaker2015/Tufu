using Codebase.Utilities;
using TMPro;
using UnityEngine;

namespace Codebase.UI.Crystals
{
	public class CrystalEnchantLifesView : MonoBehaviour
	{
		[SerializeField]
		private GameObject root;

		[SerializeField]
		private TMP_Text romanText;

		[SerializeField]
		private TMP_Text countText;

		public void Setup(int enchantNumber, int remaining, int total)
		{
            // Native ELF0x13a9134.
            root.SetActive(total > 0); if (total < 1) return;
            romanText.text = RomanNumerals.ToRoman(enchantNumber); countText.text = string.Format("{0}/{1}", remaining, total);
        }
	}
}
