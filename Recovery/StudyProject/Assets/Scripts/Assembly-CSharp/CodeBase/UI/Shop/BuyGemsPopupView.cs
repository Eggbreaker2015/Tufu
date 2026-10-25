using System.Linq;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Codebase.Amulet;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Economy;
using Codebase.Shop;
using UniMob;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Shop
{
	public class BuyGemsPopupView : PopupView
	{
		private const int MaxCurrencyLots = 3;

		[SerializeField]
		private Button closeButton;

		[SerializeField]
		private Transform lotsRoot;

		[SerializeField]
		private BuyGemsLotView lotPrefab;

		[Header("Currency")]
		[SerializeField]
		private Transform currencyContainer;

		[SerializeField]
		private Codebase.UI.CurrencyView currencyViewPrefab;

		[SerializeField]
		private string gemsCurrencyKey = "gems";

		[Header("Fly Animation")]
		[SerializeField]
		private CurrencyFlyAnimator flyAnimatorPrefab;

		[SerializeField]
		private CanvasGroup canvasGroup;

		[Inject]
		private ShopLotRepository shopLotRepository;

		[Inject]
		private CurrencyStyleConfig currencyStyleConfig;

		[Inject]
		private SoulStyleConfig soulStyleConfig;

		private ViewContainer<BuyGemsLotView, ShopLotModel> lotsContainer;

		private LifetimeController openLifetime;

		private string currencyKey;

		private Sprite currencyIcon;

		private bool isCurrencyMode;

		private Codebase.UI.CurrencyView gemsCurrencyView;

		private Codebase.UI.CurrencyView dynamicCurrencyView;

		public event Action OnGemsPurchased;

		protected override void Awake()
		{
            // Original ELF 0x1619740.
            base.Awake(); closeButton.onClick.AddListener(Close);
		}

		public void SetupAsGems(double minGemsNeeded = 0.0)
		{
            // Original ELF 0x16197dc.
            PrepareOpen(); isCurrencyMode = false; currencyKey = null; currencyIcon = null;
            SpawnCurrencyViews(); InitializeGemsMode(minGemsNeeded);
		}

		public void Setup(string currencyKey, double requiredAmount = 0.0)
		{
            // Original ELF 0x1619c90.
            PrepareOpen(); isCurrencyMode = true; this.currencyKey = currencyKey; currencyIcon = GetCurrencyIcon(currencyKey);
            SpawnCurrencyViews(); InitializeCurrencyMode(currencyKey, requiredAmount);
		}

		private void PrepareOpen()
		{
            // Original ELF 0x1619830.
            CloseOpenLifetime(); DestroyCurrencyViews();
            openLifetime = new LifetimeController();
            lotsContainer = new ViewContainer<BuyGemsLotView, ShopLotModel>(lotsRoot, lotPrefab);
		}

		private void CloseOpenLifetime()
		{
            // Original ELF 0x1619fe8.
            if (openLifetime != null && !openLifetime.IsDisposed) openLifetime.Dispose();
            openLifetime = null;
		}

		private void SpawnCurrencyViews()
		{
            // Original ELF 0x1619900.
            gemsCurrencyView = SpawnCurrencyView(gemsCurrencyKey);
            if (isCurrencyMode && !string.IsNullOrEmpty(currencyKey)) dynamicCurrencyView = SpawnCurrencyView(currencyKey);
		}

		private Codebase.UI.CurrencyView SpawnCurrencyView(string key)
		{
            // Original ELF 0x161a150.
            var view = Instantiate(currencyViewPrefab, currencyContainer);
            Injector.InjectInto(view); view.Setup(key); return view;
		}

		private void DestroyCurrencyViews()
		{
            // Original ELF 0x161a01c.
            if (gemsCurrencyView != null) { Destroy(gemsCurrencyView.gameObject); gemsCurrencyView = null; }
            if (dynamicCurrencyView != null) { Destroy(dynamicCurrencyView.gameObject); dynamicCurrencyView = null; }
		}

		private void InitializeGemsMode(double minGemsNeeded)
		{
            // Original ELF 0x1619970 and its three captured delegates.
            var lots = FilterGemsLots(shopLotRepository.GetByType(ShopLotType.Gems).OrderBy(lot => lot.Def.Value).ToList(), minGemsNeeded);
            lotsContainer.BindTo(openLifetime.Lifetime, () => lots)
                .Setup((view, lot) => { view.Setup(lot); view.OnPurchased += HandleGemsPurchased; })
                .OnCleanup(view => view.OnPurchased -= HandleGemsPurchased);
		}

		private List<ShopLotModel> FilterGemsLots(List<ShopLotModel> allLots, double minGemsNeeded)
		{
            // Original ELF 0x161a230. LS/CCMP-LT retain unordered traversal; the slice starts at first covering offer, with two following offers when available.
            if (allLots.Count == 0) return allLots;
            if (!(minGemsNeeded <= 0.0))
            {
                double deficit = minGemsNeeded - GameContext.Get<global::Codebase.Player.Models.PlayerModel>().Gems;
                if (!(deficit <= 0.0))
                {
                    int index = 0;
                    while (!(allLots[index].Def.Value >= deficit) && index + 1 < allLots.Count) index++;
                    int end = Math.Min(index + 3, allLots.Count);
                    int start = Math.Max(end - 3, 0);
                    return allLots.GetRange(start, end - start);
                }
            }
            return allLots.Take(MaxCurrencyLots).ToList();
		}

		private void InitializeCurrencyMode(string currencyKey, double requiredAmount)
		{
            // Original ELF 0x1619dcc and three captured delegates.
            var lots = GetLotsForCurrency(shopLotRepository, currencyKey, requiredAmount);
            lotsContainer.BindTo(openLifetime.Lifetime, () => lots)
                .Setup((view, lot) => { view.SetupAsCurrencyLot(lot, currencyIcon); view.OnPurchased += HandleCurrencyPurchased; view.OnGemsInsufficient += HandleGemsInsufficient; })
                .OnCleanup(view => { view.OnPurchased -= HandleCurrencyPurchased; view.OnGemsInsufficient -= HandleGemsInsufficient; });
		}

		private void HandleGemsInsufficient(double lotCost)
		{
            // Original ELF 0x161a97c.
            GameContext.Get<UINavigator>().Open<BuyGemsPopupView>("Assets/Prefabs/UI/Rewarded/BuyGemsPopup.prefab", false).SetupAsGems(lotCost);
		}

		private void HandleGemsPurchased(BuyGemsLotView sender)
		{
            // Original ELF 0x161aa38 and 0x161baf4.
            if (isCurrencyMode) return;
            PlayPurchaseAnimation(sender, gemsCurrencyKey, gemsCurrencyView.IconRect, () => { OnGemsPurchased?.Invoke(); Close(); });
		}

		private void HandleCurrencyPurchased(BuyGemsLotView sender)
		{
            // Original ELF 0x161acc0 and 0x161bb24.
            if (!isCurrencyMode) return;
            PlayPurchaseAnimation(sender, currencyKey, (dynamicCurrencyView != null ? dynamicCurrencyView : gemsCurrencyView).IconRect, () => { OnGemsPurchased?.Invoke(); Close(); });
		}

		private void PlayPurchaseAnimation(BuyGemsLotView sender, string currencyKey, RectTransform target, Action onCompleted)
		{
            // Original ELF 0x161ab00: fly animator reparents itself to the target root Canvas.
            SetLotsInteractable(false); closeButton.interactable = false;
            var animator = Instantiate(flyAnimatorPrefab, transform);
            animator.SetCurrencyKey(currencyKey); animator.Setup(target);
            animator.Play(sender.IconRect, () => FadeOutAndClose(onCompleted));
		}

		private void FadeOutAndClose(Action onCompleted)
		{
            // Original ELF 0x161b468/0x161bf5c: the caller supplies Close; the fade callback restores alpha first.
            canvasGroup.DOFade(0f, 0.5f).SetEase((Ease)6).OnComplete(() => { canvasGroup.alpha = 1f; onCompleted?.Invoke(); });
		}

		private void SetLotsInteractable(bool value)
		{
            // Original ELF 0x161add4.
            foreach (var view in lotsContainer.Active) view.SetInteractable(value);
		}

		public static bool IsChaoticSoulCurrency(string currencyKey)
		{
            // Original ELF 0x161b5a0.
            return currencyKey == "soul.chaotic";
		}

		public static bool IsChaoticSoulOfferAvailable()
		{
            // Original ELF 0x161b5ec.
            return !GameContext.Get<global::Codebase.Analytics.UserModel>().ChaoticSoulOfferPurchased.Value;
		}

		public static bool HasPurchaseOptionForCurrency(ShopLotRepository repository, string currencyKey, double requiredAmount = 0.0)
		{
            // Original ELF 0x161b694.
            if (string.IsNullOrEmpty(currencyKey)) return false;
            return IsChaoticSoulCurrency(currencyKey) ? IsChaoticSoulOfferAvailable() : GetLotsForCurrency(repository, currencyKey, requiredAmount).Count > 0;
		}

		public static List<ShopLotModel> GetLotsForCurrency(ShopLotRepository repository, string currencyKey, double requiredAmount = 0.0)
		{
            // Original ELF 0x161a3e0. Soul keys are stored without their currency prefix in lot definitions.
            if (string.IsNullOrEmpty(currencyKey)) return new List<ShopLotModel>();
            List<ShopLotModel> all;
            if (currencyKey.StartsWith("soul."))
            {
                string key = currencyKey.Substring("soul.".Length);
                all = repository.GetByType(ShopLotType.Soul).Where(lot => lot.Def.SoulKey == key && lot.TotalValue > 0.0).OrderBy(lot => lot.TotalValue).ToList();
            }
            else all = repository.GetByType(ShopLotType.Currency).Where(lot => lot.Def.CurrencyKey == currencyKey && lot.TotalValue > 0.0).OrderBy(lot => lot.TotalValue).ToList();
            if (!(requiredAmount <= 0.0) && all.Count != 0)
            {
                double deficit = requiredAmount - GameContext.Get<CurrencyModel>().Get(currencyKey);
                if (!(deficit <= 0.0))
                {
                    var covering = all.Where(lot => lot.TotalValue >= deficit).ToList();
                    if (covering.Count > 0) return covering.Take(MaxCurrencyLots).ToList();
                    return new List<ShopLotModel> { CreateScaledLot(all.Last(), repository, deficit) };
                }
            }
            return all.Take(MaxCurrencyLots).ToList();
		}

		private static ShopLotModel CreateScaledLot(ShopLotModel baseLot, ShopLotRepository repository, double deficit)
		{
            // Original ELF 0x161b744: definition clone and original model lifetime/UD; scaled values are not added to the repository.
            double total = baseLot.TotalValue;
            double cost = RoundCostTo50((deficit / total) * baseLot.Def.Cost);
            ShopLotDef def = CloneLotDef(baseLot.Def); def.Key = baseLot.Def.Key + "_popup_scaled"; def.IsFree = false;
            var lot = new ShopLotModel(repository.Lifetime, def, repository.Data);
            Injector.InjectInto(lot); lot.RPM = baseLot.RPM; lot.SetScaledValues(deficit, cost); return lot;
		}

		private static double RoundCostTo50(double cost)
		{
            // Original ELF 0x161b8c0, FRINTP (not the truncation printed by the decompiler).
            return Math.Ceiling(cost / 50.0) * 50.0;
		}

		private static ShopLotDef CloneLotDef(ShopLotDef source)
		{
            // Original ELF 0x161b92c copies every original definition field, preserving Sprite identity.
            return new ShopLotDef { Key = source.Key, LotType = source.LotType, IsFree = source.IsFree, CanClaimOnFirstDay = source.CanClaimOnFirstDay,
                StatType = source.StatType, ChestKey = source.ChestKey, CurrencyKey = source.CurrencyKey, SoulKey = source.SoulKey,
                FreeValue = source.FreeValue, Value = source.Value, RequiredLocationNumber = source.RequiredLocationNumber, Cost = source.Cost,
                ProductKey = source.ProductKey, Sprite = source.Sprite, IconType = source.IconType };
		}

		private Sprite GetCurrencyIcon(string key)
		{
            // Original ELF 0x1619d04.
            if (string.IsNullOrEmpty(key)) return null;
            return key.StartsWith("soul.") ? soulStyleConfig.GetIcon(key.Substring("soul.".Length)) : currencyStyleConfig.GetIcon(key);
		}

		public override void Cleanup()
		{
            // Original ELF 0x161ba20.
            base.Cleanup(); canvasGroup.DOKill(); canvasGroup.alpha = 1f; OnGemsPurchased = null;
            closeButton.interactable = true; CloseOpenLifetime(); DestroyCurrencyViews();
		}
	}
}
