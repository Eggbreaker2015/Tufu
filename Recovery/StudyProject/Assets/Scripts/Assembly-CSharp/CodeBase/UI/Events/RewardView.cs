using Codebase.UI.Shop;
using Codebase.UI.Stats;
using Codebase.Utilities;
using System.Collections.Generic;
using System.Linq;
using Codebase.Amulet;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Player;
using Codebase.Player.Inventory;
using Codebase.Rewards;
using Codebase.Simulation.Systems;
using Codebase.UI.Amulet;
using Codebase.UI.Inventory;
using Codebase.Unit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Events
{
	public class RewardView : BaseView
	{
		[SerializeField]
		private Image icon;

		[SerializeField]
		private TMP_Text amountText;

		[SerializeField]
		private InventoryItemView itemView;

		[SerializeField]
		private Image chestIcon;

		[SerializeField]
		private TMP_Text chestAmountText;

		[SerializeField]
		private Button chestInfoButton;

		[SerializeField]
		private Button statInfoButton;

		[SerializeField]
		private GameObject statInfoButtonBg;

		[SerializeField]
		private GameObject statState;

		[SerializeField]
		private GameObject chestState;

		[SerializeField]
		private GameObject itemState;

		[SerializeField]
		private LayoutElement layoutElement;

		[Header("Simplified State")]
		[SerializeField]
		private GameObject simplifiedStatState;

		[SerializeField]
		private GameObject simplifiedRewardGlow;

		[SerializeField]
		private Image simplifiedStatIcon;

		[SerializeField]
		private TMP_Text simplifiedStatAmountText;

		[SerializeField]
		private TMP_Text simplifiedStatTypeText;

		[SerializeField]
		private GameObject simplifiedChestState;

		[SerializeField]
		private Image simplifiedChestIcon;

		[SerializeField]
		private TMP_Text simplifiedChestAmountText;

		[SerializeField]
		private GameObject simplifiedItemState;

		[SerializeField]
		private GameObject simplifiedRandomItemState;

		[Header("Simplified Random Item State")]
		[SerializeField]
		private Image simplifiedRandomItemBackground;

		[SerializeField]
		private TMP_Text simplifiedRandomItemLevelText;

		[SerializeField]
		private Sprite simplifiedRandomItemArmorSprite;

		[SerializeField]
		private Sprite simplifiedRandomItemWeaponSprite;

		[Header("Simplified Soul State")]
		[SerializeField]
		private GameObject simplifiedSoulState;

		[SerializeField]
		private SoulView simplifiedSoulIcon;

		[SerializeField]
		private TMP_Text simplifiedSoulAmountText;

		[Header("Simplified Sizes")]
		[SerializeField]
		private Vector2 simplifiedStatSize = new Vector2(120.0f, 120.0f);

		[SerializeField]
		private Vector2 simplifiedChestSize = new Vector2(140.0f, 120.0f);

		[SerializeField]
		private Vector2 simplifiedItemSize = new Vector2(130f, 130f);

		[SerializeField]
		private Vector2 simplifiedRandomItemSize = new Vector2(130f, 130f);

		[SerializeField]
		private Vector2 simplifiedSoulSize = new Vector2(120.0f, 120.0f);

		[SerializeField]
		private Vector2 minDefaultItemSize = new Vector2(170.0f, 170.0f);

		[Header("Generic State")]
		[SerializeField]
		private GameObject genericState;

		[SerializeField]
		private Image genericIcon;

		[SerializeField]
		private TMP_Text genericAmountText;

		[SerializeField]
		private GameObject genericClaimedState;

		[SerializeField]
		private Vector2 genericSize = new Vector2(100.0f, 100.0f);

		[SerializeField]
		private Vector2 bonusGapForItems = new Vector2(0.0f, 0.0f);

		[SerializeField]
		private Image cashbackBackground;

		[SerializeField]
		private bool hideStatValueSigns;

		[SerializeField]
		private bool ignoreValueColors;

		[Inject]
		private IconStyleConfig iconStyleConfig;

		[Inject]
		private ChestsConfig chestsConfig;

		[Inject]
		private SimulationSystem simulationSystem;

		[Inject]
		private ItemsProvider itemsProvider;

		[Inject]
		private RewardIconResolver rewardIconResolver;

		[Inject]
		private SoulStyleConfig soulStyleConfig;

		private RewardBase reward;

		private RewardViewStyle viewStyle;

		private bool isCashback;

		private bool isClaimed;

		public GameObject SimplifiedRewardGlow => this.simplifiedRewardGlow;

		protected override void Awake()
		{
			// Native ELF VA 0x1661b64 and chest callback 0x166366c.
			base.Awake();
			chestInfoButton.onClick.AddListener(() => {
			    var keys = new List<string>();
			    if (reward is ChestReward chest) keys.Add(chest.Key);
			    GameContext.Get<UINavigator>().Open<ChestsInfoView>("Assets/Prefabs/UI/ChestChancesInfo.prefab").Setup(keys);
			});
			statInfoButton.onClick.AddListener(OnStatInfoButtonClick);
		}

		private void OnStatInfoButtonClick()
		{
			// Native ELF VA 0x1661c58.
			if (reward is StatReward stat)
			    GameContext.Get<UINavigator>().Open<StatDescriptionView>("Assets/Prefabs/UI/Stats/StatDescriptionView.prefab", true)
			        .Setup(stat.BakedData == null ? stat.Type : stat.BakedData.Type);
		}

		public void Setup(RewardBase reward, RewardViewStyle style = RewardViewStyle.Normal, bool isCashback = false, bool isClaimed = false)
		{
			// Native ELF VA 0x165b3b4.
			this.reward = reward; viewStyle = style; this.isCashback = isCashback; this.isClaimed = isClaimed;
			simplifiedSoulState.gameObject.SetActive(false);
			SetMinSize(minDefaultItemSize.x, minDefaultItemSize.y);
			RenderReward();
		}

		public void Refresh()
		{
			// Native ELF VA 0x16620f8.
			RenderReward();
		}

		private void RenderReward()
		{
			// Native ELF VA 0x1661dcc; retain original type order and unknown-type no-op.
			simplifiedStatTypeText.gameObject.SetActive(false);
			simplifiedStatAmountText.gameObject.SetActive(true);
			if (viewStyle == RewardViewStyle.Generic) { RenderGenericReward(); return; }
			if (reward is StatReward stat) RenderStatReward(stat);
			else if (reward is ChestReward chest) RenderChestReward(chest);
			else if (reward is ItemReward item) RenderItemReward(item);
			else if (reward is SoulReward soul) RenderSoulReward(soul);
			else if (reward is ItemSlotReward slot) RenderItemSlotReward(slot);
			else if (reward is MineUnlockReward) RenderStatLikeReward(rewardIconResolver.Resolve(reward), "");
			else if (reward is GemsReward || reward is CurrencyReward || reward is EventCurrencyReward) RenderCurrencyReward();
		}

		private void RenderCurrencyReward()
		{
			// Native ELF VA 0x1662b70.
			RenderStatLikeReward(rewardIconResolver.Resolve(reward), rewardIconResolver.FormatAmount(reward));
		}

		private void RenderItemSlotReward(ItemSlotReward itemSlotReward)
		{
			// Native ELF VA 0x1662954.
			string number = GetSlotNumber(itemSlotReward.SlotKey);
			RenderStatLikeReward(rewardIconResolver.Resolve(itemSlotReward), number);
			if (viewStyle != RewardViewStyle.Simplified) return;
			simplifiedStatAmountText.gameObject.SetActive(false);
			simplifiedStatTypeText.gameObject.SetActive(true);
			simplifiedStatTypeText.text = number;
		}

		private static string GetSlotNumber(string slotKey)
		{
			// Native ELF VA 0x1662bc8.
			foreach (string piece in slotKey.Split('_')) if (int.TryParse(piece, out int number)) return number.ToString();
			return slotKey;
		}

		private void RenderStatLikeReward(Sprite sprite, string amount)
		{
			// Native ELF VA 0x1662a1c.
			SetActiveState(statState, simplifiedStatState); SetStatInfoButtonActive(false);
			cashbackBackground.enabled = isCashback;
			Image image = viewStyle == RewardViewStyle.Simplified ? simplifiedStatIcon : icon;
			TMP_Text text = viewStyle == RewardViewStyle.Simplified ? simplifiedStatAmountText : amountText;
			if (sprite != null) image.sprite = sprite;
			image.enabled = sprite != null; text.text = amount;
			if (viewStyle == RewardViewStyle.Simplified) text.color = Color.white;
			SetStatSize();
		}

		private void RenderStatReward(StatReward statReward)
		{
			// Native ELF VA 0x166223c.
			SetActiveState(statState, simplifiedStatState); cashbackBackground.enabled = isCashback;
			Image image = viewStyle == RewardViewStyle.Simplified ? simplifiedStatIcon : icon;
			TMP_Text text = viewStyle == RewardViewStyle.Simplified ? simplifiedStatAmountText : amountText;
			if ((statReward.IsRandom || statReward.UseGpm) && !string.IsNullOrEmpty(statReward.SaveKey) && statReward.BakedData == null)
			{
			    image.enabled = false; text.text = ""; SetStatInfoButtonActive(false); SetStatSize(); return;
			}
			StatType type = statReward.BakedData == null ? statReward.Type : statReward.BakedData.Type;
			double value = statReward.BakedData == null ? statReward.Value : statReward.BakedData.Value;
			if (statReward.BakedData == null && statReward.UseGpm) value *= simulationSystem.RPMCalculatedValue[type];
			StatIconStyle style = iconStyleConfig.Styles.FirstOrDefault(entry => entry.StatType == type.GetIconStatType());
			if (style != null) { image.sprite = style.Sprite; image.enabled = true; }
			text.text = FormatStatRewardValue(value, type, statReward);
			if (viewStyle == RewardViewStyle.Simplified)
			    text.color = statReward.CustomModifier && statReward.ModifierType == ModifierType.EventPercentModifier && !ignoreValueColors
			        ? new Color(1f, 0.9215686321258545f, 0.01568627543747425f, 1f) : Color.white;
			SetStatInfoButtonActive(((int)type & ~1) == 30); SetStatSize();
		}

		private string FormatStatRewardValue(double value, StatType statType, StatReward statReward)
		{
			// Native ELF VA 0x1662ffc; literal +/% and original modifier mask 0x36.
			string sign = hideStatValueSigns ? "" : "+";
			if (statReward.CustomModifier)
			{
			    int modifier = (int)statReward.ModifierType;
			    if ((uint)modifier <= 7 && ((1 << modifier) & 0x36) != 0) return sign + (value * 100.0).Format() + "%";
			    if (modifier == 0) return sign + value.Format();
			    if (modifier == 7) return sign + value.Format() + "%";
			}
			return sign + (statType.IsPercentDisplay() ? (value * 100.0).Format() + "%" : value.Format());
		}

		private void SetStatInfoButtonActive(bool active)
		{
			// Native ELF VA 0x1662f7c.
			statInfoButton.enabled = active;
			statInfoButtonBg.gameObject.SetActive(active);
		}

		private void SetStatSize()
		{
			// Native ELF VA 0x1662fcc.
			Vector2 size = viewStyle == RewardViewStyle.Simplified ? simplifiedStatSize : new Vector2(190f,190f);
			SetPreferredSize(size.x,size.y);
		}

		private void RenderChestReward(ChestReward chestReward)
		{
			// Native ELF VA 0x166258c.
			SetActiveState(chestState, simplifiedChestState); SetStatInfoButtonActive(false);
			if (!chestsConfig.TryGet(chestReward.Key, out ChestDef def)) return;
			Image image = viewStyle == RewardViewStyle.Simplified ? simplifiedChestIcon : chestIcon;
			TMP_Text text = viewStyle == RewardViewStyle.Simplified ? simplifiedChestAmountText : chestAmountText;
			image.sprite = def.ClosedSprite; image.enabled = true;
			text.text = string.Format("x{0}", chestReward.Amount);
			Vector2 size = viewStyle == RewardViewStyle.Simplified ? simplifiedChestSize : new Vector2(230f,200f);
			SetPreferredSize(size.x,size.y);
		}

		private void RenderItemReward(ItemReward itemReward)
		{
			// Native ELF VA 0x16626f0. ItemModel uses real config/copies, never grants the display model.
			SetStatInfoButtonActive(false);
			if (itemReward.IsRandom && (itemReward.BakedData == null || itemReward.ForceShowAsRandom)) { RenderRandomItemReward(itemReward); return; }
			SetActiveState(itemState, simplifiedItemState);
			ItemConfig config = itemReward.BakedData == null ? itemReward.Def : itemsProvider.Get(itemReward.BakedData.ItemKey);
			ItemModel model = new ItemModel(Lifetime, config); model.SetInitialCopies(itemReward.Level);
			itemView.Initialize(model, false);
			Vector2 size = (viewStyle == RewardViewStyle.Simplified ? simplifiedItemSize : new Vector2(210f,210f)) + bonusGapForItems;
			SetPreferredSize(size.x,size.y);
		}

		private void RenderRandomItemReward(ItemReward itemReward)
		{
			// Native ELF VA 0x16631f8.
			SetActiveState(simplifiedRandomItemState, simplifiedRandomItemState);
			simplifiedRandomItemBackground.color = itemsProvider.GetRarityColor(itemReward.Rarity);
			simplifiedRandomItemBackground.sprite = itemReward.SlotType == 0 ? simplifiedRandomItemWeaponSprite : simplifiedRandomItemArmorSprite;
			simplifiedRandomItemLevelText.text = "x" + itemReward.Level.ToString();
			SetPreferredSize(simplifiedRandomItemSize.x,simplifiedRandomItemSize.y);
		}

		private void RenderGenericReward()
		{
			// Native ELF VA 0x16620fc.
			SetActiveGenericState(); Sprite sprite = rewardIconResolver.Resolve(reward);
			if (sprite != null) genericIcon.sprite = sprite;
			genericIcon.enabled = sprite != null; genericAmountText.text = rewardIconResolver.FormatAmount(reward);
			if (genericClaimedState != null) genericClaimedState.SetActive(isClaimed);
			SetPreferredSize(genericSize.x,genericSize.y);
		}

		private void RenderSoulReward(SoulReward soulReward)
		{
			// Native ELF VA 0x166285c.
			SetActiveSoulState(); simplifiedSoulIcon.Setup(soulStyleConfig.GetStyle(soulReward.SoulKey));
			simplifiedSoulAmountText.text = string.Format("x{0}", soulReward.Amount);
			SetPreferredSize(simplifiedSoulSize.x,simplifiedSoulSize.y); SetMinSize(simplifiedSoulSize.x,simplifiedSoulSize.y);
		}

		private void SetActiveSoulState()
		{
			// Native ELF VA 0x166344c.
			statState.SetActive(false); chestState.SetActive(false); simplifiedStatState.SetActive(false); simplifiedChestState.SetActive(false);
			if (simplifiedRandomItemState != null) simplifiedRandomItemState.SetActive(false);
			itemState.SetActive(false); simplifiedItemState.SetActive(false); genericState.SetActive(false); simplifiedSoulState.SetActive(true);
		}

		private void SetActiveGenericState()
		{
			// Native ELF VA 0x1663300.
			statState.SetActive(false); chestState.SetActive(false); simplifiedStatState.SetActive(false); simplifiedChestState.SetActive(false);
			if (simplifiedRandomItemState != null) simplifiedRandomItemState.SetActive(false);
			itemState.SetActive(false); if (simplifiedItemState != itemState) simplifiedItemState.SetActive(false);
			if (genericState != null) genericState.SetActive(true);
		}

		public void SetClaimed(bool claimed)
		{
			// Native ELF VA 0x1663510.
			isClaimed = claimed;
			if (genericClaimedState != null) genericClaimedState.SetActive(claimed);
		}

		private void SetActiveState(GameObject normalState, GameObject simplifiedState)
		{
			// Native ELF VA 0x1662c6c; item normal/simplified states may be the same GameObject.
			bool simple = viewStyle == RewardViewStyle.Simplified;
			statState.SetActive(!simple && statState == normalState);
			chestState.SetActive(!simple && chestState == normalState);
			simplifiedStatState.SetActive(simple && simplifiedStatState == simplifiedState);
			simplifiedChestState.SetActive(simple && simplifiedChestState == simplifiedState);
			if (simplifiedRandomItemState != null) simplifiedRandomItemState.SetActive(simplifiedRandomItemState == simplifiedState);
			if (itemState == simplifiedItemState) itemState.SetActive(normalState == itemState || simplifiedState == simplifiedItemState);
			else { itemState.SetActive(!simple && itemState == normalState); simplifiedItemState.SetActive(simple && simplifiedItemState == simplifiedState); }
			if (genericState != null) genericState.SetActive(false);
		}

		private void SetPreferredSize(float width, float height)
		{
			// Native ELF VA 0x16631a8.
			layoutElement.preferredWidth = width;
			layoutElement.preferredHeight = height;
		}

		private void SetMinSize(float width, float height)
		{
			// Native ELF VA 0x1661d7c.
			layoutElement.minWidth = width;
			layoutElement.minHeight = height;
		}

		public void SetInfoBtnVisible(bool visible)
		{
			// Native ELF VA 0x16635b0.
			if (itemView != null) itemView.SetInfoBtnVisible(visible);
		}
	}
}
