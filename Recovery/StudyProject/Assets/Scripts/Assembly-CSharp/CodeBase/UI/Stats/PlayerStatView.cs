using Codebase.Core.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Stats
{
	public class PlayerStatView : View
	{
		[SerializeField]
		private TMP_Text nameField;

		[SerializeField]
		private TMP_Text valueField;

		[SerializeField]
		private TMP_Text statField;

		[SerializeField]
		private GameObject special;

		[SerializeField]
		private GameObject meta;

		[SerializeField]
		private Button button;

		private StatRowData data;

		private const string CappedValueColorHex = "F5D36C";

		private void OnEnable()
		{
		}

		public void Setup(StatRowData rowData)
		{
		}

		private void OpenDetails()
		{
		}

		protected override void Render()
		{
		}

		private void RenderMeta()
		{
		}

		private string FormatMetaValue()
		{
			return null;
		}

		private void RenderStat()
		{
		}

		private string FormatStatValue()
		{
			return null;
		}

		private string FormatGlobalStat()
		{
			return null;
		}

		private string FormatSpecificPercentStat()
		{
			return null;
		}

		private string FormatPercentValue()
		{
			return null;
		}

		private string FormatRegularValue()
		{
			return null;
		}

		private string GetSpecificValueSuffix(bool asPercent)
		{
			return null;
		}
	}
}
