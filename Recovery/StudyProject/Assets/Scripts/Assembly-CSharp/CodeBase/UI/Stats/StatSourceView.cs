using System.Collections.Generic;
using Codebase.Core.UI;
using Codebase.Unit;
using TMPro;
using UniMob;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Stats
{
	public class StatSourceView : LifetimeMonoBehaviour
	{
		private const string SimpleCategoryColorHex = "F5D36C";

		[SerializeField]
		private TMP_Text headerNameText;

		[SerializeField]
		private TMP_Text headerValueText;

		[SerializeField]
		private Image categoryIcon;

		[SerializeField]
		private Transform rowsContainer;

		[SerializeField]
		private StatSourceRowView rowPrefab;

		[SerializeField]
		private float baseHeight;

		[SerializeField]
		private float rowHeight;

		private bool isPercent;

		private string rowNameKey;

		private Color defaultHeaderNameColor;

		private bool headerColorCaptured;

		private List<StatSourceRow> rows;

		private ViewContainer<StatSourceRowView, StatSourceRow> rowsViewContainer;

		public void Setup(StatSourceData data)
		{
		}

		private void CaptureDefaultHeaderColor()
		{
		}

		private void BuildRows()
		{
		}

		private void ApplyHeaderStyle(StatSourceData data)
		{
		}

		private void ApplyHeight(int rowCount)
		{
		}

		private string FormatHeaderValue(StatSourceData data)
		{
			return null;
		}

		private string FormatRowName(StatSourceRow row)
		{
			return null;
		}

		private string FormatRowValue(StatSourceRow row)
		{
			return null;
		}

		private string FormatFlatNumber(double value)
		{
			return null;
		}

		private static bool IsMultiplicative(ModifierType type)
		{
			return false;
		}

		private static string FormatMultiplier(double multiplier)
		{
			return null;
		}

		private static string FormatMultiplierPercent(double multiplier)
		{
			return null;
		}

		private static string FormatModifierMultiplier(double value)
		{
			return null;
		}
	}
}
