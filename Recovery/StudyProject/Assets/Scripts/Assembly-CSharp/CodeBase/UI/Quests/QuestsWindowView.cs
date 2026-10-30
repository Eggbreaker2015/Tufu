using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CodeBase.Audio;
using Codebase.Analytics;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Player.Models;
using Codebase.Quests;
using Codebase.Rewards;
using Codebase.UI.Flows;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using TMPro;
using UniMob.Core;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using UniMob;

namespace Codebase.UI.Quests
{
	public class QuestsWindowView : PopupView
	{
		[CompilerGenerated]
		private sealed class _003C_003Ec__DisplayClass62_0
		{
			public UniTaskCompletionSource rewardCompletion;
			internal void _003CCompleteHubChain_003Eb__0()
			{
			}
		}

		[CompilerGenerated]
		private sealed class _003C_003Ec__DisplayClass63_0
		{
			public UniTaskCompletionSource rewardCompletion;
			internal void _003CCompleteSideChain_003Eb__0()
			{
			}
		}

		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CAnimateProgress_003Ed__74 : IAsyncStateMachine
		{
			public int _003C_003E1__state;
			public AsyncUniTaskMethodBuilder _003C_003Et__builder;
			public QuestsWindowView _003C_003E4__this;
			public int totalCount;
			public int targetCount;
			private float _003CstartValue_003E5__2;
			private float _003Celapsed_003E5__3;
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

		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CCloseAndPlayHubDeliveryDialogue_003Ed__54 : IAsyncStateMachine
		{
			public int _003C_003E1__state;
			public AsyncUniTaskVoidMethodBuilder _003C_003Et__builder;
			public QuestsWindowView _003C_003E4__this;
			public QuestHubTokenSlot slot;
			private UniTask.Awaiter _003C_003Eu__1;
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

		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CCompleteHubChain_003Ed__62 : IAsyncStateMachine
		{
			public int _003C_003E1__state;
			public AsyncUniTaskVoidMethodBuilder _003C_003Et__builder;
			public QuestsWindowView _003C_003E4__this;
			public QuestChainDef chainDef;
			private _003C_003Ec__DisplayClass62_0 _003C_003E8__1;
			private ItemSlotUnlockFlow? _003CunlockFlow_003E5__2;
			private UniTask.Awaiter _003C_003Eu__1;
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

		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CCompleteSideChain_003Ed__63 : IAsyncStateMachine
		{
			public int _003C_003E1__state;
			public AsyncUniTaskVoidMethodBuilder _003C_003Et__builder;
			public QuestsWindowView _003C_003E4__this;
			public QuestChainDef chainDef;
			private _003C_003Ec__DisplayClass63_0 _003C_003E8__1;
			private bool _003ChasFinale_003E5__2;
			private ItemSlotUnlockFlow? _003CunlockFlow_003E5__3;
			private UniTask.Awaiter _003C_003Eu__1;
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

		[CompilerGenerated]
		private sealed class _003CFindQuestCurrencyKeys_003Ed__70 : IEnumerable<string>, IEnumerable, IEnumerator<string>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;
			private string _003C_003E2__current;
			private int _003C_003El__initialThreadId;
			private IEnumerable<RewardBase> rewards;
			public IEnumerable<RewardBase> _003C_003E3__rewards;
			private IEnumerator<RewardBase> _003C_003E7__wrap1;
			private IEnumerator<string> _003C_003E7__wrap2;
			string IEnumerator<string>.Current
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
					return null;
				}
			}

			[DebuggerHidden]
			public _003CFindQuestCurrencyKeys_003Ed__70(int _003C_003E1__state)
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

			private void _003C_003Em__Finally1()
			{
			}

			private void _003C_003Em__Finally2()
			{
			}

			[DebuggerHidden]
			void IEnumerator.Reset()
			{
			}

			[DebuggerHidden]
			IEnumerator<string> IEnumerable<string>.GetEnumerator()
			{
				return null;
			}

			[DebuggerHidden]
			IEnumerator IEnumerable.GetEnumerator()
			{
				return null;
			}
		}

		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CPlayChainFinale_003Ed__66 : IAsyncStateMachine
		{
			public int _003C_003E1__state;
			public AsyncUniTaskMethodBuilder _003C_003Et__builder;
			public QuestChainDef chainDef;
			public QuestsWindowView _003C_003E4__this;
			private UniTask.Awaiter _003C_003Eu__1;
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

		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CResetQuestsScroll_003Ed__46 : IAsyncStateMachine
		{
			public int _003C_003E1__state;
			public AsyncUniTaskVoidMethodBuilder _003C_003Et__builder;
			public QuestsWindowView _003C_003E4__this;
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

		private const float FinaleCameraResetDelay = 0.5f;
		[SerializeField]
		private Button closeBackgroundButton;
		[SerializeField]
		private Button completeButton;
		[SerializeField]
		private TMP_Text titleText;
		[SerializeField]
		private TMP_Text descriptionText;
		[SerializeField]
		private TMP_Text progressAmountText;
		[SerializeField]
		private Slider progressSlider;
		[SerializeField]
		private GameObject timerContent;
		[SerializeField]
		private TMP_Text timerText;
		[SerializeField]
		private TMP_Text completeButtonReadyText;
		[SerializeField]
		private TMP_Text completeButtonTimerLabelText;
		[SerializeField]
		private TMP_Text completeButtonTimerCostText;
		[SerializeField]
		private Transform chainRewardsContainer;
		[SerializeField]
		private QuestRewardItemView chainRewardItemPrefab;
		[SerializeField]
		private Transform questsContainer;
		[SerializeField]
		private QuestItemView questItemPrefab;
		[Inject]
		private QuestModel questModel;
		[Inject]
		private QuestConfig questConfig;
		[Inject]
		private PlayerModel playerModel;
		[Inject]
		private IAnalyticsWrapper analytics;
		[Inject]
		private UINavigator navigator;
		[Inject]
		private QuestMobSpawnerService questMobSpawnerService;
		[Inject]
		private IAudioPlayer audioPlayer;
		private ViewContainer<QuestItemView, QuestWindowListEntry> questsViewContainer;
		private ViewContainer<QuestRewardItemView, RewardBase> chainRewardsViewContainer;
		private QuestChainDef chain;
		private float secondTimer;
		private bool isChainFinaleRunning;
		private bool isProgressAnimating;
		private ScrollRect questsScroll;
		[CompilerGenerated]
		private List<QuestWindowListEntry> __ListEntries__BackingField;
		[CompilerGenerated]
		private List<RewardBase> __ChainRewards__BackingField;
		[CompilerGenerated]
		private int __TimerTick__BackingField;
		private ComputedAtom<List<QuestWindowListEntry>> __ListEntries;
		private ComputedAtom<List<RewardBase>> __ChainRewards;
		private ComputedAtom<int> __TimerTick;
		private List<QuestWindowListEntry> ListEntries
		{
			[CompilerGenerated]
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__ListEntries == null)
					__ListEntries = CodeGenAtom.CreatePooled(this, null, () => ListEntries, false);
				return __ListEntries.DirectEvaluate() ? __ListEntries__BackingField : __ListEntries.Value;
			}

			[CompilerGenerated]
			set
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__ListEntries != null && !__ListEntries.CompareAndInvalidate(value))
					return;
				__ListEntries__BackingField = value;
			}
		}

		private List<RewardBase> ChainRewards
		{
			[CompilerGenerated]
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__ChainRewards == null)
					__ChainRewards = CodeGenAtom.CreatePooled(this, null, () => ChainRewards, false);
				return __ChainRewards.DirectEvaluate() ? __ChainRewards__BackingField : __ChainRewards.Value;
			}

			[CompilerGenerated]
			set
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__ChainRewards != null && !__ChainRewards.CompareAndInvalidate(value))
					return;
				__ChainRewards__BackingField = value;
			}
		}

		private int TimerTick
		{
			[CompilerGenerated]
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__TimerTick == null)
					__TimerTick = CodeGenAtom.CreatePooled(this, null, () => TimerTick, false);
				return __TimerTick.DirectEvaluate() ? __TimerTick__BackingField : __TimerTick.Value;
			}

			[CompilerGenerated]
			set
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__TimerTick != null && !__TimerTick.CompareAndInvalidate(value))
					return;
				__TimerTick__BackingField = value;
			}
		}

		protected override void Awake()
		{ // Original ELF 0x1636864; candidate only.
			base.Awake();
			closeBackgroundButton.onClick.AddListener(CloseAndFocusEnemy);
			completeButton.onClick.AddListener(HandleCompleteButton);
		}

		protected override void Start()
		{ // Original ELF 0x1636958; candidate only.
			base.Start();
			questsViewContainer = new ViewContainer<QuestItemView, QuestWindowListEntry>(questsContainer, questItemPrefab).BindTo(Lifetime, () => ListEntries).Setup((view, entry) =>
			{
				if (entry.IsHubToken)
					view.SetupHubToken(chain, entry.HubSlot, () => HandleHubTokenDelivered(entry.HubSlot), CloseAndFocusEnemy);
				else
					view.Setup(entry.Step, CloseAndFocusEnemy, () => HandleStepRewardClaimed(entry.Step));
			});
			chainRewardsViewContainer = new ViewContainer<QuestRewardItemView, RewardBase>(chainRewardsContainer, chainRewardItemPrefab).BindTo(Lifetime, () => ChainRewards).Setup((view, reward) => view.Setup(reward));
		}

		private void Update()
		{ // Original ELF 0x1636cac; candidate only.
			if (chain == null)
				return;
			secondTimer += Time.deltaTime;
			if (!(secondTimer < 1f))
			{
				secondTimer = 0f;
				TimerTick = unchecked(TimerTick + 1);
			}
		}

		public void Setup(QuestChainDef chain)
		{ // Original ELF 0x1635f04; candidate only.
			this.chain = chain;
			questModel.ReleaseQuestRoadHold(chain.ChainKey);
			if (chain.Role == QuestChainRole.Hub)
			{
				var slot = questModel.Hub.GetInitialDeliverableSlot(chain);
				questModel.DeliverInitialHubTokenIfNeeded(chain);
				PlayHubDeliveryDialogue(slot);
				ListEntries = chain.HubTokenSlots.Select(QuestWindowListEntry.FromHubSlot).ToList();
			}
			else
				ListEntries = chain.Steps.Select(QuestWindowListEntry.FromStep).ToList();
			ChainRewards = chain.ChainReward?.ToList() ?? new List<RewardBase>();
			if (global::Codebase.Rpg.RpgModeConfig.Enabled) ChainRewards = global::Codebase.Rpg.RpgRewardFilter.Filter(ChainRewards);
			Initialize();
			ResetQuestsScroll().Forget();
		}

		private async UniTaskVoid ResetQuestsScroll()
		{ // Original ELF 0x1636de8; candidate only.
			if (questsScroll == null)
				questsScroll = questsContainer.GetComponentInParent<ScrollRect>(true);
			if (questsScroll == null)
				return;
			await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
			Canvas.ForceUpdateCanvases();
			questsScroll.StopMovement();
			questsScroll.verticalNormalizedPosition = 1f;
			int active = GetActiveEntryIndex();
			if (active >= 0)
				ScrollToEntry(active);
		}

		private int GetActiveEntryIndex()
		{ // Original ELF 0x1636e74; candidate only.
			var entries = ListEntries;
			if (entries != null)
				for (int i = 0; i < entries.Count; i++)
					if (IsEntryActionable(entries[i]))
						return i;
			return -1;
		}

		private bool IsEntryActionable(QuestWindowListEntry entry)
		{ // Original ELF 0x1636f24; candidate only.
			if (entry.IsHubToken)
				return questModel.Hub.CanDeliverHubToken(chain, entry.HubSlot.TokenKey) || questModel.Hub.ShouldShowHubTokenFind(chain, entry.HubSlot);
			var step = entry.Step;
			if (!questModel.IsStepGiven(step.StepKey))
				return false;
			bool complete = questModel.IsStepComplete(step.StepKey);
			bool claimed = questModel.IsStepRewardClaimed(step.StepKey);
			return !(complete && claimed);
		}

		private void ScrollToEntry(int index)
		{ // Original ELF 0x1637018; candidate only.
			var content = questsScroll.content;
			var viewport = questsScroll.viewport;
			if (content == null || viewport == null || index < 0 || index >= questsContainer.childCount)
				return;
			Canvas.ForceUpdateCanvases();
			var entry = (RectTransform)questsContainer.GetChild(index);
			var viewportCorners = new Vector3[4];
			var entryCorners = new Vector3[4];
			viewport.GetWorldCorners(viewportCorners);
			entry.GetWorldCorners(entryCorners);
			float margin = (viewportCorners[1].y - viewportCorners[0].y) * .1f;
			float top = viewportCorners[1].y - margin;
			float delta;
			if (entryCorners[1].y > top)
				delta = entryCorners[1].y - top;
			else
			{
				float bottom = viewportCorners[0].y + margin;
				if (!(entryCorners[0].y < bottom))
					return;
				delta = entryCorners[0].y - bottom;
			}

			var position = content.anchoredPosition;
			content.anchoredPosition = new Vector2(position.x, position.y - delta);
		}

		private void PlayHubDeliveryDialogue(QuestHubTokenSlot slot)
		{ // Original ELF 0x1636d10; candidate only.
			if (slot == null || slot.DeliveryMonologue == null || slot.DeliveryMonologue.Count == 0)
				return;
			var giver = questConfig.GetGiverByHubChain(chain.ChainKey);
			var view = navigator.Open<QuestNpcDialogueView>("Assets/Prefabs/UI/Mines/Dialogue/DialogueWindow.prefab");
			view.Setup(giver.Portrait, slot.DeliveryNpcNameKey, slot.DeliveryMonologue, null);
		}

		private void HandleStepRewardClaimed(QuestStepDef step)
		{ // Original ELF 0x1637228; candidate only.
			AnimateProgress(questModel.Chain.GetClaimedStepsCount(chain), chain.Steps.Count).Forget();
			Initialize();
		}

		private void HandleHubTokenDelivered(QuestHubTokenSlot slot)
		{ // Original ELF 0x1637364; candidate only.
			if (!questModel.Hub.IsHubAllTokensDelivered(chain) && HasDeliveryDialogue(slot))
			{
				CloseAndPlayHubDeliveryDialogue(slot).Forget();
				return;
			}

			AnimateProgress(questModel.Hub.GetHubDeliveredCount(chain), chain.HubTokenSlots.Count).Forget();
			Initialize();
		}

		private bool HasDeliveryDialogue(QuestHubTokenSlot slot)
		{ // Original ELF 0x1637484; candidate only.
			return slot != null && slot.DeliveryMonologue != null && slot.DeliveryMonologue.Count > 0;
		}

		private async UniTaskVoid CloseAndPlayHubDeliveryDialogue(QuestHubTokenSlot slot)
		{ // Original ELF 0x16374dc; candidate only.
			string chainKey = chain.ChainKey, tokenKey = slot.TokenKey;
			Close();
			await App.InitiateFlow(new Codebase.Quests.Flows.QuestHubDeliveryDialogueFlow { ChainKey = chainKey, TokenKey = tokenKey });
		}

		protected override void Render()
		{ // Original ELF 0x1637588; candidate only.
			if (chain == null)
				return;
			_ = TimerTick;
			titleText.text = Codebase.Utilities.LocalizationStrings.Get(chain.ChainNameKey);
			descriptionText.text = Codebase.Utilities.LocalizationStrings.Get(chain.ChainDescriptionKey);
			RenderProgress();
			bool complete;
			if (chain.Role == QuestChainRole.Hub)
				complete = questModel.Hub.IsHubAllTokensDelivered(chain);
			else
				complete = questModel.Chain.IsChainComplete(chain) && !questModel.Chain.HasUncollectedStepReward(chain);
			if (complete && !questModel.IsChainRewardClaimed(chain.ChainKey))
				RenderCompleteState();
			else
				RenderInProgressState();
		}

		private void RenderProgress()
		{ // Original ELF 0x16376cc; candidate only.
			int value = chain.Role == QuestChainRole.Hub ? questModel.Hub.GetHubDeliveredCount(chain) : questModel.Chain.GetClaimedStepsCount(chain);
			int total = chain.Role == QuestChainRole.Hub ? chain.HubTokenSlots.Count : chain.Steps.Count;
			progressAmountText.text = string.Format("{0}/{1}", value, total);
			progressSlider.maxValue = total;
			if (!isProgressAnimating)
				progressSlider.value = value;
		}

		private void RenderInProgressState()
		{ // Original ELF 0x1637a60; candidate only.
			completeButton.gameObject.SetActive(false);
			timerContent.SetActive(false);
			completeButtonReadyText.gameObject.SetActive(false);
			completeButtonTimerLabelText.gameObject.SetActive(false);
			completeButtonTimerCostText.gameObject.SetActive(false);
		}

		private void RenderCompleteState()
		{ // Original ELF 0x1637848; candidate only.
			bool timer = questModel.IsChainRewardClaimTimerActive(chain.ChainKey);
			completeButton.gameObject.SetActive(true);
			timerContent.SetActive(timer);
			completeButtonReadyText.gameObject.SetActive(!timer);
			completeButtonTimerLabelText.gameObject.SetActive(timer);
			completeButtonTimerCostText.gameObject.SetActive(false);
			if (!timer)
				return;
			int remaining = questModel.GetChainRewardClaimRemainingSeconds(chain.ChainKey);
			timerText.text = "<sprite name=atlas_56> " + Codebase.Utilities.TimeLocalizer.GetFormatTime(remaining, true);
			int cost = questModel.GetSkipChainRewardClaimGemCost(chain.ChainKey);
			completeButtonTimerLabelText.text = string.Format("{0} <sprite name=gem>{1}", Codebase.Utilities.LocalizationStrings.Get("QUEST_SKIP"), cost);
		}

		private void HandleCompleteButton()
		{ // Original ELF 0x1637b04; candidate only.
			if (chain == null)
				return;
			if (questModel.IsChainRewardClaimTimerActive(chain.ChainKey))
				HandleSkipChainRewardClaimTimer();
			else
				HandleCompleteChain();
		}

		private void HandleSkipChainRewardClaimTimer()
		{ // Original ELF 0x1637b50; candidate only.
			if (chain == null)
				return;
			int cost = questModel.GetSkipChainRewardClaimGemCost(chain.ChainKey);
			if (cost < 1)
				return;
			if (!(playerModel.Gems < (double)cost))
			{
				audioPlayer.PlaySoundOnce("shop_gems_small");
				playerModel.Gems -= cost;
				analytics.Send(new Codebase.Analytics.Events.HardCurrencySpentEvent("quest_skip_chain_reward_claim", cost, new Dictionary<string, object> { { "chain_key", chain.ChainKey } }));
				questModel.ClearChainRewardClaimTimer(chain.ChainKey);
			}
			else
				navigator.Open<Codebase.UI.Shop.BuyGemsPopupView>("Assets/Prefabs/UI/Rewarded/BuyGemsPopup.prefab").SetupAsGems(cost);
		}

		private void HandleCompleteChain()
		{ // Original ELF 0x1637e74; candidate only.
			if (chain == null)
				return;
			if (chain.Role == QuestChainRole.Hub)
			{
				if (!questModel.Hub.IsHubAllTokensDelivered(chain) || questModel.IsChainRewardClaimed(chain.ChainKey) || isChainFinaleRunning)
					return;
				CompleteHubChain(chain).Forget();
			}
			else
			{
				if (!questModel.Chain.IsChainComplete(chain) || questModel.IsChainRewardClaimed(chain.ChainKey) || isChainFinaleRunning)
					return;
				CompleteSideChain(chain).Forget();
			}
		}

		private async UniTaskVoid CompleteHubChain(QuestChainDef chainDef)
		{ // Original ELF 0x1637f58; candidate only.
			isChainFinaleRunning = true;
			audioPlayer.PlaySoundOnce("mail_claim");
			try
			{
				int total = chainDef.HubTokenSlots.Count;
				await AnimateProgress(total, total);
				Close();
				var unlock = ResolveItemSlotUnlockFlow(chainDef);
				var claimed = new UniTaskCompletionSource();
				App.ExecuteCommand(new Codebase.Quests.Commands.ClaimQuestChainRewardCommand { ChainKey = chainDef.ChainKey, OnComplete = () => claimed.TrySetResult() });
				await claimed.Task;
				if (unlock.HasValue)
					await App.InitiateFlow(unlock.GetValueOrDefault());
				FocusHubQuestGiverIfNeeded(chainDef);
				await App.InitiateFlow(new Codebase.Quests.Flows.QuestHubFinaleDialogueFlow { ChainKey = chainDef.ChainKey });
				await App.InitiateFlow(new Codebase.Quests.Flows.QuestCutsceneFlow { StepKey = "quest_hub_finale" });
				questModel.MarkGiven("quest_hub_finale");
				questModel.MarkChainRewardClaimed(chainDef.ChainKey);
				App.ExecuteCommand(new Codebase.Quests.Commands.FocusNextQuestGiverCommand { CompletedChainKey = chainDef.ChainKey });
			}
			finally
			{
				isChainFinaleRunning = false;
			}
		}

		private async UniTaskVoid CompleteSideChain(QuestChainDef chainDef)
		{ // Original ELF 0x1638008; candidate only.
			isChainFinaleRunning = true;
			audioPlayer.PlaySoundOnce("mail_claim");
			try
			{
				int total = chainDef.Steps.Count;
				bool finale = HasChainFinale(chainDef);
				await AnimateProgress(total, total);
				Close();
				var unlock = ResolveItemSlotUnlockFlow(chainDef);
				var claimed = new UniTaskCompletionSource();
				App.ExecuteCommand(new Codebase.Quests.Commands.ClaimQuestChainRewardCommand { ChainKey = chainDef.ChainKey, OnComplete = () => claimed.TrySetResult() });
				await claimed.Task;
				if (unlock.HasValue)
					await App.InitiateFlow(unlock.GetValueOrDefault());
				FocusHubQuestGiverIfNeeded(chainDef);
				if (finale)
				{
					await PlayChainFinale(chainDef);
					await UniTask.Delay(TimeSpan.FromSeconds(.5));
				}

				questModel.MarkChainRewardClaimed(chainDef.ChainKey);
				if (finale && TryOpenHubWindowForCompletedChain(chainDef))
					return;
				App.ExecuteCommand(new Codebase.Quests.Commands.FocusNextQuestGiverCommand { CompletedChainKey = chainDef.ChainKey });
			}
			finally
			{
				isChainFinaleRunning = false;
			}
		}

		private bool TryOpenHubWindowForCompletedChain(QuestChainDef completedChain)
		{ // Original ELF 0x16380b8; candidate only.
			var hub = questConfig.GetHubChainForTokenSource(completedChain.ChainKey);
			if (hub == null || !questModel.Chain.IsChainUnlocked(hub) || questModel.Chain.IsChainFullyComplete(hub))
				return false;
			navigator.Open<QuestsWindowView>("Assets/Prefabs/UI/Mines/Quests/QuestsWindow.prefab", true).Setup(hub);
			return true;
		}

		private bool HasChainFinale(QuestChainDef chainDef)
		{ // Original ELF 0x16381a4; candidate only.
			if (chainDef.Steps.Count == 0)
				return false;
			return questModel.StepFlow.ShouldPlayFinaleOnChainComplete(chainDef.Steps[chainDef.Steps.Count - 1]);
		}

		private async UniTask PlayChainFinale(QuestChainDef chainDef)
		{ // Original ELF 0x163824c; candidate only.
			if (chainDef.Role == QuestChainRole.Hub || chainDef.Steps.Count == 0)
				return;
			var step = chainDef.Steps[chainDef.Steps.Count - 1];
			if (!questModel.StepFlow.ShouldPlayFinaleOnChainComplete(step))
				return;
			var target = QuestNpcLocator.FindForChain(chainDef);
			if (target == null)
				return;
			await App.InitiateFlow(new Codebase.Quests.Flows.QuestDialogueFlow { StepKey = step.StepKey, CameraTarget = target });
		}

		private ItemSlotUnlockFlow? ResolveItemSlotUnlockFlow(QuestChainDef chainDef)
		{ // Original ELF 0x1638304; candidate only.
			if (global::Codebase.Rpg.RpgModeConfig.Enabled) return null;
			var reward = FindItemSlotReward(chainDef.ChainReward);
			return reward == null ? (ItemSlotUnlockFlow? )null : ItemSlotUnlockFlow.For(reward.SlotKey, true);
		}

		private static ItemSlotReward FindItemSlotReward(IEnumerable<RewardBase> rewards)
		{ // Original ELF 0x163833c; candidate only.
			foreach (var reward in rewards)
			{
				if (reward is ItemSlotReward slot)
					return slot;
				if (reward is BundleReward bundle)
				{
					var nested = FindItemSlotReward(bundle.Rewards);
					if (nested != null)
						return nested;
				}
			}

			return null;
		}

		private void FocusHubQuestGiverIfNeeded(QuestChainDef chainDef)
		{ // Original ELF 0x1638674; candidate only.
			foreach (string key in FindQuestCurrencyKeys(chainDef.ChainReward))
				App.ExecuteCommand(new Codebase.Quests.Commands.FocusHubQuestGiverCommand { TokenKey = key });
		}

		private static IEnumerable<string> FindQuestCurrencyKeys(IEnumerable<RewardBase> rewards)
		{ // Original ELF 0x1638978; candidate only.
			foreach (var reward in rewards)
			{
				if (reward is CurrencyReward currency && Codebase.Economy.CurrencyKeys.IsQuestCurrency(currency.Key))
					yield return currency.Key;
				else if (reward is BundleReward bundle)
					foreach (string key in FindQuestCurrencyKeys(bundle.Rewards))
						yield return key;
			}
		}

		private void CloseAndFocusEnemy()
		{ // Original ELF 0x16389ec; candidate only.
			audioPlayer.PlaySoundOnce("tap");
			FocusActiveEnemy();
			Close();
		}

		private void FocusActiveEnemy()
		{ // Original ELF 0x1638ac0; candidate only.
			if (chain == null)
				return;
			if (chain.Role == QuestChainRole.Hub)
			{
				FocusNextQuestGiver();
				return;
			}

			string stepKey = questModel.Chain.GetActiveHuntStepKey(chain);
			if (stepKey == null)
				return;
			var step = chain.Steps.FirstOrDefault(s => s.StepKey == stepKey);
			if (step == null)
				return;
			questModel.ReleaseQuestMobSpawn(stepKey);
			questMobSpawnerService.SyncStep(stepKey, step, true, false);
			if (questMobSpawnerService.TryGetMobTransform(stepKey, out var target))
				App.InitiateFlow(new Codebase.Quests.Flows.QuestMobSpawnedFlow { TargetTransform = target, SpawnVfx = questMobSpawnerService.GetMobSpawnVfx(stepKey) }).Forget();
		}

		private void FocusNextQuestGiver()
		{ // Original ELF 0x1638cfc; candidate only.
			var next = questModel.Popup.GetSideChainForObjectivePlaque();
			if (next == null)
				return;
			var target = QuestNpcLocator.FindForChain(next);
			if (target == null)
				return;
			App.InitiateFlow(new Codebase.Quests.Flows.QuestMobSpawnedFlow { TargetTransform = target }).Forget();
		}

		private async UniTask AnimateProgress(int targetCount, int totalCount)
		{ // Original ELF 0x16372b4; candidate only.
			isProgressAnimating = true;
			progressSlider.maxValue = totalCount;
			float from = progressSlider.value, elapsed = 0f;
			do
			{
				elapsed += Time.deltaTime;
				float value = Mathf.Lerp(from, targetCount, elapsed + elapsed);
				progressSlider.value = value;
				progressAmountText.text = string.Format("{0}/{1}", Mathf.RoundToInt(value), totalCount);
				await UniTask.Yield();
			}
			while (elapsed < .5f);
			progressSlider.value = targetCount;
			progressAmountText.text = string.Format("{0}/{1}", targetCount, totalCount);
			isProgressAnimating = false;
		}
	}
}
