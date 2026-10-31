using Codebase.Abilities.Conditions;
using UniMob;
using CodeBase.Audio;
using Codebase.Core;
using System.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using AssetKits.ParticleImage;
using BrunoMikoski.AnimationSequencer;
using Codebase.Abilities;
using Codebase.Abilities.Core;
using Codebase.Abilities.Effects;
using Codebase.Core.UI;
using Codebase.Pet;
using Codebase.Player;
using Codebase.Player.Inventory;
using Codebase.Player.Models;
using Codebase.UI.Inventory;
using Codebase.Unit;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Codebase.UI
{
	public class ItemSlotView : View, IPointerDownHandler, IEventSystemHandler, IPointerUpHandler
	{
		[CompilerGenerated]
		private sealed class _003CFillProgressBar_003Ed__58 : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			public ItemSlotView _003C_003E4__this;

			private float _003Cduration_003E5__2;

			private float _003Celapsed_003E5__3;

			private float _003CblockThreshold_003E5__4;

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
			public _003CFillProgressBar_003Ed__58(int _003C_003E1__state)
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

		[SerializeField]
		private GameObject locker;

		[SerializeField]
		private GameObject background;

		[SerializeField]
		private TMP_Text levelField;

		[SerializeField]
		private TMP_Text rarityField;

		[SerializeField]
		private Image icon;

		[SerializeField]
		private Image rarityBg;

		[SerializeField]
		private Image typeAndRarity;

		[SerializeField]
		private Image selected;

		[SerializeField]
		private Button button;

		[SerializeField]
		private Image slotType;

		[SerializeField]
		private Image normalLocker;

		[SerializeField]
		private Image vipLocker;

		[SerializeField]
		private TMP_Text vipText;

		[SerializeField]
		private ParticleImage divineParticles;

		[SerializeField]
		private GameObject cooldown;

		[SerializeField]
		private Image cooldownFill;

		[SerializeField]
		private Image cooldownIcon;

		[SerializeField]
		private TMP_Text cooldownText;

		[SerializeField]
		private GameObject abilityDuration;

		[SerializeField]
		private Slider abilityDurationSlider;

		[SerializeField]
		private Image fillBar;

		[SerializeField]
		private AnimationSequencerController pinAnimation;

		[SerializeField]
		private GameObject pin;

		[SerializeField]
		private GameObject evolveNotification;

		// Native 0x15f7b88; original rodata ELF 0x921400.
		private static readonly Color StacksTextColor = new Color(1f, 0.8352941274642944f, 0f, 1f);

		private InventoryView inventory;

		private PlayerModel playerModel;

		private PetsModel petsModel;

		private IconStyleConfig iconStyleConfig;

		private Coroutine fillCoroutine;

		private bool shouldBlockClick;

		private Color levelDefaultColor;

		private bool abilityVisualsDirty;

		private bool pinVisible;

		private bool cooldownVisible;

		private bool abilityDurationVisible;

		private AbilityBuffIconsView buffIcons;

		private bool levelVisible;

		private bool typeAndRarityVisible;

		private bool vipTextVisible;

		private int displayedLevel;

		private int displayedCooldown;

		private ItemSlotModel model { get; set; }

		public void Initialize(ItemSlotModel model)
		{
			// Native ELF VA 0x15f5c5c, callback 0x15f7bd8.
			base.Initialize();
			inventory = GameContext.Get<InventoryView>();
			playerModel = GameContext.Get<PlayerModel>();
			this.model = model;
			if (GetComponentInParent<ItemsHotbarView>() != null) global::Codebase.Rpg.UI.RpgLineupCell.AddSlot(transform, model);
			levelDefaultColor = levelField.color;
			abilityVisualsDirty = true;
			iconStyleConfig = GameContext.Get<IconStyleConfig>();
			if (buffIcons == null) CreateBuffIcons();
			button.onClick.RemoveAllListeners();
			button.onClick.AddListener(() => {
			    if (!this.model.IsEquipped) return;
			    if (shouldBlockClick) { shouldBlockClick = false; return; }
			    GameContext.Get<IAudioPlayer>().PlaySoundOnce("light_tap");
			    if (GetComponentInParent<ItemsHotbarView>() != null && !inventory.IsOpened)
			    {
			        global::Codebase.Rpg.UI.RpgLineupCell.RequestSlot(this.model);
			        return;
			    }
			    using (Atom.NoWatch) inventory.SelectItem(this.model.ItemModel);
			});
			fillBar.fillAmount = 0f;
			if (model.IsPinned) pinAnimation.Play();
			Render();
		}

		private void Update()
		{
			// Native ELF VA 0x15f5e4c.
			bool visible = model.IsPinned && inventory.IsOpened;
			if (pinVisible != visible || abilityVisualsDirty) { pinVisible = visible; pin.SetActive(visible); }
			UpdateAbilityVisuals();
			if (GetComponentInParent<ItemsHotbarView>() != null)
			{
				typeAndRarity.enabled = false;
				selected.enabled = inventory.IsOpened && model.ItemModel != null && playerModel.SelectedItem == model.ItemModel;
				evolveNotification.SetActive(false);
				if (model.ItemModel != null) PetStatsText.SetText(levelField, "★" + global::Codebase.Rpg.RpgStars.FromCopies(model.ItemModel.Copies));
			}
		}

		private void UpdateAbilityVisuals()
		{
			// Native ELF VA 0x15f5eb8; first cooldown state in original order. Buffs are drawn by buffIcons.
			ItemModel item = model.ItemModel;
			// Pet mode: a dead pet's slot shows its revive countdown instead.
			if (TryRenderPetRevive(item)) { buffIcons.Clear(); return; }
			if (item == null || !item.HasAbility) { ApplyOverlayVisibility(false, false); buffIcons.Clear(); ApplyLevelAndRarityVisibility(item, false); return; }
			// Every timed or stacked buff gets its own icon above the slot (the single yellow bar showed only the first one).
			AbilityState cooling = null;
			bool anyTimed = false;
			buffIcons.Begin();
			foreach (AbilityState state in item.AbilityStates)
			{
			    bool timed = TryGetActiveDuration(state, out float c, out float m);
			    bool stacked = state.GetStackLimit() > 0 && state.Stacks > 0;
			    if (timed || stacked) buffIcons.Add(state.Def, timed ? (m > 0f ? c / m : 1f) : -1f, stacked ? state.Stacks : 0);
			    anyTimed |= timed;
			    if (cooling == null && state.Cooldown > 0f) cooling = state;
			}
			buffIcons.End();
			if (cooling != null)
			{
			    if (cooldownIcon.sprite != item.CurrentIcon) cooldownIcon.sprite = item.CurrentIcon;
			    float cd = GetMaxCooldown(cooling);
			    cooldownFill.fillAmount = cd > 0f ? cooling.Cooldown / cd : 0f;
			    // Original FRINTP/FCVTPS: ceiling, not truncation.
			    int value = (int)Math.Ceiling(cooling.Cooldown);
			    if (displayedCooldown != value || abilityVisualsDirty) { displayedCooldown = value; cooldownText.text = value.ToString(); }
			    ApplyOverlayVisibility(true, anyTimed);
			}
			else ApplyOverlayVisibility(false, anyTimed);
			ApplyLevelAndRarityVisibility(item, cooling != null);
		}

		private bool TryRenderPetRevive(ItemModel item)
		{
			if (item == null) return false;
			if (petsModel == null) petsModel = GameContext.Get<PetsModel>();
			var hero = GameContext.Get<global::Codebase.Rpg.HeroRosterModel>().Heroes.Find(value => value.Item == item);
			PetModel pet = hero != null ? hero.Pet : null;
			if (pet == null || pet.State != PetState.Dead) return false;
			float remaining = Mathf.Max(0f, hero.ReviveAt - Time.time);
			float total = PetConfig.Instance.ReviveCooldown > 0f ? PetConfig.Instance.ReviveCooldown : 15f;
			ApplyOverlayVisibility(true, false);
			if (cooldownIcon.sprite != item.CurrentIcon) cooldownIcon.sprite = item.CurrentIcon;
			cooldownFill.fillAmount = total > 0f ? Mathf.Clamp01(remaining / total) : 0f;
			int value = (int)Math.Ceiling(remaining);
			if (displayedCooldown != value || abilityVisualsDirty) { displayedCooldown = value; cooldownText.text = value.ToString(); }
			ApplyLevelAndRarityVisibility(item, true);
			return true;
		}

		private void ApplyOverlayVisibility(bool showCooldown, bool showAbilityDuration)
		{
			// Native ELF VA 0x15f6384.
			if (cooldownVisible != showCooldown || abilityVisualsDirty) { cooldownVisible = showCooldown; cooldown.SetActive(showCooldown); }
			if (abilityDurationVisible != showAbilityDuration || abilityVisualsDirty) { abilityDurationVisible = showAbilityDuration; abilityDuration.SetActive(showAbilityDuration); }
		}

		private void ApplyLevelAndRarityVisibility(ItemModel item, bool isOverlayActive)
		{
			// Native ELF VA 0x15f640c. Stacks used to replace the level number here; they now sit on the buff icons.
			bool type = item != null && !isOverlayActive && item.Rarity != Rarity.Mythical;
			bool vip = model.IsVip && !model.IsUnlocked && !isOverlayActive;
			if (item != null && (displayedLevel != item.DisplayLevel || abilityVisualsDirty)) { displayedLevel = item.DisplayLevel; levelField.text = displayedLevel.ToString(); }
			if (abilityVisualsDirty) levelField.color = levelDefaultColor;
			bool level = item != null && !isOverlayActive;
			if (levelVisible != level || abilityVisualsDirty) { levelVisible = level; levelField.enabled = level; }
			if (typeAndRarityVisible != type || abilityVisualsDirty) { typeAndRarityVisible = type; typeAndRarity.enabled = type; }
			if (vipTextVisible != vip || abilityVisualsDirty) { vipTextVisible = vip; vipText.enabled = vip; }
			abilityVisualsDirty = false;
		}

		private void CreateBuffIcons()
		{
			// The old duration bar only fits one buff: hide it and put the icon row where it was.
			Transform bar = abilityDuration.transform.Find("Bar");
			Vector2 position = new Vector2(0f, 95f);
			if (bar != null) { position = ((RectTransform)bar).anchoredPosition; bar.gameObject.SetActive(false); }
			buffIcons = AbilityBuffIconsView.Create((RectTransform)transform, position, levelField, StacksTextColor);
			// Draw under the pin marker and the hold-to-pin ring, which overlap the row's area.
			Transform pinRoot = pin.transform.parent;
			if (pinRoot != null && pinRoot.parent == transform) buffIcons.transform.SetSiblingIndex(pinRoot.GetSiblingIndex());
		}

		private float GetMaxCooldown(AbilityState state)
		{
			// Native ELF VA 0x15f6870.
			foreach (var condition in state.Def.Conditions) if (condition is CooldownCondition cd) return cd.Cooldown;
			return 1f;
		}

		private bool TryGetActiveDuration(AbilityState state, out float current, out float max)
		{
			// Native ELF VA 0x15f6654; greatest remaining duration determines its paired maximum.
			current = max = 0f;
			foreach (IAbilityEffect effect in state.Def.Effects)
			    if (TryGetEffectDuration(state, effect, out float c, out float m) && c > 0f && c > current) { current = c; max = m; }
			return current > 0f;
		}

		private bool TryGetEffectDuration(AbilityState state, IAbilityEffect effect, out float current, out float max)
		{
			// Native ELF VA 0x15f6a70.
			if (effect is WeaponStatStackEffect stack && stack.Duration > 0f) { current = state.Duration; max = stack.Duration; return true; }
			if (effect is TemporaryWeaponBuffEffect buff && buff.Duration > 0f && !buff.HideInUI) { current = GetTemporaryBuffDuration(state, buff.BuffType); max = buff.Duration; return true; }
			if (effect is AddStatEffect stat && stat.Duration > 0f) { current = GetStatModifierDuration(state, stat.StatType); max = stat.Duration; return true; }
			current = max = 0f; return false;
		}

		private float GetTemporaryBuffDuration(AbilityState state, WeaponBuffType buffType)
		{
			// Native ELF VA 0x15f6bf8.
			switch (buffType) {
			case WeaponBuffType.Damage: return state.TemporaryDamageDuration;
			case WeaponBuffType.AttackSpeed: return state.TemporaryAttackSpeedDuration;
			case WeaponBuffType.CritDamage: return state.TemporaryCritDuration;
			case WeaponBuffType.CritChance: return state.TemporaryCritChanceDuration;
			default: return 0f;
			}
		}

		private float GetStatModifierDuration(AbilityState state, StatType statType)
		{
			// Native ELF VA 0x15f6c60.
			if (playerModel?.Unit == null) return 0f;
			foreach (Stat stat in playerModel.Unit.Stats)
			    if (stat.Type == statType) {
			        foreach (StatModifier modifier in stat.Modifiers)
			            if (modifier.Name == state.Def.Key && modifier.Source == ModifierSource.AbilityTemporary) return modifier.Duration;
			        return 0f;
			    }
			return 0f;
		}

		protected override void Render()
		{
			// Native ELF VA 0x15f7088; predicates 0x15f7e20..0x15f7ecc.
			abilityVisualsDirty = true;
			ItemModel item = model.ItemModel;
			locker.SetActive(!model.IsUnlocked);
			slotType.sprite = iconStyleConfig.SlotTypeStyles.First(style => style.SlotType == model.SlotType).Sprite;
			slotType.SetNativeSize(); slotType.enabled = item == null;
			bool vip = model.IsVip && !model.IsUnlocked;
			normalLocker.enabled = !model.IsVip && !model.IsUnlocked;
			vipLocker.enabled = vip; vipText.text = vip ? "VIP 4" : string.Empty; vipText.enabled = vip;
			if (item == null) {
			    levelField.text = rarityField.text = string.Empty;
			    icon.enabled = rarityBg.enabled = typeAndRarity.enabled = selected.enabled = divineParticles.enabled = false;
			    evolveNotification.SetActive(false); return;
			}
			selected.enabled = GameContext.Get<PlayerModel>().SelectedItem == item;
			Rarity rarity = item.Rarity;
			divineParticles.enabled = item.IsDivine;
			levelField.text = string.Format("{0}", item.DisplayLevel); rarityField.text = rarity.ToString();
			icon.sprite = item.CurrentIcon; icon.enabled = true;
			var rarityStyle = iconStyleConfig.ItemRarityStyles.First(style => style.Rarity == item.Config.Rarity);
			rarityBg.sprite = item.IsDivine ? rarityStyle.EvolvedSprite : rarityStyle.Sprite;
			typeAndRarity.enabled = rarity != Rarity.Mythical;
			if (typeAndRarity.enabled) {
			    typeAndRarity.sprite = (item.IsArmor ? iconStyleConfig.ArmorTypeMinimizedStyles : iconStyleConfig.WeaponTypeMinimizedStyles)
			        .First(style => style.WeaponType == item.Config.DamageType).Sprite;
			    typeAndRarity.color = GameContext.Get<ItemsProvider>().RarityColors[(int)rarity];
			}
			rarityBg.enabled = true; evolveNotification.SetActive(item.CanEvolve);
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			// Native ELF VA 0x15f782c.
			if (!model.IsEquipped || !inventory.IsOpened) return;
			shouldBlockClick = false;
			if (fillCoroutine != null) StopCoroutine(fillCoroutine);
			fillCoroutine = StartCoroutine(FillProgressBar());
		}

		public void OnPointerUp(PointerEventData eventData)
		{
			// Native ELF VA 0x15f7918.
			if (fillCoroutine != null) { StopCoroutine(fillCoroutine); fillCoroutine = null; }
			fillBar.fillAmount = 0f;
		}

		private IEnumerator FillProgressBar()
		{
			// Native iterator ELF VA 0x15f7f10, rodata 0x8df618 duration=.75f, block=.2f.
			float duration = .75f, elapsed = 0f, blockThreshold = .2f;
			while (elapsed < duration) {
			    elapsed += Time.deltaTime; fillBar.fillAmount = elapsed / duration;
			    if (elapsed >= blockThreshold) shouldBlockClick = true;
			    yield return null;
			}
			fillBar.fillAmount = 1f; OnPinComplete();
		}

		private void OnPinComplete()
		{
			// Native ELF VA 0x15f7988; original pin/unpin audio keys.
			fillBar.fillAmount = 0f;
			if (!model.IsPinned) { model.IsPinned = true; pinAnimation.Play(); pin.SetActive(true); GameContext.Get<IAudioPlayer>().PlaySoundOnce("pin"); }
			else { model.IsPinned = false; pin.SetActive(false); GameContext.Get<IAudioPlayer>().PlaySoundOnce("unpin"); }
		}
	}
}

