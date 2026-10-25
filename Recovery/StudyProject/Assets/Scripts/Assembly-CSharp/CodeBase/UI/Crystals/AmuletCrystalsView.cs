using Codebase.UI.Shop;
using Codebase.Crystals.Commands;
using Codebase.Analytics.Events;
using Codebase.Services.Time;
using Codebase.Utilities;
using UniMob;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AssetKits.ParticleImage;
using CodeBase.Audio;
using Codebase.Analytics;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Crystals;
using Codebase.Meta;
using Codebase.Player;
using Codebase.Player.Models;
using Codebase.Services.Cheats;
using Codebase.Unit;
using DG.Tweening;
using TMPro;
using UniMob.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Crystals
{
	public class AmuletCrystalsView : PopupView
	{
		[SerializeField]
		private List<AmuletCrystalSlotView> slots = new List<AmuletCrystalSlotView>();

		[SerializeField]
		private List<ParticleImage> slotParticles = new List<ParticleImage>();

		[SerializeField]
		private TMP_Text statsText;

		[SerializeField]
		private Transform itemsContainer;

		[SerializeField]
		private AmuletCrystalItemView itemPrefab;

		[SerializeField]
		private Button closeButton;

		[SerializeField]
		private Button infoButton;

		[SerializeField]
		private Button crystalUpgradesButton;

		[SerializeField]
		private GameObject crystalUpgradesNotification;

		[SerializeField]
		private Button sellAllButton;

		[SerializeField]
		private Button shopButton;

		[SerializeField]
		private Button filterButton;

		[SerializeField]
		private Sprite filterDefaultSprite;

		[SerializeField]
		private Sprite filterAppliedSprite;

		[SerializeField]
		private GameObject filtered;

		[SerializeField]
		private Button openButton;

		[SerializeField]
		private GameObject openNotification;

		[SerializeField]
		private Button openSingleGeodeButton;

		[SerializeField]
		private Button openAllGeodesButton;

		[SerializeField]
		private TMP_Text openAllGeodesCountText;

		[SerializeField]
		private TMP_Text geodesCountText;

		[SerializeField]
		private TMP_Text geodeResetTimerText;

		[SerializeField]
		private GameObject sellFlow;

		[SerializeField]
		private TMP_Text sellFlowDustText;

		[SerializeField]
		private TMP_Text sellFlowSummaryText;

		[SerializeField]
		private ParticleImage sellFlowParticle;

		[SerializeField]
		private Button sellFlowContinueButton;

		[SerializeField]
		private float sellFlowParticleDelay = .5f;

		[SerializeField]
		private float sellFlowParticleDuration = 1f;

		[SerializeField]
		private float sellFlowDustTweenDuration = 1f;

		[SerializeField]
		private float massSellSoundStartDelay = .4f;

		[SerializeField]
		private GameObject sellConfirmFlow;

		[SerializeField]
		private Transform sellConfirmItemsContainer;

		[SerializeField]
		private TMP_Text sellConfirmText;

		[SerializeField]
		private Button sellConfirmCloseButton;

		[SerializeField]
		private Button sellConfirmSellButton;

		[SerializeField]
		private ParticleImage crystalSellDustParticle;

		[SerializeField]
		private TMP_Text enchantStonesCountTxt;

		[SerializeField]
		private TMP_Text enchantStonesResetTimeTxt;

		[SerializeField]
		private RectTransform enchantStonesCurrency;

		[SerializeField]
		private GameObject enchantStonesNotification;

		[Inject]
		private CrystalModel crystalModel;

		[Inject]
		private IconStyleConfig iconStyleConfig;

		[Inject]
		private CrystalStyleConfig crystalStyleConfig;

		[Inject]
		private MetaStatsModel metaStatsModel;

		[Inject]
		private IAudioPlayer audioPlayer;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private CheatsModel cheatsModel;

		[Inject]
		private IAnalyticsWrapper analytics;

		private static int openInstances;

		private ViewContainer<AmuletCrystalItemView, CrystalItemModel> unequippedContainer;

		private ViewContainer<AmuletCrystalItemView, CrystalItemModel> sellConfirmContainer;

		private Sequence sellFlowSequence;

		private Sequence massSellSoundSequence;

		private Sequence enchantStonesBounceSequence;

		private bool isSellFlowPlaying;

		private Vector3 enchantStonesCurrencyInitialScale;

		private const int MaxMassSellSounds = 15;

		private const string SellSummaryLocKey = "CRYSTALS_SELL_SUMMARY";

		private const string SellConfirmTextLocKey = "CRYSTALS_SELL_CONFIRM_TEXT";

		private const float EnchantStonesBounceStartDelay = 1f;

		private const float EnchantStonesBounceInterval = 10f;

		private const float EnchantStonesBounceDuration = 0.8f;

		private const float EnchantStonesBouncePunchScale = 0.2f;

		private const int EnchantStonesBounceVibrato = 3;

		private const float EnchantStonesBounceElasticity = 0.35f;

		[CompilerGenerated]
		private HashSet<StatType> __BaseStatFilter__BackingField = new HashSet<StatType>(); // Native ctor13b0d3c.

		[CompilerGenerated]
		private HashSet<StatType> __SpecialStatFilter__BackingField = new HashSet<StatType>(); // Native ctor13b0d3c.

		[CompilerGenerated]
		private HashSet<LegendaryAbilityType> __LegendaryFilter__BackingField = new HashSet<LegendaryAbilityType>(); // Native ctor13b0d3c.

		private ComputedAtom<HashSet<StatType>> __BaseStatFilter;

		private ComputedAtom<HashSet<StatType>> __SpecialStatFilter;

		private ComputedAtom<HashSet<LegendaryAbilityType>> __LegendaryFilter;

		private ComputedAtom<List<CrystalItemModel>> __UnequippedCrystals;

		private ComputedAtom<List<CrystalItemModel>> __SellableCrystals;

		private ComputedAtom<bool> __HasSellableCrystals;

		private ComputedAtom<bool> __CanAffordAnyUpgrade;

		public static bool IsOpen => openInstances > 0; // Native ELF0x13ab324.

		public HashSet<StatType> BaseStatFilter
		{
            // Native ELF0x13ada38.
            get {
                UniMob.LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__BaseStatFilter == null) __BaseStatFilter = CodeGenAtom.CreatePooled(this, null, () => BaseStatFilter, false);
                if (!__BaseStatFilter.DirectEvaluate()) return __BaseStatFilter.Value;
                return __BaseStatFilter__BackingField;
            }
            private set {
                // Native ELF0x13ae158.
                UniMob.LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__BaseStatFilter != null && !__BaseStatFilter.CompareAndInvalidate(value)) return;
                __BaseStatFilter__BackingField = value;
            }
        }

		public HashSet<StatType> SpecialStatFilter
		{
            // Native ELF0x13adb60.
            get {
                UniMob.LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__SpecialStatFilter == null) __SpecialStatFilter = CodeGenAtom.CreatePooled(this, null, () => SpecialStatFilter, false);
                if (!__SpecialStatFilter.DirectEvaluate()) return __SpecialStatFilter.Value;
                return __SpecialStatFilter__BackingField;
            }
            private set {
                // Native ELF0x13ae1d8.
                UniMob.LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__SpecialStatFilter != null && !__SpecialStatFilter.CompareAndInvalidate(value)) return;
                __SpecialStatFilter__BackingField = value;
            }
        }

		public HashSet<LegendaryAbilityType> LegendaryFilter
		{
            // Native ELF0x13adc88.
            get {
                UniMob.LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__LegendaryFilter == null) __LegendaryFilter = CodeGenAtom.CreatePooled(this, null, () => LegendaryFilter, false);
                if (!__LegendaryFilter.DirectEvaluate()) return __LegendaryFilter.Value;
                return __LegendaryFilter__BackingField;
            }
            private set {
                // Native ELF0x13ae258.
                UniMob.LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__LegendaryFilter != null && !__LegendaryFilter.CompareAndInvalidate(value)) return;
                __LegendaryFilter__BackingField = value;
            }
        }

		private List<CrystalItemModel> UnequippedCrystals { get {
            // Native ELF0x13ae2d8.
            UniMob.LifetimeScopeExtension.ThrowIfDisposed(this);
            if (__UnequippedCrystals == null) __UnequippedCrystals = CodeGenAtom.CreatePooled(this, null, () => UnequippedCrystals, false);
            if (!__UnequippedCrystals.DirectEvaluate()) return __UnequippedCrystals.Value;
            return crystalModel.Crystals.Where(c => !c.IsEquipped).Where(PassesStatFilter).OrderByDescending(c => c.Rarity).ThenByDescending(c => c.EnchantLevel).ThenBy(c => c.Id).ToList();
        } }

		private List<CrystalItemModel> SellableCrystals { get {
            // Native ELF0x13af66c.
            UniMob.LifetimeScopeExtension.ThrowIfDisposed(this);
            if (__SellableCrystals == null) __SellableCrystals = CodeGenAtom.CreatePooled(this, null, () => SellableCrystals, false);
            if (!__SellableCrystals.DirectEvaluate()) return __SellableCrystals.Value;
            return crystalModel.Crystals.Where(c => !c.IsEquipped && !c.IsProtected).OrderByDescending(c => c.Rarity).ThenByDescending(c => c.EnchantLevel).ThenBy(c => c.Id).ToList();
        } }

		private bool HasSellableCrystals { get {
            // Native ELF0x13ad680.
            UniMob.LifetimeScopeExtension.ThrowIfDisposed(this);
            if (__HasSellableCrystals == null) __HasSellableCrystals = CodeGenAtom.CreatePooled(this, null, () => HasSellableCrystals, false);
            if (!__HasSellableCrystals.DirectEvaluate()) return __HasSellableCrystals.Value;
            return crystalModel.Crystals.Any(c => !c.IsEquipped && !c.IsProtected);
        } }

		private bool CanAffordAnyUpgrade { get {
            // Native ELF0x13ad890.
            UniMob.LifetimeScopeExtension.ThrowIfDisposed(this);
            if (__CanAffordAnyUpgrade == null) __CanAffordAnyUpgrade = CodeGenAtom.CreatePooled(this, null, () => CanAffordAnyUpgrade, false);
            if (!__CanAffordAnyUpgrade.DirectEvaluate()) return __CanAffordAnyUpgrade.Value;
            return crystalModel.Upgrades.Defs.Any(d => !crystalModel.Upgrades.IsMaxLevel(d.Key) && crystalModel.Upgrades.CanAfford(d.Key));
        } }

		protected override void Awake()
		{
            base.Awake();
            global::Codebase.Core.GameContext.Set(this);
            enchantStonesCurrencyInitialScale = enchantStonesCurrency.localScale;
            closeButton.onClick.AddListener(HandleClose);
            infoButton.onClick.AddListener(OpenTutorial);
            crystalUpgradesButton.onClick.AddListener(HandleCrystalUpgradesClick);
            openButton.onClick.AddListener(HandleOpenGeodeClick);
            openSingleGeodeButton.onClick.AddListener(HandleOpenGeodeClick);
            openAllGeodesButton.onClick.AddListener(HandleOpenAllGeodesClick);
            sellAllButton.onClick.AddListener(HandleSellAllClick);
            shopButton.onClick.AddListener(HandleShopClick);
            filterButton.onClick.AddListener(HandleFilterClick);
            sellFlowContinueButton.onClick.AddListener(HandleSellFlowContinueClick);
            sellConfirmCloseButton.onClick.AddListener(HideSellConfirm);
            sellConfirmSellButton.onClick.AddListener(HandleSellConfirmAcceptClick);
        }

		public override void Initialize()
		{
            // Native ELF0x13ab7d8.
            openInstances++; base.Initialize();
            foreach (var slot in slots) { Injector.InjectInto(slot); slot.Initialize(); }
            Atom.Reaction(Lifetime, () => { Ticker.TickEveryFrame(); geodeResetTimerText.text = BuildResetTimerText(); });
            if (unequippedContainer == null) unequippedContainer = new ViewContainer<AmuletCrystalItemView, CrystalItemModel>(itemsContainer, itemPrefab)
                .BindTo(Lifetime, () => UnequippedCrystals).Setup((view, item) => { view.SetInteractable(true); view.Setup(item); });
            if (sellConfirmContainer == null) sellConfirmContainer = new ViewContainer<AmuletCrystalItemView, CrystalItemModel>(sellConfirmItemsContainer, itemPrefab)
                .BindTo(Lifetime, () => SellableCrystals).Setup((view, item) => { view.SetInteractable(false); view.Setup(item); });
            TryShowTutorial(); analytics.Send(new CrystalsOpenedEvent());
        }

		public override void Cleanup()
		{
            // Native ELF0x13abef4.
            StopEnchantStonesBounce(); openInstances--; base.Cleanup();
        }

		private void HandleClose()
		{
            // Native ELF0x13abfa8.
            Close(); crystalModel.TryShowCrystalRespawnedPopup();
        }

		private void TryShowTutorial()
		{
            // Native ELF0x13abe74.
            if (!playerModel.Data.SeenCrystalsTutorial.Value) OpenTutorial();
        }

		private void OpenTutorial()
		{
            // Native ELF0x13abfd0.
            GameContext.Get<UINavigator>().Open<CrystalsTutorialView>("Assets/Prefabs/UI/Amulet/Crystals/CrystalsTutorial.prefab", true);
        }

		private void UpdateEnchantStonesTime()
		{
            // Native ELF0x13ac074.
            enchantStonesResetTimeTxt.text = TimeLocalizer.GetFormatTime((float)crystalModel.TimeToNextEnchantStoneReset.TotalSeconds, true);
        }

		protected override void Render()
		{
            // Native ELF0x13ac120.
            statsText.text = BuildStatsText();
            geodesCountText.text = string.Format("{0}<size=80%>/{1}", crystalModel.GeodesCount, crystalModel.MaxGeodes);
            sellAllButton.interactable = HasSellableCrystals;
            double openAll = metaStatsModel.Get("feature_unlock").GetValue("crystals_open_all"), count = crystalModel.GeodesCount;
            sellAllButton.gameObject.SetActive(metaStatsModel.Get("feature_unlock").GetValue("crystals_sell_all") > 0.0);
            openButton.gameObject.SetActive(count <= 0.0 || openAll <= 0.0); openButton.interactable = count > 0.0;
            bool multi = count > 0.0 && openAll > 0.0;
            openSingleGeodeButton.gameObject.SetActive(multi); openSingleGeodeButton.interactable = count > 0.0;
            openAllGeodesButton.gameObject.SetActive(count >= 2.0 && multi); openAllGeodesButton.interactable = count > 0.0;
            openAllGeodesCountText.text = string.Format("X{0}", count); openNotification.SetActive(count > 0.0);
            crystalUpgradesNotification.SetActive(CanAffordAnyUpgrade);
            filterButton.gameObject.SetActive(metaStatsModel.Get("feature_unlock").GetValue("crystals_filter") > 0.0);
            bool noFilter = BaseStatFilter.Count < 1 && SpecialStatFilter.Count < 1 && LegendaryFilter.Count < 1;
            filterButton.image.sprite = noFilter ? filterDefaultSprite : filterAppliedSprite;
            filtered.SetActive(!noFilter && metaStatsModel.Get("feature_unlock").GetValue("crystals_filter") > 0.0);
            UpdateSlotParticles();
            double stones = crystalModel.EnchantmentStonesCount;
            enchantStonesCountTxt.text = string.Format("{0}<size=80%>/{1}", stones, crystalModel.MaxEnchantmentStones);
            enchantStonesNotification.SetActive(stones > 0.0);
            if (stones <= 0.0) StopEnchantStonesBounce(); else StartEnchantStonesBounce();
            UpdateEnchantStonesTime();
        }

		private void StartEnchantStonesBounce()
		{
            // Native ELF0x13adfb0.
            if (enchantStonesBounceSequence != null && enchantStonesBounceSequence.IsActive()) return;
            enchantStonesCurrency.localScale = enchantStonesCurrencyInitialScale;
            enchantStonesBounceSequence = DOTween.Sequence().SetTarget(this);
            enchantStonesBounceSequence.AppendInterval(1f).Append(enchantStonesCurrency.DOPunchScale(Vector3.one * .2f, .8f, 3, .35f)).AppendInterval(8.2f).SetLoops(-1);
        }

		private void StopEnchantStonesBounce()
		{
            // Native ELF0x13abf58.
            if (enchantStonesBounceSequence != null) enchantStonesBounceSequence.Kill();
            enchantStonesBounceSequence = null; enchantStonesCurrency.localScale = enchantStonesCurrencyInitialScale;
        }

		private bool PassesStatFilter(CrystalItemModel crystal)
		{
            // Native ELF0x13ae794.
            if (BaseStatFilter.Count == 0 && SpecialStatFilter.Count == 0 && LegendaryFilter.Count == 0) return true;
            if (BaseStatFilter.Count > 0) {
                var stats = new HashSet<StatType> { crystal.BaseStatType };
                if (crystal.HasSecondaryStat) stats.Add(crystal.SecondaryStatType);
                if (!BaseStatFilter.All(stats.Contains)) return false;
            }
            if (SpecialStatFilter.Count > 0 && (!crystal.HasSpecialStat || !SpecialStatFilter.All(s => s == crystal.SpecialStatType))) return false;
            return LegendaryFilter.Count < 1 || (crystal.HasLegendaryAbility && LegendaryFilter.Contains(crystal.LegendaryAbility));
        }

		private void HandleFilterClick()
		{
            // Native ELF0x13aeb20.
            GameContext.Get<UINavigator>().Open<CrystalStatsFilterView>("Assets/Prefabs/UI/Amulet/Crystals/CrystalStatsFilterView.prefab", true).Setup(BaseStatFilter, SpecialStatFilter, LegendaryFilter, HandleFilterApplied);
        }

		private void HandleFilterApplied(HashSet<StatType> baseFilter, HashSet<StatType> specialFilter, HashSet<LegendaryAbilityType> legendaryFilter)
		{
            // Native ELF0x13af554.
            BaseStatFilter = baseFilter ?? new HashSet<StatType>(); SpecialStatFilter = specialFilter ?? new HashSet<StatType>(); LegendaryFilter = legendaryFilter ?? new HashSet<LegendaryAbilityType>();
        }

		private void HandleSellAllClick()
		{
            // Native ELF0x13afacc.
            if (isSellFlowPlaying) return;
            var sellable = SellableCrystals; if (sellable.Count < 1) return; ShowSellConfirm(sellable);
        }

		private void ShowSellConfirm(List<CrystalItemModel> sellable)
		{
            // Native ELF0x13afb38.
            double dust = sellable.Sum(c => crystalModel.CalculateSellPrice(c));
            sellConfirmText.text = LocalizationStrings.Get("CRYSTALS_SELL_CONFIRM_TEXT").Replace("{dust}", dust.Format()); sellConfirmFlow.SetActive(true);
        }

		private void HideSellConfirm()
		{
            // Native ELF0x13afcac.
            sellConfirmFlow.SetActive(false);
        }

		private void HandleSellConfirmAcceptClick()
		{
            // Native ELF0x13afcc8.
            if (isSellFlowPlaying) return;
            int count = crystalModel.Crystals.Count(c => !c.IsEquipped && !c.IsProtected);
            HideSellConfirm(); if (count < 1) return;
            double before = crystalModel.MagicDust; App.ExecuteCommand(new SellAllUnprotectedCrystalsCommand());
            PlaySellFlow(before, crystalModel.MagicDust, count);
        }

		private void PlaySellFlow(double dustBefore, double dustAfter, int soldCount)
		{
            // Native ELF0x13afe88.
            isSellFlowPlaying = true; sellFlow.SetActive(true); sellFlowDustText.text = dustBefore.Format();
            sellFlowSummaryText.text = LocalizationStrings.Get("CRYSTALS_SELL_SUMMARY").Replace("{count}", soldCount.ToString()).Replace("{dust}", (dustAfter - dustBefore).Format());
            sellFlowParticle.gameObject.SetActive(false); sellFlowContinueButton.gameObject.SetActive(false);
            if (sellFlowSequence != null) sellFlowSequence.Kill(); double displayed = dustBefore;
            sellFlowSequence = DOTween.Sequence().SetTarget(this);
            sellFlowSequence.AppendInterval(sellFlowParticleDelay).AppendCallback(() => {
                sellFlowParticle.gameObject.SetActive(true); sellFlowParticle.rateOverTime = soldCount; sellFlowParticle.Play(); StartMassSellSoundLoop(soldCount);
            }).AppendInterval(sellFlowParticleDuration).AppendCallback(() => sellFlowParticle.Stop())
                .Append(DOTween.To(() => displayed, value => { displayed = value; sellFlowDustText.text = value.Format(); }, dustAfter, sellFlowDustTweenDuration).SetEase(Ease.OutQuad))
                .OnComplete(() => { sellFlowSequence = null; sellFlowContinueButton.gameObject.SetActive(true); });
        }

		private void StartMassSellSoundLoop(int soldCount)
		{
            // Native ELF0x13b0300.
            if (massSellSoundSequence != null) massSellSoundSequence.Kill();
            int count = Math.Min(soldCount, 15); if (soldCount <= 0) return;
            float interval = sellFlowParticleDuration / (float)count;
            massSellSoundSequence = DOTween.Sequence().SetTarget(this); massSellSoundSequence.AppendInterval(massSellSoundStartDelay);
            for (int i = 0; i < count; i++) { massSellSoundSequence.AppendCallback(() => audioPlayer.PlaySoundOnce("mass_sell")); if (i < count - 1) massSellSoundSequence.AppendInterval(interval); }
        }

		private void StopMassSellSoundLoop()
		{
            // Native ELF0x13b0478.
            if (massSellSoundSequence != null) massSellSoundSequence.Kill(); massSellSoundSequence = null;
        }

		private void HandleSellFlowContinueClick()
		{
            // Native ELF0x13b04a8.
            if (sellFlowSequence != null && sellFlowSequence.IsActive()) sellFlowSequence.Complete(); HideSellFlow();
        }

		private void HideSellFlow()
		{
            // Native ELF0x13b04dc.
            StopMassSellSoundLoop(); sellFlow.SetActive(false); sellFlowContinueButton.gameObject.SetActive(false); isSellFlowPlaying = false;
        }

		private void OnDisable()
		{
            // Native ELF0x13b052c.
            StopEnchantStonesBounce(); if (sellFlowSequence != null && sellFlowSequence.IsActive()) sellFlowSequence.Kill(); sellFlowSequence = null;
            HideSellFlow(); HideSellConfirm();
        }

		public void PlayCrystalSellDustEffect(Vector3 worldPosition)
		{
            // Native ELF0x13b0580.
            crystalSellDustParticle.transform.position = worldPosition; crystalSellDustParticle.gameObject.SetActive(true); crystalSellDustParticle.Play();
        }

		private void HandleShopClick()
		{
            // Native ELF0x13b0604.
            GameContext.Get<UINavigator>().Open<ShopView>("Assets/Prefabs/UI/Shop.prefab").OpenAmuletTab();
        }

		private void HandleCrystalUpgradesClick()
		{
            // Native ELF0x13b06b4.
            GameContext.Get<UINavigator>().Open<AmuletCrystalUpgradesView>("Assets/Prefabs/UI/Amulet/Crystals/AmuletCrystalUpgradesView.prefab", true);
        }

		private void HandleOpenGeodeClick()
		{
            // Native ELF0x13b0758.
            if (crystalModel.GeodesCount <= 0.0) return;
            GameContext.Get<UINavigator>().Open<AmuletCrystalsBlacksmithView>("Assets/Prefabs/UI/Amulet/Crystals/AmuletCrystalsBlacksmithView.prefab", true);
        }

		private void HandleOpenAllGeodesClick()
		{
            // Native ELF0x13b0824.
            if (crystalModel.GeodesCount < 2.0) return;
            GameContext.Get<UINavigator>().Open<AmuletCrystalsBlacksmithView>("Assets/Prefabs/UI/Amulet/Crystals/AmuletCrystalsBlacksmithView.prefab", true).BeginBatchOpen();
        }

		private string BuildResetTimerText()
		{
            // Native ELF0x13b0900.
            float seconds = crystalModel.SecondsUntilNextGeode;
            if (seconds <= 0f) seconds = (float)crystalModel.GeodeTimerMinutes * 60f;
            return TimeLocalizer.GetFormatTime(seconds, true);
        }

		private void UpdateSlotParticles()
		{
            // Native ELF0x13addb0.
            for (int i = 0; i < slotParticles.Count; i++) {
                var particle = slotParticles[i]; if (particle == null) continue;
                var crystal = i < slots.Count && slots[i] != null ? slots[i].EquippedCrystal : null;
                particle.enabled = crystal != null;
                if (crystal != null) particle.startColor = new ParticleSystem.MinMaxGradient(crystalStyleConfig.GetRarityColor(crystal.Rarity));
            }
        }

		private string BuildStatsText()
		{
            // Native ELF0x13ac694.
            var equipped = crystalModel.Crystals.Where(c => c.IsEquipped).ToList(); if (equipped.Count == 0) return string.Empty;
            var legendaryOrder = new List<LegendaryAbilityType>(); var legendary = new Dictionary<LegendaryAbilityType, double>();
            var specialOrder = new List<StatType>(); var special = new Dictionary<StatType, double>();
            var baseOrder = new List<StatType>(); var bases = new Dictionary<StatType, double>();
            foreach (var c in equipped) {
                if (c.HasLegendaryAbility) {
                    var def = crystalModel.Config.GetLegendaryAbilityDef(c.LegendaryAbility);
                    if (def == null || !def.HideInGlobalStats) {
                        if (!legendary.ContainsKey(c.LegendaryAbility)) { legendaryOrder.Add(c.LegendaryAbility); legendary[c.LegendaryAbility] = 0.0; }
                        legendary[c.LegendaryAbility] += CrystalBalance.GetLegendaryAbilityValue(crystalModel.Config, c);
                    }
                }
                if (c.HasSpecialStat) {
                    float value = crystalModel.GetSpecialStatValue(c, c.SpecialStatType, c.EnchantLevel);
                    if (!special.ContainsKey(c.SpecialStatType)) { specialOrder.Add(c.SpecialStatType); special[c.SpecialStatType] = 0.0; }
                    special[c.SpecialStatType] += (double)value;
                }
                if (!bases.ContainsKey(c.BaseStatType)) { baseOrder.Add(c.BaseStatType); bases[c.BaseStatType] = 0.0; }
                bases[c.BaseStatType] += c.BaseStatValue;
                if (c.HasSecondaryStat) {
                    if (!bases.ContainsKey(c.SecondaryStatType)) { baseOrder.Add(c.SecondaryStatType); bases[c.SecondaryStatType] = 0.0; }
                    bases[c.SecondaryStatType] += c.SecondaryStatValue;
                }
            }
            var lines = new List<string>(legendaryOrder.Count + specialOrder.Count + baseOrder.Count);
            foreach (var type in legendaryOrder) { string line = CrystalStatTextFormatter.FormatLegendaryAbilityLine(crystalModel, type, legendary[type]); if (!string.IsNullOrEmpty(line)) lines.Add(CrystalStatTextFormatter.WrapColor(line, "#FFD500")); }
            foreach (var type in specialOrder) lines.Add(CrystalStatTextFormatter.WrapColor(CrystalStatTextFormatter.FormatStatLine(iconStyleConfig, type, special[type], true), "#FFD500"));
            foreach (var type in baseOrder) lines.Add(CrystalStatTextFormatter.WrapColor(CrystalStatTextFormatter.FormatStatLine(iconStyleConfig, type, bases[type], false), "white"));
            return string.Join("<br>", lines);
        }

		[StaticReset]
		private static void ResetStaticState()
		{
            // Native ELF0x13b0cf4.
            openInstances = 0;
        }
	}
}
