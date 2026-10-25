using UnityEngine.UI;
using Codebase.Utilities;
using UniMob;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Codebase.ABTests;
using Codebase.Amulet;
using Codebase.Analytics;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Crystals;
using Codebase.Economy;
using Codebase.Player.Models;
using Codebase.Shop;
using Codebase.UI.VIP;
using UniMob.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace Codebase.UI.Shop
{
	public class ShopView : PopupView
	{
		private static readonly ShopTabDef[] Tabs = {
            // Native cctor 0x162d884: display order is intentionally different from enum order.
            new ShopTabDef(ShopTab.Stats, "SHOP_TAB_STATS"), new ShopTabDef(ShopTab.Chests, "SHOP_TAB_CHESTS"),
            new ShopTabDef(ShopTab.SoulGraph, "SHOP_TAB_SOUL_GRAPH"), new ShopTabDef(ShopTab.Amulet, "SHOP_TAB_CRYSTALS")
        };

		[Header("Tabs")]
		[SerializeField]
		private Transform tabsRoot;

		[SerializeField]
		private ShopTabButtonView tabButtonPrefab;

		[Header("Prefabs")]
		[SerializeField]
		private ShopContainerView containerPrefab;

		[SerializeField]
		private ShopItemView itemPrefab;

		[Header("Layout")]
		[SerializeField]
		private Transform containersRoot;

		[SerializeField]
		private RectTransform contentRoot;

		[Header("No Ads")]
		[SerializeField]
		private GameObject noAdsSection;

		[Header("Amulet Offers")]
		[FormerlySerializedAs("soulOfferSection")]
		[SerializeField]
		private GameObject soulPassSection;

		[FormerlySerializedAs("crystalOfferSection")]
		[SerializeField]
		private GameObject crystalPassSection;

		[FormerlySerializedAs("soulOfferBanner")]
		[SerializeField]
		private ShopOfferBannerView soulPassBanner;

		[FormerlySerializedAs("crystalOfferBanner")]
		[SerializeField]
		private ShopOfferBannerView crystalPassBanner;

		[Header("Chaotic Soul Offer")]
		[SerializeField]
		private GameObject chaoticSoulOfferSection;

		[SerializeField]
		private ShopOfferBannerView chaoticSoulOfferBanner;

		[Header("Dependencies")]
		[SerializeField]
		private ShopTimerView timerView;

		[SerializeField]
		private VipCurrencyView vipCurrencyView;

		private readonly List<ShopContainerView> spawnedContainers = new List<ShopContainerView>(); // Native ctor 0x162d7a0.

		[Inject]
		private ShopLotRepository shopLotRepository;

		[Inject]
		private UserModel userModel;

		[Inject]
		private ABTestService abTestService;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private AmuletModel amuletModel;

		[Inject]
		private CrystalModel crystalModel;

		[Inject]
		private CrystalConfig crystalConfig;

		[Inject]
		private AmuletConfig amuletConfig;

		[Inject]
		private SoulStyleConfig soulStyleConfig;

		[Inject]
		private CurrencyStyleConfig currencyStyleConfig;

		[Inject]
		private ShopSectionsConfig sectionsConfig;

		[Inject]
		private AmuletShopSectionsConfig amuletSectionsConfig;

		[Inject]
		private ChestShopSectionsConfig chestSectionsConfig;

		[Inject]
		private SoulGraphShopSectionsConfig soulGraphSectionsConfig;

		private ShopTab lastBuiltTab = (ShopTab)(-1);

		private readonly List<string> lastBuiltLotKeys = new List<string>();

		private bool scrollToChaoticSoulOfferPending;

		private ViewContainer<ShopTabButtonView, ShopTabDef> tabsContainer;

		[CompilerGenerated]
		private ShopTab __ActiveTab__BackingField;

		private ComputedAtom<ShopTab> __ActiveTab;

		private ComputedAtom<ShopTab> __EffectiveTab;

		private ShopTab ActiveTab
		{
            get {
                // Native 0x162a180.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__ActiveTab == null) __ActiveTab = CodeGenAtom.CreatePooled(this, null, () => ActiveTab, false);
                return __ActiveTab.DirectEvaluate() ? __ActiveTab__BackingField : __ActiveTab.Value;
            }
            set {
                // Native 0x162a29c.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__ActiveTab != null && !__ActiveTab.CompareAndInvalidate(value)) return;
                __ActiveTab__BackingField = value;
            }
		}

		private ShopTab EffectiveTab {
            get {
                // Native 0x162a314.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__EffectiveTab == null) __EffectiveTab = CodeGenAtom.CreatePooled(this, null, () => EffectiveTab, false);
                return __EffectiveTab.DirectEvaluate() ? (IsTabLocked(ActiveTab) ? ShopTab.Stats : ActiveTab) : __EffectiveTab.Value;
            }
        }

		protected override void Awake()
		{
            // Native 0x162a4ac and weekly provider callback 0x162da4c.
            base.Awake();
            Injector.InjectInto(this);
            foreach (var view in GetComponentsInChildren<View>(true)) Injector.InjectInto(view);
            soulPassBanner.SetOfferType(ShopOfferType.SoulPass);
            crystalPassBanner.SetOfferType(ShopOfferType.CrystalPass);
            chaoticSoulOfferBanner.SetOfferType(ShopOfferType.ChaoticSoul);
            soulPassBanner.SetPurchasedFlag(userModel.SoulPassPurchased);
            crystalPassBanner.SetPurchasedFlag(userModel.CrystalPassPurchased);
            soulPassBanner.SetDuration(userModel.SoulPassPurchaseTime, amuletConfig.PassDurationDays);
            crystalPassBanner.SetDuration(userModel.CrystalPassPurchaseTime, crystalConfig.PassDurationDays);
            soulPassBanner.SetBonuses(BuildSoulPassBonuses());
            crystalPassBanner.SetBonuses(BuildCrystalPassBonuses());
            chaoticSoulOfferBanner.SetPurchasedFlag(userModel.ChaoticSoulOfferPurchased);
            chaoticSoulOfferBanner.SetWeeklyReset(() => userModel.ChaoticSoulOfferNextResetTime);
            chaoticSoulOfferBanner.SetBonuses(BuildChaoticSoulOfferBonuses());
		}

		protected override void Start()
		{
            // Native 0x162b154, original setup/cleanup closures 0x162da64/162dba0.
            base.Start();
            tabsContainer = new ViewContainer<ShopTabButtonView, ShopTabDef>(tabsRoot, tabButtonPrefab)
                .BindTo(Lifetime, () => Tabs)
                .WithEqualityCheck((a, b) => a.Tab == b.Tab)
                .Setup((view, tab) => {
                    view.Setup(tab);
                    view.SetSelected(ActiveTab == tab.Tab);
                    view.SetLocked(IsTabLocked(tab.Tab));
                    view.SetNotification(HasFreeClaim(tab.Tab));
                    view.OnClicked += HandleTabClicked;
                })
                .OnCleanup(view => view.OnClicked -= HandleTabClicked);
		}

		public void OpenAmuletTab()
		{
            // Native 0x162b488; original legacy Amulet tab is the Crystals tab.
            if (crystalModel.IsUnlocked) ActiveTab = ShopTab.Amulet;
		}

		public void OpenChestsTab()
		{
            // Native 0x162b4c0.
            ActiveTab = ShopTab.Chests;
		}

		public void OpenSoulGraphTab()
		{
            // Native 0x162b4c8.
            if (amuletModel.IsUnlocked) ActiveTab = ShopTab.SoulGraph;
		}

		public void OpenChaoticSoulOffer()
		{
            // Native 0x162b500.
            scrollToChaoticSoulOfferPending = true;
            ShopTab previous = EffectiveTab;
            OpenSoulGraphTab();
            if (previous == ShopTab.SoulGraph) TryScrollToChaoticSoulOffer();
		}

		protected override void Render()
		{
            // Native 0x162b574 and original pass-banner predicates 0x162dd00/162dd68/162ddd0.
            UpdateTabButtons();
            var sections = GetActiveSections();
            bool soul = abTestService.IsSoulPassEnabled() && sections.Any(section => section.PassBanners.Contains(ShopPassBannerType.SoulPass));
            bool crystal = abTestService.IsCrystalPassEnabled() && sections.Any(section => section.PassBanners.Contains(ShopPassBannerType.CrystalPass));
            bool chaotic = sections.Any(section => section.PassBanners.Contains(ShopPassBannerType.ChaoticSoulOffer));
            soulPassSection.SetActive(soul); crystalPassSection.SetActive(crystal); chaoticSoulOfferSection.SetActive(chaotic);
            Rebuild(); UpdateNoAdsSection(); TryScrollToChaoticSoulOffer();
		}

		private void UpdateNoAdsSection()
		{
            // Native 0x162cac0.
            bool visible = EffectiveTab == ShopTab.Stats && abTestService.IsNoAdsEnabled() && userModel.RewardedDelayFromStartUnlocked.Value;
            noAdsSection.SetActive(visible);
            if (!visible) return;
            if (userModel.NoAdsPurchased.Value) {
                if (noAdsSection.transform.GetSiblingIndex() != noAdsSection.transform.parent.childCount - 1) noAdsSection.transform.SetAsLastSibling();
            } else if (noAdsSection.transform.GetSiblingIndex() != 0) noAdsSection.transform.SetAsFirstSibling();
		}

		private void OnEnable()
		{
            // Native 0x162cc50.
            Initialize(); timerView.Initialize();
		}

		private void HandleTabClicked(ShopTab tab)
		{
            // Native 0x162cc80.
            if (!IsTabLocked(tab)) ActiveTab = tab;
		}

		private void UpdateTabButtons()
		{
            // Native 0x162b85c; null guard is original for the pre-Start render boundary.
            if (tabsContainer == null) return;
            foreach (var view in tabsContainer.Active) {
                view.SetLocked(IsTabLocked(view.Tab));
                view.SetSelected(view.Tab == EffectiveTab);
                view.SetNotification(HasFreeClaim(view.Tab));
            }
		}

		private bool IsTabLocked(ShopTab tab)
		{
            // Native 0x162a458.
            if (tab == ShopTab.SoulGraph) return !amuletModel.IsUnlocked;
            if (tab == ShopTab.Amulet) return !crystalModel.IsUnlocked;
            return false;
		}

		private bool HasFreeClaim(ShopTab tab)
		{
            // Native 0x162ccb8.
            switch (tab) {
                case ShopTab.Stats: return HasFreeClaimInSections(sectionsConfig.Sections, ShopLotType.Stat);
                case ShopTab.Amulet: return HasFreeClaimInSections(amuletSectionsConfig.Sections, ShopLotType.Currency);
                case ShopTab.Chests: return HasFreeClaimInSections(chestSectionsConfig.Sections, ShopLotType.Chest);
                case ShopTab.SoulGraph: return HasFreeClaimInSections(soulGraphSectionsConfig.Sections, ShopLotType.Soul);
                default: return false;
            }
		}

		private bool HasFreeClaimInSections(List<ShopSectionDef> sections, ShopLotType lotType)
		{
            // Native 0x162cd38.
            foreach (var section in sections) foreach (var def in section.Lots) {
                if (def.LotType != lotType) continue;
                var model = shopLotRepository.Get(def.Key);
                if (model != null && model.CanClaim && model.TotalValue > 0.0) return true;
            }
            return false;
		}

		private List<ShopSectionDef> GetActiveSections()
		{
            // Native 0x162bb8c.
            switch (EffectiveTab) {
                case ShopTab.Amulet: return amuletSectionsConfig.Sections;
                case ShopTab.Chests: return chestSectionsConfig.Sections;
                case ShopTab.SoulGraph: return soulGraphSectionsConfig.Sections;
                default: return sectionsConfig.Sections;
            }
		}

		private void Rebuild()
		{
            // Native 0x162bbf4: rebuild only when the effective tab or ordered visible keys change.
            var sections = GetActiveSections();
            var keys = GetVisibleLotKeys(sections);
            if (lastBuiltTab == EffectiveTab && lastBuiltLotKeys.SequenceEqual(keys)) return;
            lastBuiltTab = EffectiveTab; lastBuiltLotKeys.Clear(); lastBuiltLotKeys.AddRange(keys);
            foreach (var container in spawnedContainers) if (container != null) Destroy(container.gameObject);
            spawnedContainers.Clear();
            soulPassSection.transform.SetParent(containersRoot, false);
            crystalPassSection.transform.SetParent(containersRoot, false);
            chaoticSoulOfferSection.transform.SetParent(containersRoot, false);
            foreach (var section in sections) {
                foreach (var banner in section.PassBanners) {
                    if (banner == ShopPassBannerType.SoulPass) soulPassSection.transform.SetAsLastSibling();
                    else if (banner == ShopPassBannerType.CrystalPass) crystalPassSection.transform.SetAsLastSibling();
                    else if (banner == ShopPassBannerType.ChaoticSoulOffer) chaoticSoulOfferSection.transform.SetAsLastSibling();
                }
                var container = Instantiate(containerPrefab, containersRoot);
                Injector.InjectInto(container);
                container.SetupHeader(section.Title, section.HeaderBackgroundColor);
                container.SetColumns(section.Columns);
                if (section.HasExtraInfo) container.SetupInfoButton(section.Lots.Select(lot => lot.ChestKey).Where(key => !string.IsNullOrEmpty(key)).Distinct().ToList());
                if (section.HasExtraHeader) container.SetupExtraHeader(section.ExtraTitle, section.ExtraHeaderBackgroundColor);
                if (section.HasOfferWallSection && abTestService.IsOfferwall() && playerModel.LastUnlockedLocation.Number >= abTestService.GetOfferwallLastLocationUnlocked()) container.SetupOfferwallSection();
                int count = 0; float preferred = 0f;
                foreach (var def in section.Lots) {
                    var model = shopLotRepository.Get(def.Key);
                    if (model == null || !(model.TotalValue > 0.0)) continue;
                    count++;
                    var item = Instantiate(itemPrefab, container.ItemsRoot);
                    Injector.InjectInto(item); item.Setup(def, model);
                    preferred = Mathf.Max(preferred, item.PreferredHeight);
                }
                container.SetItemsCount(count, preferred);
                if (count < 1) Destroy(container.gameObject); else spawnedContainers.Add(container);
            }
		}

		private List<string> GetVisibleLotKeys(List<ShopSectionDef> sections)
		{
            // Native 0x162d140: keys retain authored section/lot order and duplicates.
            var keys = new List<string>();
            foreach (var section in sections) foreach (var def in section.Lots) {
                var model = shopLotRepository.Get(def.Key);
                if (model != null && model.TotalValue > 0.0) keys.Add(def.Key);
            }
            return keys;
		}

		private List<ShopOfferBonusData> BuildSoulPassBonuses()
		{
            // Native 0x162a77c, exact original order and integer-percent truncation.
            return new List<ShopOfferBonusData> {
                new ShopOfferBonusData(soulStyleConfig.GetIcon("divine"), LocalizationStrings.Get("SHOP_BANNER_SOUL_PASS_DIVINE").Replace("{value}", amuletConfig.PassOneTimeDivineSouls.ToString())),
                new ShopOfferBonusData(currencyStyleConfig.GetIcon("boss_tower.tickets"), LocalizationStrings.Get("SHOP_BANNER_SOUL_PASS_TICKETS").Replace("{value}", amuletConfig.PassDailyBossTickets.ToString())),
                new ShopOfferBonusData(soulStyleConfig.GetIcon("great"), LocalizationStrings.Get("SHOP_BANNER_SOUL_PASS_LEGENDARY").Replace("{value}", amuletConfig.PassDailyLegendarySouls.ToString())),
                new ShopOfferBonusData(amuletConfig.PassSoulDropIcon, LocalizationStrings.Get("SHOP_BANNER_SOUL_PASS_DROP").Replace("{value}", ((int)(amuletConfig.PassDoubleSoulChance * 100f)).ToString()), true)
            };
		}

		private List<ShopOfferBonusData> BuildCrystalPassBonuses()
		{
            // Native 0x162ab58; minutes use original integer division, not rounding.
            return new List<ShopOfferBonusData> {
                new ShopOfferBonusData(crystalConfig.PassSlotIcon, LocalizationStrings.Get("SHOP_BANNER_CRYSTAL_PASS_SLOT")),
                new ShopOfferBonusData(crystalConfig.PassBlacksmithingIcon, LocalizationStrings.Get("SHOP_BANNER_CRYSTAL_PASS_BLACKSMITHING").Replace("{value}", crystalConfig.PassBlacksmithingBonus.ToString())),
                new ShopOfferBonusData(crystalConfig.PassEnchantmentIcon, LocalizationStrings.Get("SHOP_BANNER_CRYSTAL_PASS_ENCHANTMENT").Replace("{value}", crystalConfig.PassEnchantmentBonus.ToString())),
                new ShopOfferBonusData(crystalConfig.PassEnchantStonesIcon, LocalizationStrings.Get("SHOP_BANNER_CRYSTAL_PASS_ENCHANT_STONES").Replace("{value}", crystalConfig.PassMaxEnchantStonesBonus.ToString())),
                new ShopOfferBonusData(crystalConfig.PassGeodeTimerIcon, LocalizationStrings.Get("SHOP_BANNER_CRYSTAL_PASS_GEODE_TIMER").Replace("{value}", (crystalConfig.PassGeodeTimerReductionSeconds / 60).ToString())),
                new ShopOfferBonusData(crystalConfig.PassGeodesIcon, LocalizationStrings.Get("SHOP_BANNER_CRYSTAL_PASS_MAX_GEODES").Replace("{value}", crystalConfig.PassExtraMaxGeodes.ToString()))
            };
		}

		private List<ShopOfferBonusData> BuildChaoticSoulOfferBonuses()
		{
            // Native 0x162afac.
            return new List<ShopOfferBonusData> {
                new ShopOfferBonusData(soulStyleConfig.GetIcon("chaotic"), LocalizationStrings.Get("SHOP_BANNER_CHAOTIC_SOUL_REWARD")),
                ShopOfferBonusData.Description(LocalizationStrings.Get("SHOP_BANNER_CHAOTIC_SOUL_DESCRIPTION"))
            };
		}

		private void TryScrollToChaoticSoulOffer()
		{
            // Native 0x162b548 clears pending before forcing layout/scroll.
            if (!scrollToChaoticSoulOfferPending) return;
            scrollToChaoticSoulOfferPending = false; Canvas.ForceUpdateCanvases(); ScrollToChaoticSoulOffer();
		}

		private void ScrollToChaoticSoulOffer()
		{
            // Native 0x162d550 ASM tracks rect-height/local-y/pivot registers; denominator minimum is original float32 .0001.
            var scroll = contentRoot.GetComponentInParent<ScrollRect>();
            if (scroll == null) return;
            var target = chaoticSoulOfferSection.transform as RectTransform;
            if (target == null) return;
            Canvas.ForceUpdateCanvases();
            var content = scroll.content;
            var viewport = scroll.viewport != null ? scroll.viewport : (RectTransform)scroll.transform;
            float contentHeight = content.rect.height, viewportHeight = viewport.rect.height;
            float localY = content.InverseTransformPoint(target.position).y;
            float normalized = (localY + contentHeight * content.pivot.y + viewportHeight * -.5f) / Mathf.Max(.0001f, contentHeight - viewportHeight);
            Vector2 position = scroll.normalizedPosition; position.y = Mathf.Clamp01(normalized); scroll.normalizedPosition = position;
		}
	}
}
