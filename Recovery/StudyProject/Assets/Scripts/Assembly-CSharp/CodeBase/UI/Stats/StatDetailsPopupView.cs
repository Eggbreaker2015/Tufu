using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using CodeBase.Audio;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Location;
using Codebase.Meta;
using Codebase.Player;
using Codebase.Player.Inventory;
using Codebase.Unit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Stats
{
	public class StatDetailsPopupView : PopupView
	{
		private readonly struct MetaParam
		{
			public readonly int IntParam;

			public readonly string StringParam;

			public MetaParam(int intParam, string stringParam)
			{
				IntParam = 0;
				StringParam = null;
			}
		}

		[CompilerGenerated]
		private sealed class _003CRebuildLayoutNextFrame_003Ed__26 : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			public StatDetailsPopupView _003C_003E4__this;

			object IEnumerator<object>.Current
			{
				[DebuggerHidden]
				get
				{
					return this._003C_003E2__current;
				}
			}

			object IEnumerator.Current
			{
				[DebuggerHidden]
				get
				{
					return this._003C_003E2__current;
				}
			}

			[DebuggerHidden]
			public _003CRebuildLayoutNextFrame_003Ed__26(int _003C_003E1__state)
			{
			}

			[DebuggerHidden]
			void IDisposable.Dispose()
			{
			}

			private bool MoveNext()
			{
				return false;
			}

			bool IEnumerator.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				return this.MoveNext();
			}

			[DebuggerHidden]
			void IEnumerator.Reset()
			{
			}
		}

		private const string CappedValueColorHex = "F5D36C";

		[SerializeField]
		private Button closeButton;

		[SerializeField]
		private TMP_Text statNameText;

		[SerializeField]
		private TMP_Text statValueText;

		[SerializeField]
		private Transform sourcesContainer;

		[SerializeField]
		private StatSourceView sourcePrefab;

		[Inject]
		private IconStyleConfig iconStyleConfig;

		[Inject]
		private IAudioPlayer audioPlayer;

		[Inject]
		private IslandMasteryModel islandMasteryModel;

		[Inject]
		private IslandMedalStyleConfig islandMedalStyleConfig;

		private StatBase source;

		private Stat stat;

		private MetaStat metaStat;

		private MetaStatDef metaDef;

		private bool isMeta;

		private List<StatSourceData> sourceGroups;

		private ViewContainer<StatSourceView, StatSourceData> sourcesViewContainer;

		private TMP_Text intermediateFormulaText;

		private bool DisplayAsPercent => false;

		protected override void Awake()
		{
		}

		private void HandleClose()
		{
		}

		protected override void Start()
		{
		}

		public void Setup(Stat stat)
		{
		}

		public void Setup(MetaStat metaStat, MetaStatDef metaDef)
		{
		}

		private void RebuildSources()
		{
		}

		[IteratorStateMachine(typeof(_003CRebuildLayoutNextFrame_003Ed__26))]
		private IEnumerator RebuildLayoutNextFrame()
		{
			return null;
		}

		protected override void Render()
		{
		}

		private void RenderMeta()
		{
		}

		private string BuildMetaFormulaText()
		{
			return null;
		}

		private string FormatMetaNumber(double value)
		{
			return null;
		}

		private bool ShouldShowPostFormulaSplit()
		{
			return false;
		}

		private bool HasPostFormulaModifiers()
		{
			return false;
		}

		private void EnsureIntermediateFormulaText()
		{
		}

		private int GetIntermediateSiblingIndex()
		{
			return 0;
		}

		private List<StatSourceData> BuildSourceGroups()
		{
			return null;
		}

		private bool HasMetaParams()
		{
			return false;
		}

		private List<StatSourceData> BuildMetaParamGroups()
		{
			return null;
		}

		private List<MetaParam> CollectMetaParams()
		{
			return null;
		}

		private StatSourceData BuildMetaParamFlatGroup(int intParam, string stringParam, string label)
		{
			return null;
		}

		private StatSourceData BuildMetaParamPercentGroup(int intParam, string stringParam, string label)
		{
			return null;
		}

		private double CalculateMetaParamMultCoefficient(int intParam, string stringParam)
		{
			return 0.0;
		}

		private double GetMetaParamValue(int intParam, string stringParam)
		{
			return 0.0;
		}

		private string ResolveParamLabel(int intParam, string stringParam)
		{
			return null;
		}

		private Rarity? ResolveParamRarity(int intParam, string stringParam)
		{
			return null;
		}

		private Sprite ResolveParamIcon(int intParam, string stringParam)
		{
			return null;
		}

		private string ResolveRowNameKey(int intParam, string stringParam)
		{
			return null;
		}

		private StatSourceData BuildPostFormulaFlatGroup()
		{
			return null;
		}

		private StatSourceData BuildFlatGroup()
		{
			return null;
		}

		private StatSourceData BuildPercentGroup()
		{
			return null;
		}

		private StatSourceData BuildPostFormulaPercentGroup()
		{
			return null;
		}

		private double CalculateMultCoefficient()
		{
			return 0.0;
		}

		private double CalculatePostFormulaMultiplier()
		{
			return 0.0;
		}

		private double GetPostFormulaFlatTotal()
		{
			return 0.0;
		}

		private string BuildRawValueFormulaText(bool appendSpecificTransition)
		{
			return null;
		}

		private string BuildSpecificValueFormulaText()
		{
			return null;
		}

		private string ColorizeIfCapped(string text)
		{
			return null;
		}

		private static bool IsRawPercentModifier(ModifierType type)
		{
			return false;
		}

		private static string FormatMultiplierPercent(double multiplier)
		{
			return null;
		}

		private static double AggregateValue(ModifierType type, IEnumerable<double> values)
		{
			return 0.0;
		}
	}
}
