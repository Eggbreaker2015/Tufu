using Codebase.UI.Amulet;
using Codebase.Amulet.Flows;
using UniMob;
using Codebase.Utilities;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BrunoMikoski.AnimationSequencer;
using CodeBase.Audio;
using Codebase.Amulet;
using Codebase.Amulet.Upgrades;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Enemy;
using Codebase.Meta;
using Codebase.Player.Inventory;
using Codebase.Player.Models;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Codebase.UI.Inventory
{
	public class InventoryView : View
	{
		private class CategoryDefinition
		{
			public readonly ItemSlotType SlotType;

			public readonly string HeaderLocKey;

			public CategoryDefinition(ItemSlotType slotType, string headerLocKey)
			{
			// Native ELF VA 0x1652e9c.
			SlotType = slotType;
			HeaderLocKey = headerLocKey;
		}
		}

		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CPlayEmptyAmuletReveal_003Ed__54 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskVoidMethodBuilder _003C_003Et__builder;

			public InventoryView _003C_003E4__this;

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
		private struct _003CScrollItemsToStartDeferred_003Ed__64 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskVoidMethodBuilder _003C_003Et__builder;

			public InventoryView _003C_003E4__this;

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

		private const int FillSoulCost = 1;

		private const int AmuletPurchasableGlowMinCount = 5;

		private const int AmuletPurchasableGlowMaxCount = 15;

		private const float AmuletPurchasableGlowMinScale = 1f;

		private const float AmuletPurchasableGlowMaxScale = 1.1f;

		// Native ELF VA 0x1652c98; literal keys resolved through original ELF RELA.
        private static readonly CategoryDefinition[] AllCategories = {
            new CategoryDefinition(ItemSlotType.Weapon, "INVENTORY_WEAPONS"),
            new CategoryDefinition(ItemSlotType.Helmet, "INVENTORY_HELMETS"),
            new CategoryDefinition(ItemSlotType.Shield, "INVENTORY_SHIELDS")
        };

		[SerializeField]
		private ItemSlotView slotPrefab;

		[SerializeField]
		private StatView statPrefab;

		[SerializeField]
		private InventoryCategoryBlock categoryBlockPrefab;

		[SerializeField]
		private Transform slotsContainer;

		[SerializeField]
		private Transform statsContainer;

		[SerializeField]
		private GameObject itemPreviewRoot;

		[SerializeField]
		private GameObject itemPreviewMask;

		[SerializeField]
		private GameObject unequipMask;

		[FormerlySerializedAs("crossroadsButton")]
		[SerializeField]
		private Button amuletGraphButton;

		[Header("Amulet")]
		[SerializeField]
		private TMP_Text amuletLevelText;

		[SerializeField]
		private TMP_Text amuletExpText;

		[SerializeField]
		private Slider amuletExpSlider;

		[SerializeField]
		private GameObject amuletRequiresSoulState;

		[SerializeField]
		private GameObject amuletUnlockedState;

		[SerializeField]
		private Button fillAmuletButton;

		[SerializeField]
		private TMP_Text fillAmuletButtonText;

		[SerializeField]
		private AnimationSequencerController emptyAmuletAnimation;

		[FormerlySerializedAs("amuletRevealAnimation")]
		[SerializeField]
		private AnimationSequencerController amuletUnlockAnimation;

		[SerializeField]
		private Button searchSoulButton;

		[SerializeField]
		private GameObject amuletPurchasableNotification;

		[SerializeField]
		private TMP_Text amuletPurchasableCountText;

		[SerializeField]
		private RectTransform amuletPurchasableNotificationGlow;

		[SerializeField]
		private GameObject amuletNotificationHighlight;

		[SerializeField]
		private GameObject loadoutButton;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private AmuletModel amuletModel;

		[Inject]
		private AmuletUpgradesModel amuletUpgradesModel;

		[Inject]
		private UINavigator navigator;

		[Inject]
		private IAudioPlayer audioPlayer;

		[Inject]
		private SoulStyleConfig soulStyleConfig;

		[Inject]
		private EnemiesModel enemiesModel;

		[Inject]
		private MetaStatsModel metaStatsModel;

		[Inject]
		private UiModel uiModel;

		private bool unequipState;

		private bool pendingFocusOnClose;

		private ScrollRect itemsScrollRect;

		private bool showUnlockHelmetSlotAfter;

		public bool IsOpened => gameObject.activeSelf; // Native 0x1650fe0.

		public bool UnequipState => this.unequipState;

		private ScrollRect ItemsScrollRect
        {
            // Native ELF VA 0x1651008, component lookup starts at the serialized items container.
            get { if (itemsScrollRect == null) itemsScrollRect = slotsContainer.GetComponentInParent<ScrollRect>(); return itemsScrollRect; }
        }

		private List<ItemModel> unequippedItems => playerModel.Items
            .Where(item => playerModel.ItemSlots.All(slot => slot.ItemModel != item))
            .OrderByDescending(item => item.Rarity).ThenByDescending(item => item.EffectiveLevel)
            .ThenByDescending(item => item.IsArmor ? item.Armor : item.DPS).ToList(); // Native 0x1651080 and four original selectors.

		protected override void Start()
		{
			// Native ELF VA 0x16513ac and setup lambda 0x1652fd4.
			base.Start();
			new ViewContainer<InventoryCategoryBlock, CategoryDefinition>(slotsContainer, categoryBlockPrefab)
			    .BindTo(Lifetime, () => GetNonEmptyCategories())
			    .Setup((view, category) => view.Setup(Lifetime, category.HeaderLocKey, () => GetCategoryItems(category.SlotType)));
			amuletGraphButton.onClick.AddListener(OpenAmuletGraph);
			searchSoulButton.onClick.AddListener(HandleSearchSoul);
			fillAmuletButton.onClick.AddListener(HandleFillAmulet);
			// Pet mode: the 图鉴 entry button in the Stats panel.
			if (global::Codebase.Pet.PetConfig.Enabled) global::Codebase.UI.Pets.PetUiBuilder.InstallInventoryEntry(this);
		}

		public void OpenForAmuletReveal()
		{
			// Native ELF VA 0x1651638.
			pendingFocusOnClose = NeedsGiantSoulForUnlock();
			gameObject.SetActive(true);
		}

		public void OpenForEmptyAmuletReveal()
		{
			// Native ELF VA 0x16516f4.
			showUnlockHelmetSlotAfter = true;
			OpenForAmuletReveal();
			PlayEmptyAmuletReveal().Forget();
		}

		private async UniTaskVoid PlayEmptyAmuletReveal()
		{
			// Native MoveNext ELF VA 0x16534e0.
			await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
			audioPlayer.PlaySoundOnce("amulet_unlock_popup");
			emptyAmuletAnimation.Play();
		}

		private void HandleFillAmulet()
		{
			// Native ELF VA 0x16517c0.
			if (!amuletModel.TryFillWithSoul()) return;
			audioPlayer.PlaySoundOnce("amulet_unlock");
			amuletUnlockAnimation.Play();
		}

		private void HandleSearchSoul()
		{
			// Native ELF VA 0x16518b4.
			gameObject.SetActive(false);
			App.InitiateFlow(new FocusGiantSoulFlow()).Forget();
		}

		public override void Cleanup()
		{
			// Native ELF VA 0x1651954.
			base.Cleanup();
			uiModel.BlockItemsHotbar = true;
			if (pendingFocusOnClose)
			{
			    pendingFocusOnClose = false;
			    if (NeedsGiantSoulForUnlock())
			    {
			        App.InitiateFlow(new FocusGiantSoulFlow { showUnlockHelmetSlotAfter = showUnlockHelmetSlotAfter }).Forget();
			        showUnlockHelmetSlotAfter = false;
			    }
			}
		}

		private void OpenAmuletGraph()
		{
			// Native ELF VA 0x1651a18.
			if (playerModel.Data.SeenAmuletGraphTutorial.Value) OpenAmuletUpgradeGraph();
			else navigator.Open<AmuletTutorialView>("Assets/Prefabs/UI/Amulet/AmuletTutorial.prefab").Setup(OpenAmuletUpgradeGraph);
		}

		private void OpenAmuletUpgradeGraph()
		{
			// Native ELF VA 0x1651b48.
			audioPlayer.PlaySoundOnce("graph_open");
			navigator.Open<AmuletUpgradeGraphView>("Assets/Prefabs/UI/Amulet/AmuletUpgradesGraph.prefab");
		}

		private List<CategoryDefinition> GetNonEmptyCategories()
		{
			// Native ELF VA 0x1651c48; preserve static category order.
			List<ItemModel> items = unequippedItems;
			return AllCategories.Where(category => items.Any(item => item.Config.SlotType == category.SlotType)).ToList();
		}

		private List<ItemModel> GetCategoryItems(ItemSlotType slotType)
		{
			// Native ELF VA 0x1651d84.
			return unequippedItems.Where(item => item.Config.SlotType == slotType).ToList();
		}

		private void OnEnable()
		{
			// Native ELF VA 0x1651e90: original virtual Initialize before scrolling.
			Initialize();
			ScrollItemsToStart();
			if (uiModel != null) uiModel.BlockItemsHotbar = false;
		}

		private void ScrollItemsToStart()
		{
			// Native ELF VA 0x1651ecc.
			ItemsScrollRect.verticalNormalizedPosition = 1f;
			ScrollItemsToStartDeferred().Forget();
		}

		
		private async UniTaskVoid ScrollItemsToStartDeferred()
		{
			// Native async MoveNext 0x1653774; original Yield timing is enum value 13.
			await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
			ItemsScrollRect.verticalNormalizedPosition = 1f;
		}

		protected override void Render()
		{
			// Native ELF VA 0x1651fa4.
			ItemModel selectedItem = playerModel.SelectedItem;
			itemPreviewRoot.SetActive(selectedItem != null);
			itemPreviewMask.SetActive(selectedItem != null);
			unequipMask.gameObject.SetActive(unequipState);
			if (selectedItem == null) unequipState = false;
			RenderAmuletStates();
			loadoutButton.SetActive(metaStatsModel.Get("loadout_button").GetValue() > 0.0);
		}

		private void RenderAmuletStates()
		{
			// Native ELF VA 0x165209c and soul predicate 0x16532d8.
			double empty = amuletModel.EmptyAmuletCount; bool unlocked = empty <= 0.0 && amuletModel.IsUnlocked;
			amuletRequiresSoulState.SetActive(empty > 0.0); amuletUnlockedState.SetActive(unlocked);
			searchSoulButton.gameObject.SetActive(enemiesModel.Enemies.Any(enemy => enemy.SoulReward == "huge") && NeedsGiantSoulForUnlock());
			if (empty <= 0.0) { if (unlocked) RenderAmulet(); return; }
			bool enough = amuletModel.HasEnoughSouls("huge",1.0); fillAmuletButton.interactable = enough;
			fillAmuletButtonText.text = string.Format("{0} <sprite name={1}> <color={2}>{3}</color>", LocalizationStrings.Get("AMULET_UNLOCK_BUTTON"), soulStyleConfig.GetIcon("huge").name, enough ? "white" : "#FF4040", 1);
		}

		private bool NeedsGiantSoulForUnlock()
		{
			// Native ELF VA 0x1651670; original soul key is huge, cost exactly one.
			return amuletModel.EmptyAmuletCount > 0.0 && !amuletModel.HasEnoughSouls("huge", 1.0);
		}

		private void RenderAmulet()
		{
			// Native ELF VA 0x16524ac; original markup intentionally has no invented closing tags.
			amuletLevelText.text = LocalizationStrings.Get("REWARD_LEVEL").Replace("{value}",amuletModel.Level.ToString());
			amuletExpText.text = amuletModel.CurrentLevelExperience.Format()+"<color=#FFCCAF><size=75%>/"+amuletModel.NextLevelExperience.Format();
			amuletExpSlider.value = amuletModel.LevelProgress;
			RenderAmuletPurchasableNotification();
		}

		private void RenderAmuletPurchasableNotification()
		{
			// Native ELF VA 0x165266c; original glow thresholds >5 and highlight >=15.
			int count = amuletUpgradesModel.PurchasableNodeCount;
			amuletPurchasableNotification.SetActive(count > 0);
			if (count <= 0) { amuletNotificationHighlight.SetActive(false); return; }
			amuletPurchasableCountText.text = count.ToString();
			amuletNotificationHighlight.SetActive(count >= 15);
			amuletPurchasableNotificationGlow.gameObject.SetActive(count > 5);
			if (count > 5) amuletPurchasableNotificationGlow.localScale = Vector3.one * (1f + .1f * Mathf.Clamp01((count - 5) / 10f));
		}

		public void SelectItem(ItemModel model)
		{
			// Native ELF VA 0x1652804.
			if (!unequipState)
			{
			    playerModel.SelectedItem = model;
			    model.ShowNotification = false;
			    return;
			}
			PlayerModel current = GameContext.Get<PlayerModel>();
			if (current.EquippedItems.Any(item => item == model))
			{
			    current.UnequipItem(model);
			    current.EquipItem(playerModel.SelectedItem);
			    current.SelectedItem = null;
			    unequipState = false;
			}
		}

		public void Equip()
		{
			// Native ELF VA 0x16529d8; exact occupied/single-slot replacement branches.
			if (playerModel.SelectedItem == null) return;
			ItemSlotModel occupied = playerModel.ItemSlots.FirstOrDefault(slot => slot.ItemModel == playerModel.SelectedItem);
			if (unequipState) { unequipState = false; Render(); return; }
			if (occupied != null) playerModel.UnequipItem(playerModel.SelectedItem);
			else if (!playerModel.EquipItem(playerModel.SelectedItem))
			{
			    ItemSlotModel first = playerModel.ItemSlots.FirstOrDefault(slot => slot.IsUnlocked && slot.SlotType == playerModel.SelectedItem.Config.SlotType);
			    if (first != null && playerModel.ItemSlots.Count(slot => slot.IsUnlocked && slot.SlotType == playerModel.SelectedItem.Config.SlotType) == 1)
			    {
			        playerModel.UnequipItem(first.ItemModel);
			        playerModel.EquipItem(playerModel.SelectedItem);
			        playerModel.SelectedItem = null;
			    }
			    unequipState = true;
			    Render();
			    return;
			}
			playerModel.SelectedItem = null;
		}
	}
}
