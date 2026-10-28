using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BrunoMikoski.AnimationSequencer;
using CodeBase.Audio;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Pet;
using Codebase.Player;
using Codebase.Player.Inventory;
using Codebase.Player.Models;
using Codebase.Rewards;
using Codebase.UI.Inventory;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using Codebase.Unit;
using Codebase.Utilities;

namespace Codebase.UI.Shop
{
	public class ChestOpenView : PopupView
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CWaitForTap_003Ed__46 : IAsyncStateMachine
		{
			public int _003C_003E1__state;
			public AsyncUniTaskMethodBuilder _003C_003Et__builder;
			public ChestOpenView _003C_003E4__this;
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
		private struct _003CWaitForTapOrAnimationEnd_003Ed__47 : IAsyncStateMachine
		{
			public int _003C_003E1__state;
			public AsyncUniTaskMethodBuilder _003C_003Et__builder;
			public ChestOpenView _003C_003E4__this;
			private AnimationSequencerController _003Ccontroller_003E5__2;
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

		[SerializeField]
		private AnimationSequencerController animationController;
		[SerializeField]
		private AnimationSequencerController statAnimationController;
		[SerializeField]
		private GameObject root;
		[SerializeField]
		private Button openButton;
		[SerializeField]
		private List<Image> glows;
		[SerializeField]
		private Image closedChest;
		[SerializeField]
		private Image openedChest;
		[SerializeField]
		private TMP_Text itemName;
		[SerializeField]
		private GameObject tapToOpen;
		[Header("Item State")]
		[SerializeField]
		private GameObject itemState;
		[SerializeField]
		private ItemAbilitiesView abilitiesView;
		[SerializeField]
		private ItemAbilityView abilityView;
		[SerializeField]
		private InventoryItemView itemView;
		[SerializeField]
		private TMP_Text damageName;
		[SerializeField]
		private TMP_Text damagePrevious;
		[SerializeField]
		private TMP_Text damageNext;
		[SerializeField]
		private GameObject damageNextIcon;
		[Header("Stat State")]
		[SerializeField]
		private GameObject statState;
		[SerializeField]
		private Image statIcon;
		[SerializeField]
		private TMP_Text statName;
		[SerializeField]
		private TMP_Text statValue;
		[SerializeField]
		private List<Image> remainingChests;
		[SerializeField]
		private TMP_Text levelText;
		[SerializeField]
		private TMP_Text copiesText;
		[Inject]
		private ItemsProvider itemsProvider;
		[Inject]
		private IAudioPlayer audioPlayer;
		[Inject]
		private IconStyleConfig iconStyleConfig;
		[Inject]
		private PlayerModel playerModel;
		private bool tapReceived;
		private bool animationFinished;
		private List<ItemModel> droppedItems;
		private List<RewardBase> rewards;
		private int currentChestIndex;
		private bool isStatReward;
		private new void Awake()
		{
			base.Awake();
			animationController.OnFinishedEvent.AddListener(OnAnimationFinished);
			if (statAnimationController != null)
				statAnimationController.OnFinishedEvent.AddListener(OnAnimationFinished);
			openButton.onClick.AddListener(OnButtonTap);
			SetUserCloseCallback(() => SetActive(false));
		}

		public void SetActive(bool active)
		{
			root.SetActive(active);
			tapToOpen.SetActive(active);
			tapReceived = false;
			animationFinished = true;
		}

		public void SetChestCount(int count)
		{
			for (int i = 0; i < remainingChests.Count; i++)
			{
				remainingChests[i].sprite = closedChest.sprite;
				remainingChests[i].gameObject.SetActive(i < count - 1);
			}
		}

		public void SetDrop(ItemModel model, int copiesGained = 10)
		{
			isStatReward = false;
			if (statState != null)
				statState.SetActive(false);
			if (itemState != null)
				itemState.SetActive(true);
			model.ShowNotification = false;
			itemView.Initialize(model, true, false);
			bool preDivine = model.IsPreDivine;
			if (abilitiesView != null)
			{
				abilitiesView.gameObject.SetActive(preDivine && model.HasAbility);
				if (preDivine && model.HasAbility)
					abilitiesView.Setup(model, 2, true);
			}
			else if (abilityView != null)
			{
				abilityView.gameObject.SetActive(preDivine && model.HasAbility);
				if (preDivine && model.HasAbility)
					abilityView.Setup(model, 0, true, false);
			}

			PetStatsText.SetItemName(itemName, model.Config);
			damageName.text = LocalizationStrings.Get(model.IsArmor ? "STAT_Armor" : "STAT_Damage");
			int currentLevel = model.EffectiveLevel;
			var styles = GameContext.Get<IconStyleConfig>();
			Sprite sprite = model.IsArmor ? styles.Styles.First(style => style.StatType == model.Config.DamageStatTypeFlat).Sprite : styles.WeaponTypeStyles.First(style => style.WeaponType == model.Config.DamageType).Sprite;
			int previousLevel = currentLevel;
			double previous;
			if (preDivine)
			{
				previousLevel = Math.Max(1, unchecked(currentLevel - copiesGained));
				previous = model.IsArmor ? model.CalcArmor(previousLevel) : model.CalcDamage(previousLevel);
			}
			else
			{
				int previousCopies = Math.Max(0, unchecked(model.Copies - copiesGained));
				previous = model.IsArmor ? model.CalcArmor(model.Level, previousCopies) : model.CalcDamage(model.Level, previousCopies);
			}

			double current = model.IsArmor ? model.Armor : model.Damage;
			// Pet mode: a weapon shows its pet's attack before and after the drop.
			if (PetStats.TryGet(model, out PetStatsInfo pet))
			{
				previous = PetStatsText.AttackAt(pet, previous, preDivine ? currentLevel - previousLevel : Math.Min(copiesGained, model.Copies));
				current = pet.Attack;
			}
			// Preserve the original ordered float comparisons, including unordered NaN behavior.
			float abs = Mathf.Abs((float)previous), nextAbs = Mathf.Abs((float)current);
			if (abs <= nextAbs)
				abs = nextAbs;
			float tolerance = abs * 0.000001f, epsilon = Mathf.Epsilon * 8f;
			if (tolerance <= epsilon)
				tolerance = epsilon;
			float difference = Mathf.Abs((float)current - (float)previous);
			damageNext.gameObject.SetActive(tolerance <= difference);
			damageNextIcon.gameObject.SetActive(tolerance <= difference);
			if (tolerance > difference)
				previous = current;
			damagePrevious.text = "<sprite name=" + sprite.name + ">" + previous.Format();
			damageNext.text = current.Format();
			levelText.text = preDivine && !(difference < tolerance) ? string.Format("{0} <color=green>> {1}", previousLevel, currentLevel) : currentLevel.ToString();
			SetCopiesText(model, copiesGained, !preDivine);
			foreach (var glow in glows)
				glow.color = itemsProvider.GetRarityColor(model.Rarity);
		}

		private void HideCopiesText()
		{
			copiesText.gameObject.SetActive(false);
		}

		private void SetCopiesText(ItemModel model, int copiesGained, bool isEvolved)
		{
			if (copiesGained > 0 && isEvolved)
			{
				copiesText.gameObject.SetActive(true);
				copiesText.text = string.Format("{0}/{1} <color=green>(+{2})</color>", model.Copies, model.LevelsToEvolve, copiesGained);
			}
			else
				HideCopiesText();
		}

		public void SetDroppedItems(List<ItemModel> items)
		{
			droppedItems = items;
			currentChestIndex = 0;
		}

		public void SetRewards(List<RewardBase> rewards)
		{
			this.rewards = rewards;
			currentChestIndex = 0;
		}

		public void SetReward(RewardBase reward)
		{
			if (reward is StatReward stat)
				SetStatReward(stat);
			else if (reward is GemsReward gems)
				SetGemsReward(gems);
			else if (reward is ItemReward item)
				SetItemReward(item);
		}

		private void SetStatReward(StatReward statReward)
		{
			isStatReward = true;
			HideCopiesText();
			if (itemState != null)
				itemState.SetActive(false);
			if (statState != null)
				statState.SetActive(true);
			StatType type = statReward.BakedData == null ? statReward.Type : statReward.BakedData.Type;
			float value = statReward.BakedData == null ? statReward.Value : statReward.BakedData.Value;
			StatType iconType = type.GetIconStatType();
			var style = iconStyleConfig.Styles.FirstOrDefault(item => item.StatType == iconType);
			statIcon.sprite = style.Sprite;
			statIcon.enabled = true;
			statName.text = LocalizationStrings.Get(string.Format("STAT_{0}", type));
			var modifier = new StatModifier
			{
				Value = value,
				ModifierType = statReward.CustomModifier ? statReward.ModifierType : ModifierType.Flat
			};
			statValue.text = statReward.CustomModifier && statReward.ModifierType == ModifierType.EventPercentModifier ? "+" + ((double)value).Format() + "%" : modifier.ToString();
			itemName.text = LocalizationStrings.Get(string.Format("STAT_{0}", type));
			foreach (var glow in glows)
				glow.color = new Color(1f, 235f / 255f, 4f / 255f, 1f);
		}

		private void SetGemsReward(GemsReward gemsReward)
		{
			isStatReward = true;
			HideCopiesText();
			if (itemState != null)
				itemState.SetActive(false);
			if (statState != null)
				statState.SetActive(true);
			statIcon.sprite = iconStyleConfig.GemsSprite;
			statIcon.enabled = true;
			statName.text = LocalizationStrings.Get("CURRENCY_Gems");
			statValue.text = "+" + gemsReward.Gems.Format();
			itemName.text = LocalizationStrings.Get("CURRENCY_Gems");
			foreach (var glow in glows)
				glow.color = new Color(1f, 0.447f, 0.98f, 1f);
		}

		private void SetItemReward(ItemReward itemReward)
		{
			ItemConfig config = itemReward.Def;
			if (itemReward.BakedData != null)
				config = itemsProvider.Get(itemReward.BakedData.ItemKey);
			var existing = playerModel.Items.FirstOrDefault(item => item.Config == config);
			var display = new ItemModel(Lifetime, config);
			int copies = itemReward.Level;
			if (existing != null)
			{
				if (existing.Evolved)
				{
					display.Evolved = true;
					display.Level = existing.Level;
				}

				copies = itemReward.BakedTotalCopies ?? existing.Copies;
			}

			display.SetInitialCopies(copies);
			SetDrop(display, itemReward.Level);
		}

		public async UniTask WaitForTap()
		{
			await UniTask.WaitUntil(() => tapReceived);
			tapReceived = false;
		}

		public async UniTask WaitForTapOrAnimationEnd()
		{
			tapReceived = false;
			animationFinished = false;
			var controller = GetCurrentAnimationController();
			controller.Complete(true);
			controller.ClearPlayingSequence();
			controller.ResetToInitialState();
			PlayAnimation();
			tapToOpen.SetActive(false);
			await UniTask.WaitUntil(() => tapReceived || animationFinished);
			if (!animationFinished)
				controller.Complete(true);
			tapReceived = false;
			currentChestIndex++;
			await UniTask.CompletedTask;
		}

		public void ResetAnimation()
		{
			animationController.ClearPlayingSequence();
			animationController.ResetToInitialState();
			if (statAnimationController != null)
			{
				statAnimationController.ClearPlayingSequence();
				statAnimationController.ResetToInitialState();
			}
		}

		private void PlayAnimation()
		{
			GetCurrentAnimationController().Play();
		}

		private AnimationSequencerController GetCurrentAnimationController()
		{
			return isStatReward && statAnimationController != null ? statAnimationController : animationController;
		}

		private void OnAnimationFinished()
		{
			animationFinished = true;
		}

		private void OnButtonTap()
		{
			tapReceived = true;
		}

		public void SetChestSprites(Sprite closed, Sprite opened)
		{
			closedChest.sprite = closed;
			openedChest.sprite = opened;
		}

		public void PlayOpenSound()
		{
			audioPlayer.PlaySoundOnce("chest_open");
		}
	}
}
