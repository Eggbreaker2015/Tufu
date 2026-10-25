using System.Runtime.CompilerServices;
using CodeBase.Audio;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Quests;
using TMPro;
using UniMob.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Quests
{
	public class QuestPopupView : View
	{
		[SerializeField]
		private GameObject buttonState;

		[SerializeField]
		private GameObject plaqueState;

		[SerializeField]
		private Button openButton;

		[SerializeField]
		private TMP_Text buttonStatusText;

		[SerializeField]
		private Image chainIcon;

		[SerializeField]
		private GameObject exclamation;

		[SerializeField]
		private TMP_Text plaqueText;

		[Inject]
		private QuestModel questModel;

		[Inject]
		private QuestConfig config;

		[Inject]
		private UINavigator navigator;

		[Inject]
		private IAudioPlayer audioPlayer;

		[CompilerGenerated]
		private QuestChainDef __Chain__BackingField;

		[CompilerGenerated]
		private QuestPopupDisplayMode __DisplayMode__BackingField;

		private ComputedAtom<QuestChainDef> __Chain;

		private ComputedAtom<QuestPopupDisplayMode> __DisplayMode;

		private QuestChainDef Chain
		{
			[CompilerGenerated]
			get
			{
				UniMob.LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__Chain == null) __Chain = CodeGenAtom.CreatePooled(this, null, () => Chain, false);
				return __Chain.DirectEvaluate() ? __Chain__BackingField : __Chain.Value;
			}
			[CompilerGenerated]
			set
			{
				UniMob.LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__Chain != null && !__Chain.CompareAndInvalidate(value)) return;
				__Chain__BackingField = value;
			}
		}

		private QuestPopupDisplayMode DisplayMode
		{
			[CompilerGenerated]
			get
			{
				UniMob.LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__DisplayMode == null) __DisplayMode = CodeGenAtom.CreatePooled(this, null, () => DisplayMode, false);
				return __DisplayMode.DirectEvaluate() ? __DisplayMode__BackingField : __DisplayMode.Value;
			}
			[CompilerGenerated]
			set
			{
				UniMob.LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__DisplayMode != null && !__DisplayMode.CompareAndInvalidate(value)) return;
				__DisplayMode__BackingField = value;
			}
		}

		protected override void Awake()
		{
			// Original ELF 0x1635938.
			base.Awake();
			openButton.onClick.AddListener(HandleOpen);
		}

		public void Setup(QuestChainDef chain, QuestPopupDisplayMode displayMode)
		{
			// Original ELF 0x16359d4.
			Chain = chain;
			DisplayMode = displayMode;
			Initialize();
		}

		public void DisposeView()
		{
			// Original ELF 0x1635a0c.
			Cleanup();
		}

		protected override void Render()
		{
			// Original ELF 0x1635a18.
			if (Chain == null) return;
			if (DisplayMode != QuestPopupDisplayMode.Plaque) { RenderButtonState(); return; }
			var stepKey = questModel.Popup.GetObjectivePlaqueStepKey(Chain);
			if (stepKey == null || config.GetStep(stepKey) == null) return;
			RenderPlaqueState();
		}

		private void RenderPlaqueState()
		{
			// Original ELF 0x1635aac.
			plaqueState.SetActive(true);
			buttonState.SetActive(false);
			plaqueText.text = Codebase.Utilities.LocalizationStrings.Get("QUEST_PROLOGUE_OBJECTIVE_PLAQUE") + "<br><size=150%>0/1</size>";
		}

		private void RenderButtonState()
		{
			// Original ELF 0x1635b60.
			plaqueState.SetActive(false);
			buttonState.SetActive(true);
			buttonStatusText.text = "<color=#77503A>" + Codebase.Utilities.LocalizationStrings.Get(Chain.ChainNameKey) + "</color>\n<color=#9F673F><size=0.8em>" + Codebase.Utilities.LocalizationStrings.Get("QUEST_POPUP_IN_PROGRESS") + "</color></color>";
			if (Chain.ChainIcon != null) chainIcon.sprite = Chain.ChainIcon;
			exclamation.SetActive(questModel.Chain.HasUnclaimedReward(Chain));
		}

		private void HandleOpen()
		{
			// Original ELF 0x1635de8.
			audioPlayer.PlaySoundOnce("tap");
			navigator.Open<QuestsWindowView>("Assets/Prefabs/UI/Mines/Quests/QuestsWindow.prefab", true).Setup(Chain);
		}
	}
}
