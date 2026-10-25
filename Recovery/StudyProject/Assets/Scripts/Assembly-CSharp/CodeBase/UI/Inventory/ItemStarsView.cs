using Codebase.Utilities;
using Codebase.Player;
using Codebase.Core;
using System.Linq;
using System.Collections.Generic;
using Codebase.Core.UI;
using Codebase.Player.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Inventory
{
	public class ItemStarsView : View
	{
		[SerializeField]
		private List<Image> stars;

		public const int COUNT = 5;

		[SerializeField]
		private bool isDescription;

		[SerializeField]
		private TMP_Text description;

		private ItemModel model;

		private int abilityIndex;

		public void Setup(ItemModel itemModel, int abilityIndex = -1)
		{
			// Native ELF VA 0x1655338.
			model = itemModel; this.abilityIndex = abilityIndex; Initialize(); Render();
		}

		protected override void Render()
		{
			// Native ELF VA 0x1655d58 and selectors 0x1656398/0x16563f4.
			var state = abilityIndex < 0 ? model.AbilityStates.FirstOrDefault(s => s.Def.LevelScaling.Count > 0)
			    : model.AbilityStates.FirstOrDefault(s => s.Def == model.Config.AbilityDefs[abilityIndex]);
			if (state == null) return;
			var style = GameContext.Get<IconStyleConfig>().StarsStyle;
			if (isDescription) { int next = state.GetNextStarLevel(); description.text = (LocalizationStrings.Get("ABILITY_STARS_DESCRIPTION") + " ").Replace("{level}", next.ToString()) + BuildStarsSpriteText(next); return; }
			var (red,pink,gold,silver) = CalculateStars(state.Stars,5);
			for(int i=0;i<5;i++) stars[i].sprite = i<red ? style.Red : i<red+pink ? style.Pink : i<red+pink+gold ? style.Gold : i<red+pink+gold+silver ? style.Silver : style.Empty;
		}

		public static string BuildStarsSpriteText(int totalStars, int starCount = 5)
		{
			// Native ELF VA 0x1655204, original sprite literal.
			var (red,pink,gold,silver) = CalculateStars(totalStars, starCount); string result = string.Empty;
			for (int i=0; i<starCount; i++) { int type = i<red ? 4 : i<red+pink ? 3 : i<red+pink+gold ? 2 : i<red+pink+gold+silver ? 1 : 0; result += string.Format("<sprite name=\"stars_{0}\">",type); }
			return result;
		}

		private static (int, int, int, int) CalculateStars(int totalStars, int starCount)
		{
			// Native ELF VA 0x1656208; four progressively filled colour tiers, original clamp 4*starCount.
			if (starCount < 1) return (0,0,0,0);
			int total = UnityEngine.Mathf.Clamp(totalStars, 0, starCount * 4);
			if (total <= starCount) return (0,0,0,total);
			if (total <= starCount*2) return (0,0,total-starCount,0);
			if (total <= starCount*3) return (0,total-starCount*2,0,0);
			return (total-starCount*3,0,0,0);
		}
	}
}
