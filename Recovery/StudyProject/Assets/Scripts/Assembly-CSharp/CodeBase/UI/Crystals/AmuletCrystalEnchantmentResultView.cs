using Codebase.Crystals.Commands;
using Codebase.UI.Shop;
using Codebase.Utilities;
using UniMob;
using System.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BrunoMikoski.AnimationSequencer;
using CodeBase.Audio;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Crystals;
using Codebase.Player;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using DG.Tweening;
using TMPro;
using UniMob.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Crystals
{
	public class AmuletCrystalEnchantmentResultView : PopupView
	{




		[SerializeField]
		private Button closeButton;

		[SerializeField]
		private Button failCloseButton;

		[SerializeField]
		private Button tryAgainButton;

		[SerializeField]
		private Button guaranteeEnchantButton;

		[SerializeField]
		private GameObject guaranteeEnchantTip;

		[SerializeField]
		private TMP_Text guaranteeProgressText;

		[SerializeField]
		private GameObject successState;

		[SerializeField]
		private GameObject failState;

		[SerializeField]
		private GameObject cashbackState;

		[SerializeField]
		private TMP_Text cashbackText;

		[SerializeField]
		private GameObject crystalState;

		[SerializeField]
		private GameObject chargingState;

		[SerializeField]
		private Button chargeTapButton;

		[SerializeField]
		private AnimationSequencerController chargeAnimation;

		[SerializeField]
		private AnimationSequencerController successStatsAnimation;

		[SerializeField]
		private TMP_Text chargeProgressText;

		[SerializeField]
		private Transform chargeStepsContainer;

		[SerializeField]
		private CrystalEnchantStepView chargeStepPrefab;

		[SerializeField]
		private GameObject[] chargeStepEffects;

		[SerializeField]
		private AmuletCrystalItemView previousItemView;

		[SerializeField]
		private AmuletCrystalItemView itemView;

		[SerializeField]
		private TMP_Text baseStatsText;

		[SerializeField]
		private TMP_Text specialStatText;

		[SerializeField]
		private TMP_Text legendaryAbilityText;

		[SerializeField]
		private TMP_Text sellPriceText;

		[SerializeField]
		private TMP_Text transitionText;

		[SerializeField]
		private TMP_Text chargingSuccessChanceText;

		[SerializeField]
		private TMP_Text failButtonsSuccessChanceText;

		[SerializeField]
		private CrystalEnchantLifesView lifesView;

		[SerializeField]
		private Transform lifesContainer;

		[SerializeField]
		private CrystalEnchantLifeItemView lifeItemPrefab;

		[SerializeField]
		private float successResultDismissDelay = 2f; // Original ctor ELF1676f40.

		[SerializeField]
		private float extraHealthActivateSoundDelay = 0.7f; // Original ctor ELF1676f40.

		[SerializeField]
		private float crystalBouncePunchScale = 0.15f; // Original ctor ELF1676f40.

		[SerializeField]
		private float crystalBounceDuration = 0.35f; // Original ctor ELF1676f40.

		[SerializeField]
		private int crystalBounceVibrato = 5; // Original ctor ELF1676f40.

		[SerializeField]
		private float crystalBounceElasticity = 0.5f; // Original ctor ELF1676f40.

		[SerializeField]
		private float cashbackAnimationDelay = 2f; // Original ctor ELF1676f40.

		[SerializeField]
		private float cashbackAnimationDuration = 1f; // Original ctor ELF1676f40.

		[SerializeField]
		private float cashbackBouncePunchScale = 0.15f; // Original ctor ELF1676f40.

		[SerializeField]
		private float cashbackBounceDuration = 0.3f; // Original ctor ELF1676f40.

		[SerializeField]
		private int cashbackBounceVibrato = 5; // Original ctor ELF1676f40.

		[SerializeField]
		private float cashbackBounceElasticity = 0.5f; // Original ctor ELF1676f40.

		[Inject]
		private CrystalModel crystalModel;

		[Inject]
		private IconStyleConfig iconStyleConfig;

		[Inject]
		private CrystalStyleConfig crystalStyleConfig;

		[Inject]
		private UINavigator navigator;

		[Inject]
		private IAudioPlayer audioPlayer;

		private CrystalItemModel model;

		private bool success;

		private int prevEnchantLevel;

		private int newEnchantLevel;

		private bool isGeode;

		private int stepCount;

		private int extraAttemptsTotal;

		private IReadOnlyList<bool> rolls;

		private int rollIndex;

		private bool isChargePlaying;

		private bool resultPresented;

		private bool guaranteed;

		private float attemptSuccessChance;

		private double dustCashback;

		private Sequence cashbackSequence;

		private readonly List<CrystalEnchantStepView> chargeStepViews = new List<CrystalEnchantStepView>(); // Original ctor ELF1676f40.

		private readonly List<CrystalEnchantLifeItemView> lifeItemViews = new List<CrystalEnchantLifeItemView>(); // Original ctor ELF1676f40.

		[CompilerGenerated]
		private bool __IsCharging__BackingField;

		[CompilerGenerated]
		private int __ChargesDone__BackingField;

		[CompilerGenerated]
		private int __LifesRemaining__BackingField;

		[CompilerGenerated]
		private bool __CanDismissSuccessResult__BackingField;

		private ComputedAtom<bool> __IsCharging;

		private ComputedAtom<int> __ChargesDone;

		private ComputedAtom<int> __LifesRemaining;

		private ComputedAtom<bool> __CanDismissSuccessResult;

		private bool IsCharging
		{
            // Native ELF0x16743ec.
            get {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__IsCharging == null) __IsCharging = CodeGenAtom.CreatePooled(this, null, () => IsCharging, false);
                if (!__IsCharging.DirectEvaluate()) return __IsCharging.Value;
                return __IsCharging__BackingField;
            }
            set {
                // Native ELF0x167451c.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__IsCharging != null && !__IsCharging.CompareAndInvalidate(value)) return;
                __IsCharging__BackingField = value;
            }
        }

		private int ChargesDone
		{
            // Native ELF0x1674594.
            get {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__ChargesDone == null) __ChargesDone = CodeGenAtom.CreatePooled(this, null, () => ChargesDone, false);
                if (!__ChargesDone.DirectEvaluate()) return __ChargesDone.Value;
                return __ChargesDone__BackingField;
            }
            set {
                // Native ELF0x16746bc.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__ChargesDone != null && !__ChargesDone.CompareAndInvalidate(value)) return;
                __ChargesDone__BackingField = value;
            }
        }

		private int LifesRemaining
		{
            // Native ELF0x1674734.
            get {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__LifesRemaining == null) __LifesRemaining = CodeGenAtom.CreatePooled(this, null, () => LifesRemaining, false);
                if (!__LifesRemaining.DirectEvaluate()) return __LifesRemaining.Value;
                return __LifesRemaining__BackingField;
            }
            set {
                // Native ELF0x167485c.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__LifesRemaining != null && !__LifesRemaining.CompareAndInvalidate(value)) return;
                __LifesRemaining__BackingField = value;
            }
        }

		private bool CanDismissSuccessResult
		{
            // Native ELF0x16748d4.
            get {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__CanDismissSuccessResult == null) __CanDismissSuccessResult = CodeGenAtom.CreatePooled(this, null, () => CanDismissSuccessResult, false);
                if (!__CanDismissSuccessResult.DirectEvaluate()) return __CanDismissSuccessResult.Value;
                return __CanDismissSuccessResult__BackingField;
            }
            set {
                // Native ELF0x1674a04.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__CanDismissSuccessResult != null && !__CanDismissSuccessResult.CompareAndInvalidate(value)) return;
                __CanDismissSuccessResult__BackingField = value;
            }
        }

		protected override void Awake()
		{
            // Native ELF0x1674a7c.
            base.Awake(); closeButton.onClick.AddListener(Close); failCloseButton.onClick.AddListener(Close);
            tryAgainButton.onClick.AddListener(HandleTryAgain); guaranteeEnchantButton.onClick.AddListener(HandleGuaranteeEnchant);
            chargeTapButton.onClick.AddListener(HandleChargeTap); chargeAnimation.OnFinishedEvent.AddListener(HandleChargeFinished);
        }

		public void Setup(CrystalItemModel model, EnchantAttempt attempt, bool isGeode = false, bool guaranteed = false)
		{
            // Native ELF0x1674c9c.
            this.model = model; success = attempt.IsSuccess; prevEnchantLevel = attempt.PrevLevel; newEnchantLevel = attempt.NewLevel;
            stepCount = attempt.StepCount; extraAttemptsTotal = attempt.ExtraAttemptsTotal; rolls = attempt.Rolls;
            this.isGeode = isGeode; this.guaranteed = guaranteed; attemptSuccessChance = attempt.SuccessChance; dustCashback = attempt.DustCashback;
            Injector.InjectInto(previousItemView); Injector.InjectInto(itemView); previousItemView.SetupPreview(model, prevEnchantLevel); itemView.Setup(model);
            using (Atom.NoWatch) { isChargePlaying = false; resultPresented = false; ChargesDone = 0; rollIndex = 0; LifesRemaining = extraAttemptsTotal; IsCharging = !isGeode && stepCount > 0; CanDismissSuccessResult = true; }
            RebuildChargeSteps(); RebuildLifeItems(); ResetCashbackPresentation(); Initialize(); Render();
            if (guaranteed && IsCharging) HandleChargeTap();
        }

		public override void Cleanup()
		{
            // Native ELF0x1675394.
            base.Cleanup(); ClearChargeSteps(); ClearLifeItems(); cashbackSequence?.Kill(); cashbackSequence = null;
            cashbackText.transform.DOKill(); model = null;
        }

		protected override void Render()
		{
            // Native ELF0x167568c.
            if (model == null) return;
            bool charging = IsCharging; chargingState.SetActive(charging); chargeProgressText.text = string.Format("{0}<size=80%>/{1}", ChargesDone, stepCount); RenderChargeStepEffects();
            successState.SetActive(!charging && success); failState.SetActive(!charging && !success); cashbackState.SetActive(!charging && !success && dustCashback > 0.0); crystalState.SetActive(!isGeode);
            var parts = CrystalStatTextFormatter.BuildCrystalStatsParts(crystalModel, iconStyleConfig, model, false, true);
            if (model.EnchantLevel == 0) parts.Special = CrystalStatTextFormatter.BuildEnchantToUnlockText(crystalStyleConfig, model.Rarity);
            CrystalStatTextFormatter.ApplyStatsParts(parts, baseStatsText, specialStatText, legendaryAbilityText);
            sellPriceText.text = CrystalStatTextFormatter.FormatSellPrice(crystalModel.CalculateSellPrice(model)); transitionText.gameObject.SetActive(prevEnchantLevel > 0);
            if (prevEnchantLevel > 0) transitionText.text = BuildTransitionText(); RenderSuccessChanceTexts(); RenderExtraAttempts(); RenderGuaranteeEnchant(false);
            if (!charging && !resultPresented) { resultPresented = true; PresentResult(); }
            bool dismiss = charging || !success || CanDismissSuccessResult; closeButton.interactable = dismiss; chargeTapButton.interactable = charging; tryAgainButton.interactable = dismiss && !crystalModel.IsAtMaxEnchantLevel(model);
        }

		private void RebuildChargeSteps()
		{
            // Native ELF0x167502c.
            ClearChargeSteps(); for (int i = 0; i < stepCount; i++) { var view = Instantiate(chargeStepPrefab, chargeStepsContainer); Injector.InjectInto(view); view.SetCompleted(false); chargeStepViews.Add(view); }
        }

		private void ClearChargeSteps()
		{
            // Native ELF0x167540c.
            for (int i = 0; i < chargeStepViews.Count; i++) Destroy(chargeStepViews[i].gameObject); chargeStepViews.Clear();
        }

		private void CompleteChargeStep(int stepIndex, bool playAnimation)
		{
            // Native ELF0x167600c.
            if (stepIndex < 0 || stepIndex >= chargeStepViews.Count) return; var view = chargeStepViews[stepIndex]; view.SetCompleted(true); if (playAnimation) view.PlayCompletedAnimation();
        }

		private void RenderChargeStepEffects()
		{
            // Native ELF0x1675a20.
            for (int i = 0; i < chargeStepEffects.Length; i++) chargeStepEffects[i].SetActive(i < ChargesDone);
        }

		private void CompleteGuaranteedChargeSteps()
		{
            // Native ELF0x16760bc.
            for (int i = 0; i < stepCount; i++) CompleteChargeStep(i, i == stepCount - 1);
        }

		private void PlayCrystalBounce()
		{
            // Native ELF0x167610c.
            Transform target = itemView.transform; target.DOKill(); target.DOPunchScale(Vector3.one * crystalBouncePunchScale, crystalBounceDuration, crystalBounceVibrato, crystalBounceElasticity);
        }

		private void RenderSuccessChanceTexts()
		{
            // Native ELF0x1675d64.
            string text = CrystalStatTextFormatter.FormatSuccessChance(attemptSuccessChance); chargingSuccessChanceText.text = text; failButtonsSuccessChanceText.text = text;
        }

		private void RebuildLifeItems()
		{
            // Native ELF0x167515c.
            ClearLifeItems(); if (isGeode || extraAttemptsTotal < 1) return;
            for (int i = 0; i < extraAttemptsTotal; i++) { var view = Instantiate(lifeItemPrefab, lifesContainer); view.Setup(unchecked(prevEnchantLevel + 1)); lifeItemViews.Add(view); }
        }

		private void ClearLifeItems()
		{
            // Native ELF0x167554c.
            for (int i = 0; i < lifeItemViews.Count; i++) Destroy(lifeItemViews[i].gameObject); lifeItemViews.Clear();
        }

		private void RenderExtraAttempts()
		{
            // Native ELF0x1675dc8.
            lifesView.Setup(unchecked(prevEnchantLevel + 1), LifesRemaining, isGeode ? 0 : extraAttemptsTotal);
        }

		private void RenderGuaranteeEnchant(bool showOnFail)
		{
            // Native ELF0x1675e18.
            if (showOnFail && !isGeode && crystalModel.IsGuaranteeEnchantUnlocked) {
                bool ready = crystalModel.IsGuaranteeEnchantReady; guaranteeEnchantButton.gameObject.SetActive(ready && !crystalModel.IsAtMaxEnchantLevel(model)); guaranteeEnchantTip.SetActive(!ready);
                if (!ready) guaranteeProgressText.text = CrystalStatTextFormatter.FormatGuaranteeEnchantProgress(Math.Max(1, unchecked(crystalModel.GuaranteeEnchantThreshold - crystalModel.GuaranteeEnchantProgress)));
            } else { guaranteeEnchantButton.gameObject.SetActive(false); guaranteeEnchantTip.SetActive(false); }
        }

		private void PresentResult()
		{
            // Native ELF0x1675f68.
            if (isGeode) return;
            if (success) { PlayEnchantSuccessSound(); BeginSuccessResultDismissDelayAsync().Forget(); successStatsAnimation.ClearPlayingSequence(); successStatsAnimation.ResetToInitialState(); successStatsAnimation.Play(); }
            else { PlayEnchantFailedSound(); PlayCashbackAnimation(); }
        }

		private void ResetCashbackPresentation()
		{
            // Native ELF0x1675260.
            cashbackSequence?.Kill(); cashbackSequence = null; cashbackText.transform.DOKill(); cashbackText.transform.localScale = Vector3.one; RenderCashbackValue(0.0);
        }

		private void PlayCashbackAnimation()
		{
            // Native ELF0x1676348.
            if (dustCashback <= 0.0) return;
            double value = 0.0; cashbackSequence = DOTween.Sequence().SetTarget(this);
            float delay = cashbackAnimationDelay + -0.3f; cashbackSequence.AppendInterval(delay >= 0f ? delay : 0f);
            cashbackSequence.Append(DOTween.To(() => value, x => { value = x; RenderCashbackValue(value); }, dustCashback, cashbackAnimationDuration).SetEase(Ease.OutQuad));
            cashbackSequence.Append(cashbackText.transform.DOPunchScale(Vector3.one * cashbackBouncePunchScale, cashbackBounceDuration, cashbackBounceVibrato, cashbackBounceElasticity));
            cashbackSequence.OnComplete(() => cashbackSequence = null);
        }

		private void RenderCashbackValue(double value)
		{
            // Native ELF0x1676624.
            cashbackText.text = CrystalStatTextFormatter.FormatDustCashback(value);
        }

		private void HandleChargeTap()
		{
            // Native ELF0x1675320.
            if (!IsCharging || isChargePlaying) return; isChargePlaying = true; PlayEnchantChargingSound();
            chargeAnimation.ClearPlayingSequence(); chargeAnimation.ResetToInitialState(); chargeAnimation.Play();
        }

		private void HandleChargeFinished()
		{
            // Native ELF0x1676720.
            if (!IsCharging || !isChargePlaying) return; isChargePlaying = false;
            if (guaranteed) { ChargesDone = stepCount; PlayCrystalBounce(); CompleteGuaranteedChargeSteps(); }
            else if (rolls != null && rollIndex < rolls.Count) {
                bool passed = rolls[rollIndex]; rollIndex = unchecked(rollIndex + 1);
                if (passed) {
                    ChargesDone = unchecked(ChargesDone + 1); if (ChargesDone < stepCount) PlayEnchantStepSuccessSound(ChargesDone);
                    PlayCrystalBounce(); CompleteChargeStep(unchecked(ChargesDone - 1), true); if (ChargesDone < stepCount) return;
                } else if (LifesRemaining > 0) { LifesRemaining = unchecked(LifesRemaining - 1); PlayLifeLostAnimation(); return; }
            }
            IsCharging = false;
        }

		private void PlayLifeLostAnimation()
		{
            // Native ELF0x1676a4c.
            int index = unchecked(extraAttemptsTotal - LifesRemaining - 1); if (index >= 0 && index < lifeItemViews.Count) lifeItemViews[index].PlayConsumedAnimation(); PlayExtraHealthActivateSoundDelayed().Forget();
        }
		private async UniTaskVoid PlayExtraHealthActivateSoundDelayed()
		{
            // Native ELF0x167755c.
            await UniTask.WaitForSeconds(extraHealthActivateSoundDelay, cancellationToken: destroyCancellationToken); audioPlayer.PlaySoundOnce("enchant_extra_health_activate");
        }

		private void RevealFail()
		{
            // Native ELF0x1676b08.
            IsCharging = false;
        }

		public void PlayEnchantSuccessSound()
		{
            // Native ELF0x16761a4.
            audioPlayer.PlaySoundOnce("crystal_enchant");
        }

		private void PlayEnchantStepSuccessSound(int step)
		{
            // Native ELF0x1676950.
            audioPlayer.PlaySoundOnce(string.Format("enchant_success_step_{0}", step));
        }

		public void PlayEnchantChargingSound()
		{
            // Native ELF0x1676660.
            audioPlayer.PlaySoundOnce("enchant_charging");
        }

		public void PlayEnchantFailedSound()
		{
            // Native ELF0x1676288.
            audioPlayer.PlaySoundOnce("enchant_failed");
        }

		private void BeginSuccessResultDismissDelay()
		{
            // Native ELF0x1676264.
            BeginSuccessResultDismissDelayAsync().Forget();
        }
		private async UniTaskVoid BeginSuccessResultDismissDelayAsync()
		{
            // Native ELF0x16770a0.
            using (Atom.NoWatch) CanDismissSuccessResult = false;
            await UniTask.WaitForSeconds(successResultDismissDelay, cancellationToken: destroyCancellationToken); CanDismissSuccessResult = true;
        }

		public new void Close()
		{
            // Native ELF0x1676c30.
            if (success && !IsCharging && !CanDismissSuccessResult) return; base.Close();
        }

		private void HandleTryAgain()
		{
            // Native ELF0x1676c70.
            if (success && !IsCharging && !CanDismissSuccessResult) return; TryEnchant(false);
        }

		private void HandleGuaranteeEnchant()
		{
            // Native ELF0x1676e5c.
            if (!crystalModel.IsAtMaxEnchantLevel(model) && crystalModel.IsGuaranteeEnchantReady) TryEnchant(true);
        }

		private void TryEnchant(bool guaranteed)
		{
            // Native ELF0x1676cb0.
            if (crystalModel.IsAtMaxEnchantLevel(model)) return;
            if (crystalModel.EnchantmentStonesCount <= 0.0) { OpenBuyStonesPopup(); return; }
            CrystalItemModel crystal = model;
            EnchantAttempt attempt = App.ExecuteCommand<EnchantCrystalCommand, EnchantAttempt>(new EnchantCrystalCommand { Crystal = crystal, Guaranteed = guaranteed });
            if (!attempt.Performed) return; bool geode = isGeode; Close();
            navigator.Open<AmuletCrystalEnchantmentResultView>("Assets/Prefabs/UI/Amulet/Crystals/AmuletCrystalEnchantmentResult.prefab", true).Setup(crystal, attempt, geode, guaranteed);
        }

		private void OpenBuyStonesPopup()
		{
            // Native ELF0x1676eac.
            navigator.Open<BuyGemsPopupView>("Assets/Prefabs/UI/Rewarded/BuyGemsPopup.prefab").Setup("crystal.enchantment_stones", 1.0);
        }

		private string BuildTransitionText()
		{
            // Native ELF0x1675a94.
            string color = crystalStyleConfig.GetRarityColorCode(model.Rarity);
            Sprite previous = crystalStyleConfig.GetEnchantmentIcon(prevEnchantLevel), next = crystalStyleConfig.GetEnchantmentIcon(newEnchantLevel);
            var text = new StringBuilder(); if (previous != null) text.Append("<sprite name=\"").Append(previous.name).Append("\" color=").Append(color).Append('>');
            text.Append(" <sprite name=\"").Append("atlas_114").Append("\" color=").Append(color).Append("> ");
            if (next != null) text.Append("<sprite name=\"").Append(next.name).Append("\" color=").Append(color).Append('>'); return text.ToString();
        }
	}
}
