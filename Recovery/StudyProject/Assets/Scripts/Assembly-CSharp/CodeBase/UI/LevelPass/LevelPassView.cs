using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.LevelPass;
using Codebase.Player.Models;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using TMPro;
using UniMob.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.LevelPass
{
	public class LevelPassView : PopupView
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CScrollToTier_003Ed__24 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskVoidMethodBuilder _003C_003Et__builder;

			public int totalTiers;

			public int tierIndex;

			public LevelPassView _003C_003E4__this;

			private Cysharp.Threading.Tasks.YieldAwaitable.Awaiter _003C_003Eu__1;

			private void MoveNext()
			{
			}

			void IAsyncStateMachine.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				this.MoveNext();
			}

			[DebuggerHidden]
			private void SetStateMachine(IAsyncStateMachine stateMachine)
			{
			}

			void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
			{
				//ILSpy generated this explicit interface implementation from .override directive in SetStateMachine
				this.SetStateMachine(stateMachine);
			}
		}

		[Header("Buttons")]
		[SerializeField]
		private Button closeButton;

		[SerializeField]
		private Button purchaseButton;

		[SerializeField]
		private Button prevTierButton;

		[SerializeField]
		private Button nextTierButton;

		[Header("Tier Selector")]
		[SerializeField]
		private TMP_Text tierLabel;

		[SerializeField]
		private TMP_Text passTitle;

		[SerializeField]
		private TMP_Text purchasedLabel;

		[Header("Stages")]
		[SerializeField]
		private ScrollRect scrollRect;

		[SerializeField]
		private Transform stagesContent;

		[SerializeField]
		private LevelPassStageView stageViewPrefab;

		[Inject]
		private LevelPassModel levelPassModel;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private UINavigator navigator;

		[CompilerGenerated]
		private int __SelectedPassIndex__BackingField;

		private ViewContainer<LevelPassStageView, LevelPassTierData> stagesContainer;

		private bool hasScrolledToCurrentTier;

		private bool animateStages;

		private ComputedAtom<int> __SelectedPassIndex;

		private int SelectedPassIndex
		{
			[CompilerGenerated]
			get
			{
				return 0;
			}
			[CompilerGenerated]
			set
			{
			}
		}

		protected override void Awake()
		{
		}

		protected override void Start()
		{
		}

		protected override void Render()
		{
		}

		private IEnumerable<LevelPassTierData> GetCurrentTierData()
		{
			return null;
		}

		[AsyncStateMachine(typeof(_003CScrollToTier_003Ed__24))]
		private UniTaskVoid ScrollToTier(int tierIndex, int totalTiers)
		{
			return default;
		}

		private void OnPurchaseClicked()
		{
		}

		private static string FormatIslandsLabel(LevelPassDef pass)
		{
			return null;
		}

		private void OnPrevClicked()
		{
		}

		private void OnNextClicked()
		{
		}

		private bool IsPassAccessible(int passIndex)
		{
			return false;
		}
	}
}
