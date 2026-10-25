using Codebase.Utilities;
using System.Globalization;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Crystals;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Crystals
{
	public class AmuletCrystalRarityChanceView : View
	{
		private const string IncreasedColorHex = "#42FF8E";

		private const string DecreasedColorHex = "#FFED64";

		[SerializeField]
		private Image background;

		[SerializeField]
		private Image rarityIcon;

		[SerializeField]
		private TMP_Text rarityNameText;

		[SerializeField]
		private TMP_Text currentChanceText;

		[SerializeField]
		private Image transitionIcon;

		[SerializeField]
		private TMP_Text nextChanceText;

		[Inject]
		private CrystalStyleConfig crystalStyleConfig;

		private static readonly Color IncreasedColor = ParseHexColor(IncreasedColorHex); // Native cctor1679c94.

		private static readonly Color DecreasedColor = ParseHexColor(DecreasedColorHex);

		private CrystalRarityChanceData data;

		private int index;

		public void Setup(CrystalRarityChanceData data, int index)
		{
            // Native ELF0x1679894.
            this.data = data; this.index = index; Initialize();
        }

		protected override void Render()
		{
            // Native ELF0x16798ac.
            Color color = crystalStyleConfig.GetRarityColor(data.Rarity); rarityIcon.color = color; rarityNameText.color = color;
            rarityNameText.text = LocalizationStrings.Get(string.Format("CRYSTALS_RARITY_{0}", data.Rarity));
            currentChanceText.text = FormatChance(data.CurrentChance); nextChanceText.text = FormatChance(data.NextChance);
            Color nextColor = GetNextChanceColor(data.CurrentChance, data.NextChance); nextChanceText.color = nextColor; transitionIcon.color = nextColor;
            background.enabled = (index & 1) == 0;
        }

		private static Color GetNextChanceColor(float current, float next)
		{
            // Native ELF0x1679ba0.
            // Ordered comparisons match original FCMP/B.LE then B.PL: unordered values select white.
            if (next > current) return IncreasedColor;
            if (next < current) return DecreasedColor;
            return Color.white;
        }

		private static string FormatChance(float chance)
		{
            // Native ELF0x1679abc.
            float percent = chance * 100f;
            return percent <= 0f ? "0%" : percent.ToString("F2", CultureInfo.InvariantCulture) + "%";
        }

		private static Color ParseHexColor(string hex)
		{
            // Native ELF0x1679c60.
            ColorUtility.TryParseHtmlString(hex, out Color color); return color;
        }
	}
}
