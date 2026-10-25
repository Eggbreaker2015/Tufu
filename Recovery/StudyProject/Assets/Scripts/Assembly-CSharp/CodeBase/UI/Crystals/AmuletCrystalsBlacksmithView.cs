using System;
using System.Linq;
using System.Collections.Generic;
using Codebase.Analytics.Events;
using Codebase.Crystals.Commands;
using Codebase.Utilities;
using CodeBase.Audio;
using Codebase.Analytics;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Crystals;
using Codebase.Player;
using Codebase.Services.Cheats;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Crystals
{
	public class AmuletCrystalsBlacksmithView : PopupView
	{
		[SerializeField]
		private TMP_Text titleText;

		[SerializeField]
		private TMP_Text geodesCountText;

		[SerializeField]
		private Button openButton;

		[SerializeField]
		private TMP_Text openButtonText;

		[SerializeField]
		private Button closeButton;

		[SerializeField]
		private CanvasGroup canvasGroup;

		[SerializeField]
		private CanvasGroup openingBackgroundCanvasGroup;

		[SerializeField]
		private BlacksmithGeodeView[] geodeViews;

		[SerializeField]
		private float openStepDuration = .5f;

		[SerializeField]
		private GameObject resultGroup;

		[SerializeField]
		private AmuletCrystalItemView resultItemView;

		[SerializeField]
		private TMP_Text resultBaseStatsText;

		[SerializeField]
		private TMP_Text resultSpecialStatText;

		[SerializeField]
		private TMP_Text resultLegendaryAbilityText;

		[SerializeField]
		private TMP_Text resultSellPriceText;

		[SerializeField]
		private Button continueButton;

		[SerializeField]
		private GameObject multiResultGroup;

		[SerializeField]
		private Transform multiResultContainer;

		[SerializeField]
		private CrystalGeodeMiniView multiResultPrefab;

		[SerializeField]
		private Button multiContinueButton;

		[Inject]
		private CrystalModel crystalModel;

		[Inject]
		private IconStyleConfig iconStyleConfig;

		[Inject]
		private CrystalStyleConfig crystalStyleConfig;

		[Inject]
		private CheatsModel cheatsModel;

		[Inject]
		private IAudioPlayer audioPlayer;

		[Inject]
		private IAnalyticsWrapper analytics;

		private bool isOpening;

		private bool isBatchMode;

		private int visibleGeodeCount;

		private int activeOpeningStep = -1;

		private int lastFinishedOpeningStep = -1;

		private int pendingGeodeStepCompletions;

		private Sequence openingStepSequence;

		private CrystalItemModel resultCrystal;

		private CrystalItemModel pendingCrystal;

		private List<CrystalItemModel> pendingCrystals;

		private List<CrystalItemModel> resultCrystals;

		private readonly List<CrystalGeodeMiniView> spawnedMultiResultViews = new List<CrystalGeodeMiniView>();

		private List<CrystalItemModel> externalCrystals;

		private int externalIndex;

		private UniTaskCompletionSource externalCompletion;

		private const int MaxVisibleGeodes = 5;

		private const int OpeningStepsCount = 6;

		private const string GeodesCountFormat = "{0}<size=80%><color=#C7C7C7>/{1}";

		private const string GeodeHitSound = "geode_hit";

		private const string CrystalObtainSound = "crystal_enchant";

		private bool HasResult => resultCrystal != null || (resultCrystals != null && resultCrystals.Count > 0); // Native ELF0x167a650.

		protected override void Awake()
		{
            // Native ELF0x1679d50.
            base.Awake();
            openButton.onClick.AddListener(HandleOpenClick);
            closeButton.onClick.AddListener(HandleClose);
            continueButton.onClick.AddListener(HandleContinueClick);
            multiContinueButton.onClick.AddListener(HandleContinueClick);
        }

		private void HandleClose()
		{
            // Native ELF0x1679ed0.
            audioPlayer.PlaySoundOnce("tap"); Close();
        }

		public override void Initialize()
		{
            // Native ELF0x1679f9c.
            base.Initialize(); ResetOpeningVisuals(); HideResult();
        }

		public override void Cleanup()
		{
            // Native ELF0x167a064.
            base.Cleanup(); KillOpeningAnimations();
            isOpening = false; isBatchMode = false; visibleGeodeCount = 0;
            resultCrystal = null; activeOpeningStep = -1; lastFinishedOpeningStep = -1;
            pendingCrystal = null; pendingCrystals = null; resultCrystals = null;
            canvasGroup.interactable = true; ResetOpeningVisuals(); HideResult();
            if (externalCompletion != null) externalCompletion.TrySetResult();
            externalCompletion = null; externalCrystals = null; externalIndex = 0;
            closeButton.gameObject.SetActive(true);
        }

		public void BeginBatchOpen()
		{
            // Native ELF0x167a1e8.
            isBatchMode = true; HideResult(); ResetOpeningVisuals(); Render();
        }

		public UniTask BeginExternalReveal(List<CrystalItemModel> crystals)
		{
            // Native ELF0x167a218.
            isBatchMode = crystals.Count > 1; externalCrystals = crystals; externalIndex = 0;
            externalCompletion = new UniTaskCompletionSource();
            HideResult(); ResetOpeningVisuals(); Render(); return externalCompletion.Task;
        }

		protected override void Render()
		{
            // Native ELF0x167a2e4.
            titleText.text = LocalizationStrings.Get("CRYSTALS_BLACKSMITH_TITLE_LEVEL").Replace("{value}", GetTitleBlacksmithingLevel().ToString());
            geodesCountText.text = externalCrystals == null ? string.Format(GeodesCountFormat, crystalModel.GeodesCount, crystalModel.MaxGeodes) : Math.Max(0, externalCrystals.Count - externalIndex).ToString();
            bool result = HasResult;
            bool remaining = externalCrystals == null ? crystalModel.GeodesCount > 0.0 : externalIndex < externalCrystals.Count;
            openButton.gameObject.SetActive(!result);
            openButton.interactable = !result && (isOpening || remaining);
            closeButton.gameObject.SetActive(externalCrystals == null);
            UpdateOpenButtonText();
        }

		private void UpdateOpenButtonText()
		{
            // Native ELF0x167a6b4.
            if (openButtonText == null) return;
            openButtonText.gameObject.SetActive(true);
            if (!isBatchMode) { openButtonText.text = LocalizationStrings.Get("CRYSTALS_OPEN_GEODE"); return; }
            int count = isOpening && pendingCrystals != null ? pendingCrystals.Count : externalCrystals != null ? Math.Max(0, externalCrystals.Count - externalIndex) : NativeBlacksmithInt(crystalModel.GeodesCount);
            openButtonText.text = LocalizationStrings.Get("CRYSTALS_OPEN_GEODES").Replace("{value}", count.ToString());
        }

		private int GetTitleBlacksmithingLevel()
		{
            // Native ELF0x167a54c.
            if (resultCrystal != null) return resultCrystal.Level;
            if (resultCrystals != null && resultCrystals.Count > 0) return resultCrystals[0].Level;
            if (pendingCrystal != null) return pendingCrystal.Level;
            if (pendingCrystals != null && pendingCrystals.Count > 0) return pendingCrystals[0].Level;
            if (externalCrystals != null && externalCrystals.Count > 0) return externalCrystals[0].Level;
            return NativeBlacksmithInt(crystalModel.Blacksmithing.Value);
        }

		private void HandleOpenClick()
		{
            // Native ELF0x167a888.
            if (HasResult) return;
            if (isOpening) { AdvanceOpeningStep(); return; }
            if (!TryPrepareOpeningCrystals()) return;
            HideResult(); isOpening = true; activeOpeningStep = -1; lastFinishedOpeningStep = -1;
            ConfigureVisibleGeodes(); PlayOpeningStep(1, true); Render();
        }

		private bool TryPrepareOpeningCrystals()
		{
            // Native ELF0x167a964.
            if (!isBatchMode) {
                CrystalItemModel crystal;
                if (externalCrystals != null) {
                    if (externalIndex >= externalCrystals.Count) return false;
                    crystal = externalCrystals[externalIndex++];
                } else {
                    if (crystalModel.GeodesCount <= 0.0) return false;
                    crystal = App.ExecuteCommand<GenerateCrystalCommand, CrystalItemModel>(new GenerateCrystalCommand());
                    for (int i = 1; i < cheatsModel.DropMultiplier; i++)
                        crystal = App.ExecuteCommand<GenerateCrystalCommand, CrystalItemModel>(new GenerateCrystalCommand { SkipGeodeSpend = true });
                }
                if (crystal == null) return false;
                pendingCrystal = crystal; pendingCrystals = null; visibleGeodeCount = 1; return true;
            }
            if (externalCrystals != null) {
                if (externalIndex >= externalCrystals.Count) return false;
                pendingCrystals = new List<CrystalItemModel>(); pendingCrystal = null;
                visibleGeodeCount = Math.Min(5, externalCrystals.Count - externalIndex);
                for (int i = externalIndex; i < externalCrystals.Count; i++) pendingCrystals.Add(externalCrystals[i]);
                externalIndex = externalCrystals.Count;
                return pendingCrystals.Count > 0;
            }
            if (crystalModel.GeodesCount <= 0.0) return false;
            pendingCrystals = new List<CrystalItemModel>(); pendingCrystal = null;
            int count = NativeBlacksmithInt(crystalModel.GeodesCount); visibleGeodeCount = Math.Min(5, count);
            for (int i = 0; i < count; i++) {
                var crystal = App.ExecuteCommand<GenerateCrystalCommand, CrystalItemModel>(new GenerateCrystalCommand());
                if (crystal == null) continue;
                pendingCrystals.Add(crystal);
                for (int extra = 1; extra < cheatsModel.DropMultiplier; extra++)
                    App.ExecuteCommand<GenerateCrystalCommand, CrystalItemModel>(new GenerateCrystalCommand { SkipGeodeSpend = true });
            }
            return pendingCrystals.Count > 0;
        }

		private void ConfigureVisibleGeodes()
		{
            // Native ELF0x167ad90.
            for (int i = 0; i < geodeViews.Length; i++) {
                if (i < visibleGeodeCount) geodeViews[i].ResetVisuals(); else geodeViews[i].Hide();
            }
            UpdateGeodeIdleShakes();
        }

		private void UpdateGeodeIdleShakes()
		{
            // Native ELF0x167b40c.
            if (visibleGeodeCount <= 0) return;
            bool shake = isOpening && activeOpeningStep < 0 && lastFinishedOpeningStep == 4;
            for (int i = 0; i < geodeViews.Length && i < visibleGeodeCount; i++) {
                if (shake) geodeViews[i].PlayIdleShake(); else geodeViews[i].StopIdleShake();
            }
        }

		private void StopAllGeodeShakes()
		{
            // Native ELF0x167b4c8.
            foreach (var view in geodeViews) view.StopIdleShake();
        }

		private void AdvanceOpeningStep()
		{
            // Native ELF0x167a904.
            if (!isOpening) return;
            KillOpeningAnimations();
            if (activeOpeningStep >= 0) { ApplyOpeningStepEndState(activeOpeningStep); lastFinishedOpeningStep = activeOpeningStep; }
            int next = lastFinishedOpeningStep + 1;
            if (next < OpeningStepsCount) PlayOpeningStep(next); else FinishOpening();
        }

		private void PlayOpeningStep(int step, bool withBackgroundFade = false)
		{
            // Native ELF0x167ae20.
            StopAllGeodeShakes(); activeOpeningStep = step; pendingGeodeStepCompletions = 0;
            if (withBackgroundFade) {
                pendingGeodeStepCompletions++;
                openingStepSequence = DOTween.Sequence().SetTarget(this);
                openingStepSequence.Append(openingBackgroundCanvasGroup.DOFade(0f, openStepDuration).SetEase((Ease)6));
                openingStepSequence.OnComplete(HandleOpeningPartCompleted);
            }
            audioPlayer.PlaySoundOnce(GeodeHitSound);
            var active = GetActiveGeodeViews();
            if (step == 5) {
                foreach (var view in active) view.RevealInstant();
                if (pendingGeodeStepCompletions > 0 && --pendingGeodeStepCompletions != 0) return;
                HandleOpeningStepCompleted(); return;
            }
            pendingGeodeStepCompletions += active.Count;
            if (pendingGeodeStepCompletions == 0) { HandleOpeningStepCompleted(); return; }
            foreach (var view in active) view.PlayGeodeStep(step, HandleOpeningPartCompleted);
        }

		private void HandleOpeningPartCompleted()
		{
            // Native ELF0x167bb94.
            pendingGeodeStepCompletions--;
            if (pendingGeodeStepCompletions <= 0) HandleOpeningStepCompleted();
        }

		private void HandleOpeningStepCompleted()
		{
            // Native ELF0x167bbb0.
            if (!isOpening) return;
            lastFinishedOpeningStep = activeOpeningStep; activeOpeningStep = -1; openingStepSequence = null;
            if (lastFinishedOpeningStep < 5) UpdateGeodeIdleShakes(); else FinishOpening();
        }

		private void ApplyOpeningStepEndState(int step)
		{
            // Native ELF0x167b528.
            if (step > 0) openingBackgroundCanvasGroup.alpha = 0f;
            foreach (var view in GetActiveGeodeViews()) {
                if (step == 5) view.RevealInstant(); else view.CompleteGeodeStep(step);
            }
        }

		private bool IsInstantOpeningStep(int step)
		{
            // Native ELF0x167baa8.
            return step == 5;
        }

		private List<BlacksmithGeodeView> GetActiveGeodeViews()
		{
            // Native ELF0x167bab4.
            var active = new List<BlacksmithGeodeView>();
            for (int i = 0; i < geodeViews.Length && i < visibleGeodeCount; i++) active.Add(geodeViews[i]);
            return active;
        }

		private void FinishOpening()
		{
            // Native ELF0x167b734.
            KillOpeningAnimations(); isOpening = false; activeOpeningStep = -1; lastFinishedOpeningStep = -1;
            audioPlayer.PlaySoundOnce(CrystalObtainSound);
            foreach (var view in GetActiveGeodeViews()) view.Hide();
            if (!isBatchMode) { var crystal = pendingCrystal; pendingCrystal = null; ShowSingleResult(crystal); }
            else { ShowMultiResult(pendingCrystals); pendingCrystals = null; }
            isBatchMode = false; analytics.Send(new CrystalsGeodeOpenedEvent());
        }

		private void KillOpeningAnimations()
		{
            // Native ELF0x167a14c.
            if (openingStepSequence != null) openingStepSequence.Kill();
            openingStepSequence = null; pendingGeodeStepCompletions = 0;
            openingBackgroundCanvasGroup.DOKill(); foreach (var view in geodeViews) view.KillAnimations();
        }

		private int GetIdleGeodePreviewCount()
		{
            // Native ELF0x167c1a4.
            if (!isBatchMode) return geodeViews.Length == 0 ? 0 : 1;
            return externalCrystals == null ? Math.Min(5, NativeBlacksmithInt(crystalModel.GeodesCount)) : Math.Min(5, Math.Max(0, externalCrystals.Count - externalIndex));
        }

		private void ResetOpeningVisuals()
		{
            // Native ELF0x1679fc0.
            KillOpeningAnimations(); openingBackgroundCanvasGroup.alpha = 1f;
            if (!isOpening) visibleGeodeCount = GetIdleGeodePreviewCount();
            ConfigureVisibleGeodes();
        }

		private void ShowSingleResult(CrystalItemModel crystal)
		{
            // Native ELF0x167bfe8.
            if (crystal == null) return;
            resultCrystal = crystal; resultGroup.SetActive(true);
            Injector.InjectInto(resultItemView); resultItemView.Setup(crystal);
            var parts = CrystalStatTextFormatter.BuildCrystalStatsParts(crystalModel, iconStyleConfig, crystal, false, true);
            if (crystal.EnchantLevel == 0) parts.Special = CrystalStatTextFormatter.BuildEnchantToUnlockText(crystalStyleConfig, crystal.Rarity);
            CrystalStatTextFormatter.ApplyStatsParts(parts, resultBaseStatsText, resultSpecialStatText, resultLegendaryAbilityText);
            resultSellPriceText.text = CrystalStatTextFormatter.FormatSellPrice(crystalModel.CalculateSellPrice(crystal)); Render();
        }

		private void ShowMultiResult(List<CrystalItemModel> crystals)
		{
            // Native ELF0x167bc08.
            if (crystals == null || crystals.Count == 0) return;
            resultCrystals = crystals; multiResultGroup.SetActive(true); ClearMultiResultViews();
            foreach (var crystal in crystals.OrderByDescending(item => item.Rarity).ToList()) {
                var view = Instantiate(multiResultPrefab, multiResultContainer);
                Injector.InjectInto(view); view.Setup(crystal); spawnedMultiResultViews.Add(view);
            }
            Render();
        }

		private void ClearMultiResultViews()
		{
            // Native ELF0x167c25c.
            foreach (var view in spawnedMultiResultViews) if (view != null) Destroy(view.gameObject);
            spawnedMultiResultViews.Clear();
        }

		private void HideResult()
		{
            // Native ELF0x167a004.
            resultGroup.SetActive(false); multiResultGroup.SetActive(false);
            resultCrystal = null; resultCrystals = null; ClearMultiResultViews();
        }

		private bool HasRemainingGeodes()
		{
            // Native ELF0x167c4e8.
            return externalCrystals == null ? crystalModel.GeodesCount > 0.0 : externalIndex < externalCrystals.Count;
        }

		private void HandleContinueClick()
		{
            // Native ELF0x167c558.
            if (!HasResult) return;
            HideResult(); ResetOpeningVisuals(); if (HasRemainingGeodes()) Render(); else Close();
        }
        // Original ARM64 FCVTZS plus explicit positive-Infinity compatibility branch.
        private static int NativeBlacksmithInt(double value) => double.IsNaN(value) ? 0 : double.IsPositiveInfinity(value) ? int.MinValue : value >= int.MaxValue ? int.MaxValue : value <= int.MinValue ? int.MinValue : (int)value;
	}
}
