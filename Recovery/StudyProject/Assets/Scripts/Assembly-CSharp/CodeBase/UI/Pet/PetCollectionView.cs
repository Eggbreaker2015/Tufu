using System;
using System.Collections.Generic;
using System.Text;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Pet;
using Codebase.Pet.Commands;
using Codebase.Player;
using Codebase.Player.Inventory;
using Codebase.Player.Models;
using Codebase.UI.Inventory;
using Codebase.Unit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Pets
{
	// Pet mode 图鉴 / 羁绊 popup; built in code by PetUiBuilder and pooled by the navigator.
	public class PetCollectionView : PopupView
	{
		[Serializable]
		public class Layout
		{
			public RectTransform Panel;

			public TMP_Text Title;

			public Button CloseButton;

			public Button CollectionTab;

			public Image CollectionTabImage;

			public TMP_Text CollectionTabLabel;

			public Button SynergyTab;

			public Image SynergyTabImage;

			public TMP_Text SynergyTabLabel;

			public GameObject CollectionPage;

			public TMP_Text Progress;

			public TMP_Text Shards;

			public TMP_Text Bonus;

			public ScrollRect CollectionScroll;

			public RectTransform CollectionContent;

			public GameObject SynergyPage;

			public PetSynergyTab Synergy;
		}

		private class Group
		{
			public Rarity Rarity;

			public TMP_Text Header;

			public readonly List<PetBookCell> Cells = new List<PetBookCell>();
		}

		private static readonly Rarity[] RarityOrder = { Rarity.Common, Rarity.Uncommon, Rarity.Rare, Rarity.Epic, Rarity.Legendary, Rarity.Divine, Rarity.Mythical };

		private static readonly Color TabActiveColor = new Color(0.98f, 0.75f, 0.25f, 1f);

		private static readonly Color TabInactiveColor = new Color(0.35f, 0.38f, 0.48f, 1f);

		[SerializeField]
		private Layout layout = new Layout();

		private readonly List<Group> groups = new List<Group>();

		private readonly List<GameObject> builtObjects = new List<GameObject>();

		private readonly Dictionary<string, ItemModel> ownedItems = new Dictionary<string, ItemModel>();

		private readonly StringBuilder builder = new StringBuilder();

		private string builtSignature;

		private bool synergySelected;

		private bool open;

		private float nextRefreshTime;

		// Raises the HUD's WeaponRewardView above this popup while it is open (no raycaster, so it never eats taps).
		private Canvas rewardCanvas;

		internal Layout Elements => layout;

		protected override void Awake()
		{
			base.Awake();
			if (layout.CloseButton != null) layout.CloseButton.onClick.AddListener(OnCloseClicked);
			if (layout.CollectionTab != null) layout.CollectionTab.onClick.AddListener(() => SelectTab(false));
			if (layout.SynergyTab != null) layout.SynergyTab.onClick.AddListener(() => SelectTab(true));
		}

		public override void Initialize()
		{
			// Reset before base.Initialize, which can trigger the render reaction; the pooled instance keeps its last tab otherwise.
			synergySelected = false;
			open = true;
			nextRefreshTime = 0f;
			FitPanel();
			base.Initialize();
			RaiseRewardView(true);
			if (layout.CollectionScroll != null) layout.CollectionScroll.verticalNormalizedPosition = 1f;
		}

		public override void Cleanup()
		{
			base.Cleanup();
			open = false;
			RaiseRewardView(false);
		}

		private void RaiseRewardView(bool raise)
		{
			if (!GameContext.Has<global::Codebase.UI.World.WeaponRewardView>()) return;
			var reward = GameContext.Get<global::Codebase.UI.World.WeaponRewardView>();
			if (reward == null) return;
			if (raise)
			{
				Transform parent = reward.transform.parent;
				Canvas parentCanvas = parent != null ? parent.GetComponentInParent<Canvas>() : null;
				Canvas root = parentCanvas != null ? parentCanvas.rootCanvas : null;
				if (root == null) return;
				if (rewardCanvas == null)
				{
					if (reward.GetComponent<Canvas>() != null) return; // never touch a Canvas the scene already owns
					rewardCanvas = reward.gameObject.AddComponent<Canvas>();
					rewardCanvas.additionalShaderChannels = root.additionalShaderChannels; // 25 on UiCanvas; TMP needs them
				}
				rewardCanvas.overrideSorting = true;
				rewardCanvas.sortingOrder = root.sortingOrder + 1; // 3 -> 4
			}
			else if (rewardCanvas != null)
			{
				Destroy(rewardCanvas); // restores hierarchy-order drawing and the UiCanvas raycast registration
				rewardCanvas = null;
			}
		}

		private void Update()
		{
			// Shard balance, player stats and the locale font reapply are not atoms the render reaction tracks.
			if (open && Time.unscaledTime >= nextRefreshTime) SafeRender();
		}

		protected override void Render()
		{
			SafeRender();
		}

		private void SafeRender()
		{
			nextRefreshTime = Time.unscaledTime + PetStatsText.RefreshInterval;
			try
			{
				RenderAll();
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}

		private void RenderAll()
		{
			PetStatsText.SetText(layout.Title, "宠物图鉴");
			if (layout.CollectionPage != null) layout.CollectionPage.SetActive(!synergySelected);
			if (layout.SynergyPage != null) layout.SynergyPage.SetActive(synergySelected);
			RenderTab(layout.CollectionTabImage, layout.CollectionTabLabel, "图鉴", !synergySelected);
			RenderTab(layout.SynergyTabImage, layout.SynergyTabLabel, "羁绊", synergySelected);
			if (synergySelected)
			{
				if (layout.Synergy != null) layout.Synergy.Render();
				return;
			}
			RenderCollection();
		}

		private void RenderTab(Image image, TMP_Text label, string text, bool active)
		{
			PetConfig config = PetConfig.Instance;
			Sprite sprite = config == null ? null : active ? config.TabActiveSprite : config.TabInactiveSprite;
			PetUiBuilder.SetSprite(image, sprite, active ? TabActiveColor : TabInactiveColor);
			PetStatsText.SetText(label, text);
		}

		private void SelectTab(bool synergy)
		{
			if (synergySelected == synergy) return;
			PetUiBuilder.PlayTap();
			synergySelected = synergy;
			SafeRender();
		}

		private void OnCloseClicked()
		{
			PetUiBuilder.PlayTap();
			Close();
		}

		// The panel is laid out for the 1080x2338 reference canvas; narrower or shorter screens scale it down.
		private void FitPanel()
		{
			if (layout.Panel == null || rectTransform == null) return;
			Rect rect = rectTransform.rect;
			float scale = Mathf.Min(1f, (rect.width - 24f) / PetUiBuilder.PanelWidth, (rect.height - 60f) / PetUiBuilder.PanelHeight);
			if (float.IsNaN(scale) || scale <= 0.1f) scale = 1f;
			layout.Panel.localScale = new Vector3(scale, scale, 1f);
		}

		private void RenderCollection()
		{
			PetConfig config = PetConfig.Instance;
			PlayerModel playerModel = PetUiBuilder.Find<PlayerModel>();
			ItemsProvider provider = PetUiBuilder.Find<ItemsProvider>();
			IconStyleConfig styles = PetUiBuilder.Find<IconStyleConfig>();
			List<ItemModel> items = playerModel != null ? playerModel.Items : null;

			ownedItems.Clear();
			if (items != null)
				foreach (ItemModel item in items)
					if (item != null && item.Config != null && !item.IsArmor && !ownedItems.ContainsKey(item.Config.Key))
						ownedItems.Add(item.Config.Key, item);

			List<KeyValuePair<PetDefinition, ItemConfig>> species = CollectSpecies(config, provider);
			EnsureCells(species);

			int owned = PetCollection.CountOwnedSpecies(items);
			int total = PetCollection.TotalSpecies;
			PetStatsText.SetText(layout.Progress, "已收集 <color=#FFD54A>" + owned + "</color>/" + total);
			PetStatsText.SetText(layout.Shards, "万能碎片 <color=#7CE3FF>" + PetEconomy.Shards + "</color>");
			PetStatsText.SetText(layout.Bonus, BuildBonusText(owned));

			foreach (Group group in groups)
			{
				int groupOwned = 0;
				foreach (PetBookCell cell in group.Cells)
				{
					ownedItems.TryGetValue(cell.Config != null ? cell.Config.Key : string.Empty, out ItemModel item);
					if (item != null) groupOwned++;
					cell.Render(item, styles);
				}
				string color = RarityColor(provider, group.Rarity);
				PetStatsText.SetText(group.Header, "<color=" + color + ">" + RarityName(group.Rarity) + "</color>  <size=80%>" + groupOwned + "/" + group.Cells.Count + "</size>");
			}
		}

		private string BuildBonusText(int owned)
		{
			builder.Clear();
			var active = new List<StatModifier>();
			foreach (PetCollectionTier tier in PetCollection.ActiveTiers(owned))
				if (tier.Modifiers != null) active.AddRange(tier.Modifiers);
			builder.Append("收集加成: ");
			if (active.Count > 0) builder.Append("<color=#7CFF7C>").Append(PetStatsText.FormatModifiers(active)).Append("</color>");
			else builder.Append("<color=#A0A0A0>无</color>");
			PetCollectionTier next = PetCollection.NextTier(owned);
			builder.Append('\n');
			if (next != null)
				builder.Append("<color=#A0A0A0>下一档 收集").Append(next.SpeciesRequired).Append("种: ").Append(PetStatsText.FormatModifiers(next.Modifiers)).Append("</color>");
			else
				builder.Append("<color=#FFD54A>全部收集加成已激活</color>");
			return builder.ToString();
		}

		private static List<KeyValuePair<PetDefinition, ItemConfig>> CollectSpecies(PetConfig config, ItemsProvider provider)
		{
			var result = new List<KeyValuePair<PetDefinition, ItemConfig>>();
			if (config == null || provider == null || provider.Items == null) return result;
			foreach (PetDefinition definition in config.Species)
			{
				ItemConfig item = null;
				foreach (ItemConfig candidate in provider.Items)
					if (candidate != null && candidate.Key == definition.ItemKey)
					{
						item = candidate;
						break;
					}
				if (item != null) result.Add(new KeyValuePair<PetDefinition, ItemConfig>(definition, item));
			}
			return result;
		}

		// Groups and cells are built on the pooled instance once, and again only if the species list changes.
		private void EnsureCells(List<KeyValuePair<PetDefinition, ItemConfig>> species)
		{
			builder.Clear();
			foreach (var entry in species) builder.Append(entry.Value.Key).Append('|');
			string signature = builder.ToString();
			if (signature == builtSignature && layout.CollectionContent != null) return;
			builtSignature = signature;
			foreach (GameObject built in builtObjects)
				if (built != null) Destroy(built);
			builtObjects.Clear();
			groups.Clear();
			if (layout.CollectionContent == null) return;
			foreach (Rarity rarity in RarityOrder)
			{
				Group group = null;
				RectTransform grid = null;
				foreach (var entry in species)
				{
					if (entry.Value.Rarity != rarity) continue;
					if (group == null)
					{
						group = new Group { Rarity = rarity, Header = PetUiBuilder.CreateGroupHeader(layout.CollectionContent) };
						grid = PetUiBuilder.CreateGrid(layout.CollectionContent);
						builtObjects.Add(group.Header.gameObject);
						builtObjects.Add(grid.gameObject);
						groups.Add(group);
					}
					PetBookCell cell = PetUiBuilder.CreateCell(grid);
					cell.Setup(entry.Key, entry.Value, OnCellClicked, OnUnlockClicked);
					group.Cells.Add(cell);
				}
			}
		}

		private void OnCellClicked(PetBookCell cell)
		{
			PetUiBuilder.PlayTap();
			if (cell == null || cell.Item == null) return;
			UINavigator navigator = PetUiBuilder.Find<UINavigator>();
			ItemMiniView view = navigator != null ? navigator.Open<ItemMiniView>(UIConstants.ItemMiniView, true) : null;
			if (view != null) view.Setup(cell.Item);
		}

		private void OnUnlockClicked(PetBookCell cell)
		{
			if (cell == null || cell.Config == null || !PetEconomy.CanUnlock(cell.Config)) return;
			PetUiBuilder.PlayTap();
			App.ExecuteCommand(new UnlockPetSpeciesCommand { Config = cell.Config });
			SafeRender();
		}

		private static string RarityColor(ItemsProvider provider, Rarity rarity)
		{
			if (provider == null || provider.RarityColors == null || (int)rarity >= provider.RarityColors.Count) return "#FFFFFF";
			return provider.GetRarityColorCode(rarity);
		}

		public static string RarityName(Rarity rarity)
		{
			switch (rarity)
			{
				case Rarity.Common: return "普通";
				case Rarity.Uncommon: return "优秀";
				case Rarity.Rare: return "稀有";
				case Rarity.Epic: return "史诗";
				case Rarity.Legendary: return "传说";
				case Rarity.Divine: return "神圣";
				default: return "神话";
			}
		}
	}
}
