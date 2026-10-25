using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using CodeBase.Audio;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Crystals;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Codebase.UI.Crystals
{
	public class AmuletCrystalItemView : View, IPointerDownHandler, IEventSystemHandler, IPointerUpHandler
	{
		[CompilerGenerated]
		private sealed class _003CFillProgressBar_003Ed__30 : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			public AmuletCrystalItemView _003C_003E4__this;

			private float _003Celapsed_003E5__2;

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
			public _003CFillProgressBar_003Ed__30(int _003C_003E1__state)
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
		private Image crystalIcon;

		[SerializeField]
		private Image enchantmentIcon;

		[SerializeField]
		private Image rarityBackground;

		[SerializeField]
		private TMP_Text levelText;

		[SerializeField]
		private Button button;

		[SerializeField]
		private GameObject pin;

		[SerializeField]
		private Image fillBar;

		[SerializeField]
		private GameObject newNotification;

		[SerializeField]
		private bool interactable = true;

		[SerializeField]
		private bool isPreview;

		[Inject]
		private CrystalStyleConfig styleConfig;

		[Inject]
		private IAudioPlayer audioPlayer;

		private CrystalItemModel model;

		private int? enchantLevelOverride;

		private Coroutine fillCoroutine;

		private bool shouldBlockClick;

		private const float PinHoldDuration = 0.75f;

		private const float ClickBlockThreshold = 0.2f;

		public CrystalItemModel Model => this.model;

		private bool IsPreview => isPreview || enchantLevelOverride.HasValue; // Native ELF0x1677e50.

		protected override void Awake()
		{
            // Native ELF0x1677978.
            base.Awake(); button.onClick.AddListener(HandleClick);
        }

		public void Setup(CrystalItemModel model)
		{
            // Native ELF0x16736e4.
            this.model = model; enchantLevelOverride = null; Initialize(); Render();
        }

		public void SetInteractable(bool value)
		{
			this.interactable = value;
		}

		public void SetupPreview(CrystalItemModel model, int previewEnchantLevel)
		{
            // Native ELF0x1674f80.
            this.model = model; enchantLevelOverride = previewEnchantLevel; interactable = false; Initialize(); Render();
        }

		protected override void Render()
		{
            // Native ELF0x1677a1c.
            if (model == null) return;
            int level = enchantLevelOverride ?? model.EnchantLevel;
            var sprite = styleConfig.GetCrystalSprite(model.BaseStatType, level);
            crystalIcon.sprite = sprite; crystalIcon.enabled = sprite != null;
            var enchantment = styleConfig.GetEnchantmentIcon(level);
            bool showEnchantment = level > 0 && enchantment != null;
            enchantmentIcon.sprite = enchantment; enchantmentIcon.color = styleConfig.GetRarityColor(model.Rarity);
            enchantmentIcon.enabled = showEnchantment; enchantmentIcon.gameObject.SetActive(showEnchantment);
            var background = styleConfig.GetRarityBackground(model.Rarity);
            rarityBackground.sprite = background; rarityBackground.enabled = background != null;
            levelText.text = model.Level.ToString(); pin.SetActive(!IsPreview && model.IsProtected);
            fillBar.fillAmount = 0f;
            newNotification.SetActive(model.IsNew && interactable && !enchantLevelOverride.HasValue);
        }

		private void HandleClick()
		{
            // Native ELF0x1677ed8.
            if (!interactable || model == null) return;
            if (shouldBlockClick) { shouldBlockClick = false; return; }
            model.MarkSeen();
            GameContext.Get<UINavigator>().Open<AmuletCrystalContextView>("Assets/Prefabs/UI/Amulet/Crystals/AmuletCrystalContextView.prefab").Setup(model);
        }

		public void OnPointerDown(PointerEventData eventData)
		{
            // Native ELF0x1677fbc.
            if (!interactable || IsPreview || model == null || model.IsEquipped) return;
            shouldBlockClick = false;
            if (fillCoroutine != null) StopCoroutine(fillCoroutine);
            fillCoroutine = StartCoroutine(FillProgressBar());
        }

		public void OnPointerUp(PointerEventData eventData)
		{
            // Native ELF0x16780b0.
            if (fillCoroutine != null) { StopCoroutine(fillCoroutine); fillCoroutine = null; }
            fillBar.fillAmount = 0f;
        }
		private IEnumerator FillProgressBar()
		{
            // Native ELF0x1678044.
            // Original MoveNext0x167826c: increment, paint and yield before completion test.
            float elapsed = 0f;
            do {
                elapsed = elapsed + Time.deltaTime;
                fillBar.fillAmount = elapsed / PinHoldDuration;
                if (elapsed >= ClickBlockThreshold) shouldBlockClick = true;
                yield return null;
            } while (!(elapsed >= PinHoldDuration));
            fillBar.fillAmount = 0f; OnPinComplete();
        }

		private void OnPinComplete()
		{
            // Native ELF0x167812c.
            if (model == null || model.IsEquipped) return;
            bool wasProtected = model.IsProtected;
            model.SetProtected(!wasProtected); audioPlayer.PlaySoundOnce(wasProtected ? "unpin" : "pin");
        }
	}
}
