using Codebase.Utilities;
using Codebase.Unit;
using Codebase.Player;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BrunoMikoski.AnimationSequencer;
using CodeBase.Audio;
using Codebase.Abilities;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Pet;
using Codebase.Player.Inventory;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Inventory
{
	public class EvolveItemView : PopupView
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CWaitForAnimationOrSkip_003Ed__24 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder _003C_003Et__builder;

			public EvolveItemView _003C_003E4__this;

			public bool canSkip;

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
		private struct _003CWaitForTap_003Ed__25 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder _003C_003Et__builder;

			public EvolveItemView _003C_003E4__this;

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
		private GameObject root;

		[SerializeField]
		private Button tapButton;

		[SerializeField]
		private InventoryItemView itemView;

		[SerializeField]
		private TMP_Text itemName;

		[SerializeField]
		private TMP_Text evolveText;

		[SerializeField]
		private ItemAbilitiesView abilitiesView;

		[SerializeField]
		private RectTransform abilitiesRoot;

		[SerializeField]
		private TMP_Text damageName;

		[SerializeField]
		private TMP_Text damageText;

		[SerializeField]
		private TMP_Text levelText;

		[Inject]
		private ItemsProvider itemsProvider;

		[Inject]
		private IAudioPlayer audioPlayer;

		private ItemModel oldItem;

		private ItemModel newItem;

		private bool tapReceived;

		private bool animationFinished;

		// Pet mode: the pet's stats replace the damage row inside the stats panel.
		private TMP_Text petStatsText;

		protected override void Awake()
		{
			// Native ELF VA 0x164a8cc.
			base.Awake();
			animationController.OnFinishedEvent.AddListener(OnAnimationFinished);
			tapButton.onClick.AddListener(OnButtonTap);
			SetCloseCallback(() => { });
		}

		public void SetTargetItem(ItemModel model)
		{
			// Native ELF VA 0x164aa14.
			newItem = model;
		}

		public void SetSourceItem(ItemModel model)
		{
			// Native ELF VA 0x164aa1c.
			oldItem = model;
		}

		public void Setup(ItemModel item, bool isEvolved)
		{
			// Native ELF VA 0x164aa24; original isEvolved argument is unused.
			RenderItem(item, item.IsArmor ? item.Armor : item.Damage, null);
		}

		public void SetActive(bool active)
		{
			// Native ELF VA 0x164b0c8.
			root.SetActive(active);
			tapReceived = false;
			animationFinished = false;
			if (active) PlayAnimation();
		}

		public void ChangeItemToNew()
		{
			// Native ELF VA 0x164b160 is a tail branch to ShowDiff.
			ShowDiff();
		}

		public void ShowDiff()
		{
			// Native ELF VA 0x164b164.
			if (oldItem == null || newItem == null) return;
			RenderItem(newItem, oldItem.IsArmor ? oldItem.Armor : oldItem.Damage, ComputeDiffIndices(oldItem, newItem));
		}

		
		public async UniTask WaitForAnimationOrSkip(bool canSkip = true)
		{
			// Native MoveNext ELF VA 0x164b878; both waits use original Update timing and no token.
			tapReceived = false;
			animationFinished = false;
			if (canSkip)
			{
			    await UniTask.WaitUntil(() => animationFinished || tapReceived);
			    if (!animationFinished) animationController.Complete(true);
			}
			else await UniTask.WaitUntil(() => animationFinished);
			tapReceived = false;
		}

		
		public async UniTask WaitForTap()
		{
			// Native MoveNext ELF VA 0x164bf14.
			tapReceived = false;
			await UniTask.WaitUntil(() => tapReceived);
			tapReceived = false;
		}

		private void RenderItem(ItemModel item, double previousStat, List<int> abilityIndices)
		{
			// Native ELF VA 0x164aa84; exact original arrow markup and 122-pixel ability padding.
			itemView.Initialize(item, false);
			PetStatsText.SetItemName(itemName, item.Config);
			evolveText.text = LocalizationStrings.Get("ITEM_RARITY_" + item.Rarity);
			if (abilityIndices == null) abilitiesView.Setup(item, 2, false);
			else abilitiesView.SetupSpecific(item, abilityIndices, true);
			abilitiesRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, abilitiesView.GetHeight() + 122f);
			levelText.text = item.EffectiveLevel.ToString();
			damageName.text = LocalizationStrings.Get(item.IsArmor ? "STAT_Armor" : "STAT_Damage");
			IconStyleConfig styles = GameContext.Get<IconStyleConfig>();
			string sprite = item.IsArmor ? styles.Styles.First(style => style.StatType == item.Config.DamageStatTypeFlat).Sprite.name
			    : styles.WeaponTypeStyles.First(style => style.WeaponType == item.Config.DamageType).Sprite.name;
			double value = item.IsArmor ? item.Armor : item.Damage;
			damageText.text = abilityIndices != null && value != previousStat
			    ? "<sprite name=" + sprite + "><color=white>" + previousStat.Format() + "</color> <sprite name=atlas_59> <color=#7CFF7C>" + value.Format()
			    : "<sprite name=" + sprite + ">" + value.Format();
			RenderPetStats(item, abilityIndices != null ? oldItem : null);
		}

		// The diff (ShowDiff) is the pet of the item before evolving against the evolved one.
		private void RenderPetStats(ItemModel item, ItemModel previous)
		{
			GameObject damageRow = damageName.transform.parent.gameObject;
			if (!PetStats.TryGet(item, out PetStatsInfo pet))
			{
			    if (petStatsText != null && petStatsText.gameObject.activeSelf)
			    {
			        petStatsText.gameObject.SetActive(false);
			        damageRow.SetActive(true);
			    }
			    return;
			}
			if (petStatsText == null)
			{
			    petStatsText = PetStatsText.CreateBlock(damageName, damageRow.transform.parent, "PetStats");
			    petStatsText.fontSizeMax = 36f;
			    petStatsText.alignment = TextAlignmentOptions.MidlineLeft;
			    RectTransform block = petStatsText.rectTransform;
			    block.anchorMin = Vector2.zero;
			    block.anchorMax = Vector2.one;
			    block.pivot = new Vector2(0.5f, 0.5f);
			    block.offsetMin = new Vector2(damageName.rectTransform.anchoredPosition.x, 10f);
			    block.offsetMax = new Vector2(-20f, -10f);
			}
			PetStatsInfo from = previous != null && PetStats.TryGet(previous, out PetStatsInfo old) ? old : pet;
			PetStatsText.SetText(petStatsText, PetStatsText.Build(from, pet, false));
			petStatsText.gameObject.SetActive(true);
			damageRow.SetActive(false);
		}

		private static List<int> ComputeDiffIndices(ItemModel oldItem, ItemModel newItem)
		{
			// Native ELF VA 0x164b1f8: newly unlocked abilities take precedence over scaling changes.
			var unlocked = new List<int>();
			var scaling = new List<int>();
			for (int i = 0; i < newItem.Config.AbilityDefs.Count; i++)
			{
			    AbilityDef def = newItem.Config.AbilityDefs[i];
			    bool oldUnlocked = oldItem.IsAbilityUnlocked(def);
			    bool newUnlocked = newItem.IsAbilityUnlocked(def);
			    if (newUnlocked && !oldUnlocked) unlocked.Add(i);
			    else if (newUnlocked && oldUnlocked && def.LevelScaling.Count > 0 &&
			        HasNewScalingTier(def, oldItem.IsPreDivine ? 0 : oldItem.Level, newItem.IsPreDivine ? 0 : newItem.Level)) scaling.Add(i);
			}
			return unlocked.Count > 0 ? unlocked : scaling;
		}

		private static bool HasNewScalingTier(AbilityDef def, int oldStars, int newStars)
		{
			// Native ELF VA 0x164b6f4.
			return def.LevelScaling.Any(tier => oldStars < tier.Level && tier.Level <= newStars);
		}

		private void PlayAnimation()
		{
			// Native ELF VA 0x164b114; virtual slots 22 ResetToInitialState and 8 Play.
			animationController.ClearPlayingSequence();
			animationController.ResetToInitialState();
			animationController.Play();
		}

		private void OnAnimationFinished()
		{
			// Native ELF VA 0x164b7b8.
			animationFinished = true;
		}

		private void OnButtonTap()
		{
			// Native ELF VA 0x164b7c4.
			tapReceived = true;
		}
	}
}
