using Codebase.Utilities;
using Codebase.UI.Inventory;
using Codebase.Player;
using Codebase.Core;
using System.Linq;
using Codebase.Unit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI
{
	public class StatView : MonoBehaviour
	{
		[SerializeField]
		private TMP_Text field;

		[SerializeField]
		private Button button;

		[SerializeField]
		private Image icon;

		private Stat stat;

		public void Initialize(Stat stat)
		{
			// Native ELF VA 0x15fdd0c and original captured click 0x15fe314.
			this.stat=stat;field.enabled=false;
			icon.sprite=GameContext.Get<IconStyleConfig>().Styles.First(style=>style.StatType==stat.Type).Sprite;
			button.onClick.RemoveAllListeners();
			button.onClick.AddListener(()=>GameContext.Get<InventoryItemContextMenu>().SelectStat(this.stat.Type.ToString(),GetString()));
		}

		private string GetString()
		{
			// Native ELF VA 0x15fdf20; modifier aggregate lambda 0x15fe1a8.
			string text="Base: "+stat.BaseValue.Format()+"\n";
			foreach(var modifier in stat.Modifiers)text+=modifier.Name+": "+modifier.ToString()+"\n";
			return text+"Total: "+stat.Value.Format();
		}
	}
}
