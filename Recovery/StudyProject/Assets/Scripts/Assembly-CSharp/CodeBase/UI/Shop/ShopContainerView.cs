using Codebase.UI.Flows;
using Codebase.Utilities;
using UniMob;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Integrations.TapJoy;
using Codebase.Shop;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Shop
{
	public class ShopContainerView : BaseView
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CShowOfferwallAsync_003Ed__27 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskVoidMethodBuilder _003C_003Et__builder;

			public ShopContainerView _003C_003E4__this;

			private UniTask<string>.Awaiter _003C_003Eu__1;

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

		private const string ChestsInfoExtraTitle = "ChestsInfo";

		[SerializeField]
		private Image headerBackground;

		[SerializeField]
		private TMP_Text headerTitle;

		[SerializeField]
		private Transform itemsRoot;

		[SerializeField]
		private FlexibleGridLayout grid;

		[SerializeField]
		private GameObject extraHeaderPadding;

		[SerializeField]
		private Image extraHeaderBackground;

		[SerializeField]
		private TMP_Text extraHeaderTitle;

		[SerializeField]
		private int itemsPerRow;

		[SerializeField]
		private float rowHeight;

		[SerializeField]
		private Button infoButton;

		[SerializeField]
		private GameObject offerwallSection;

		[SerializeField]
		private Button openQuestsButton;

		[Inject]
		private UINavigator navigator;

		[Inject]
		private ChestsModel chestsModel;

		[Inject]
		private IOfferwallService offerwallService;

		private bool extraHeaderVisible;

		private bool offerwallVisible;

		private string extraTitleKey;

		public Transform ItemsRoot => this.itemsRoot;

		public void SetupHeader(string title, Color backgroundColor)
		{
            // Native 0x1625760.
            headerTitle.text = LocalizationStrings.Get("SHOP_HEADER_" + title);
            headerBackground.color = backgroundColor;
		}

		public void SetColumns(int columns)
		{
            // Native 0x162582c.
            grid.Columns = columns; itemsPerRow = columns;
		}

		public void SetupExtraHeader(string title, Color backgroundColor)
		{
            // Native 0x1625870 and callback 0x1625e50.
            extraHeaderBackground.gameObject.SetActive(true);
            extraHeaderPadding.SetActive(true);
            extraHeaderBackground.color = backgroundColor;
            extraTitleKey = title; extraHeaderVisible = true;
            if (extraTitleKey == ChestsInfoExtraTitle)
                Atom.Reaction(Lifetime, () => extraHeaderTitle.text = LocalizationStrings.Get("SHOP_HEADER_" + extraTitleKey).Replace("{value}", chestsModel.Level.ToString()));
            else extraHeaderTitle.text = LocalizationStrings.Get("SHOP_HEADER_" + extraTitleKey);
		}

		public void SetupInfoButton(IReadOnlyList<string> chestKeys)
		{
            // Native 0x1625a44 and original callback 0x1625f2c.
            infoButton.gameObject.SetActive(true);
            infoButton.onClick.RemoveAllListeners();
            infoButton.onClick.AddListener(() => navigator.Open<ChestsInfoView>("Assets/Prefabs/UI/ChestChancesInfo.prefab").Setup(chestKeys));
		}

		public void SetupOfferwallSection()
		{
            // Native 0x1625b6c. Remote action is only bound, never automatically invoked.
            offerwallSection.SetActive(true); offerwallVisible = true;
            openQuestsButton.onClick.RemoveAllListeners();
            openQuestsButton.onClick.AddListener(OnOpenOfferwallClicked);
		}

		private void OnOpenOfferwallClicked()
		{
            // Native 0x1625c30.
            ShowOfferwallAsync().Forget();
		}

		
		private async UniTaskVoid ShowOfferwallAsync()
		{
            // Native 0x1625c54 / 0x1625fb0. Restored original action; validation never calls this remote branch.
            string error = await offerwallService.ShowContent();
            if (!string.IsNullOrEmpty(error))
                App.InitiateFlow<OpenDialogFlow, bool>(OpenDialogFlow.WithRawMessage(LocalizationStrings.Get("ERROR"), LocalizationStrings.Get("ERROR_PREFIX") + " " + error), false);
		}

		public void SetItemsCount(int itemsCount, float preferredHeight)
		{
            // Native 0x1625ce4 ASM fixes the decompiler's overlapping float return registers.
            float extraHeight = extraHeaderVisible ? extraHeaderBackground.preferredHeight + ((RectTransform)extraHeaderPadding.transform).sizeDelta.y : 0f;
            float offerHeight = offerwallVisible ? ((RectTransform)offerwallSection.transform).rect.height : 0f;
            Vector2 size = rectTransform.sizeDelta;
            size.y = offerHeight + (extraHeight + (size.y + ((itemsCount - 1) / itemsPerRow + 1) * preferredHeight));
            rectTransform.sizeDelta = size;
		}
	}
}
