using System;
using System.Linq;
using System.Collections.Generic;
using DG.Tweening;
using Codebase.Analytics.Events;
using Codebase.UI.Shop;
using Codebase.Utilities;
using CodeBase.Audio;
using Codebase.Amulet;
using Codebase.Amulet.Upgrades;
using Codebase.Analytics;
using Codebase.Core;
using Codebase.Core.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Codebase.UI.Amulet
{
	public class AmuletUpgradeGraphView : PopupView
	{
		[SerializeField]
		private RectTransform contentRect;

		[SerializeField]
		private RectTransform nodesContainer;

		[SerializeField]
		private RectTransform pathsContainer;

		[SerializeField]
		private AmuletUpgradeNodeView nodePrefab;

		[SerializeField]
		private BezierPathGraphic pathPrefab;

		[SerializeField]
		private Texture2D pathTexture;

		[SerializeField]
		private ScrollRect scrollRect;

		[SerializeField]
		private Button closeButton;

		[SerializeField]
		private Button infoButton;

		[SerializeField]
		private Button shopButton;

		[SerializeField]
		private Button zoomOutButton;

		[SerializeField]
		private Image zoomIcon;

		[SerializeField]
		private Sprite zoomInSprite;

		[SerializeField]
		private Sprite zoomOutSprite;

		[Header("Tooltip")]
		[SerializeField]
		private AmuletUpgradeNodeTooltipView nodeTooltip;

		[Header("Soul Currencies")]
		[SerializeField]
		private RectTransform soulsContainer;

		[SerializeField]
		private AmuletSoulCurrencyView soulCurrencyPrefab;

		[Header("Level Tabs")]
		[SerializeField]
		private RectTransform levelButtonsContainer;

		[SerializeField]
		private AmuletGraphLevelButton levelButtonPrefab;

		[SerializeField]
		private int levelsCount = 10;

		[Header("Level Info")]
		[SerializeField]
		private Image levelProgressFill;

		[SerializeField]
		private Image levelIcon;

		[SerializeField]
		private TMP_Text levelText;

		private static readonly Color LevelAccentDefaultColor = new Color(1f, 0.8f, 0.65882355f, 1f);

		private static readonly Color LevelAccentMaxedColor = ParseHexColor("#49FD8D");

		private static readonly string[] SoulRarityOrder = { "common", "large", "elite", "huge", "great", "divine", "chaotic" };

		[Header("Content Settings")]
		[SerializeField]
		private float contentPadding = 100f;

		[Inject]
		private AmuletUpgradeConfig config;

		[Inject]
		private AmuletModel amuletModel;

		[Inject]
		private AmuletUpgradesModel upgradesModel;

		[Inject]
		private SoulStyleConfig soulStyleConfig;

		[Inject]
		private UINavigator navigator;

		[Inject]
		private IAudioPlayer audioPlayer;

		[Inject]
		private IAnalyticsWrapper analytics;

		private ViewContainer<AmuletSoulCurrencyView, string> soulsViewContainer;

		private ViewContainer<AmuletGraphLevelButton, int> levelButtonsContainerView;

		private readonly Dictionary<string, AmuletUpgradeNodeView> spawnedNodes = new Dictionary<string, AmuletUpgradeNodeView>();

		private readonly Dictionary<(string parent, string child), BezierPathGraphic> spawnedPaths = new Dictionary<(string parent, string child), BezierPathGraphic>();

		private readonly Dictionary<string, Vector2> nodePositions = new Dictionary<string, Vector2>();

		private readonly Dictionary<string, int> nodeLevels = new Dictionary<string, int>();

		private AmuletUpgradeGraphLayoutResolver layoutResolver;

		private bool layoutBuilt;

		private int lastLayoutSeed = int.MinValue;

		private int lastVersion;

		private int selectedAmuletLevel = 1;

		private int pendingUnlockedLevel;

		private bool isZoomedOut;

		private Vector3 contentInitialScale;

		private const float ZoomOutScaleMultiplier = 0.5f;

		private const float ZoomOutNodeScaleMultiplier = 1.5f;

		private const float ZoomDuration = 0.5f;

		private const int AvailableNodeCountForZoomOut = 15;

		private const int FogRevealDepth = 2;

		private string currentTooltipUpgradeKey;

		private bool suppressScrollCleanup;

		private bool nodeClickedThisFrame;

		private float tooltipOpenTime;

		private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

		private bool IsTooltipSettling => Time.unscaledTime - tooltipOpenTime < 0.4f;

		protected override void Awake()
		{
			base.Awake();
			closeButton.onClick.AddListener(HandleClose);
			infoButton.onClick.AddListener(OpenTutorial);
			shopButton.onClick.AddListener(OpenShopGraphTab);
			zoomOutButton.onClick.AddListener(HandleZoomButton);
			scrollRect.onValueChanged.AddListener(HandleScrollChanged);
			contentInitialScale = contentRect.localScale;
			Injector.InjectInto(nodeTooltip);
		}

		protected override void Start()
		{
			base.Start();
			soulsViewContainer = new ViewContainer<AmuletSoulCurrencyView,string>(soulsContainer,soulCurrencyPrefab)
			    .BindTo(Lifetime,GetSoulsByRarity).Setup((view,key)=>view.Setup(key));
			levelButtonsContainerView = new ViewContainer<AmuletGraphLevelButton,int>(levelButtonsContainer,levelButtonPrefab)
			    .BindTo(Lifetime,()=>GetVisibleLevels()).Setup((view,level)=> {
			        view.Setup(level); view.SetSelected(level == selectedAmuletLevel); view.OnClicked += HandleLevelButtonClicked;
			    }).OnCleanup(view=>view.OnClicked -= HandleLevelButtonClicked);
		}

		public override void Initialize()
		{
			base.Initialize();
			ResetContentScale();
			bool hadLayout = layoutBuilt;
			EnsureLayoutBuilt();
			string focus = FindCheapestAvailableUpgradeKey();
			if (!string.IsNullOrEmpty(focus)) selectedAmuletLevel = config.GetUpgrade(focus).RequiredAmuletLevel;
			else if (!hadLayout) selectedAmuletLevel = PickDefaultAmuletLevel();
			SpawnVisibleElements(!hadLayout);
			lastVersion = upgradesModel.Version;
			UpdateLevelButtonSelection();
			UpdateLevelInfo();
			CenterScrollOnOpen(focus);
			audioPlayer.PlaySoundOnce("graph_open");
			analytics.Send(new GraphOpenedEvent());
		}

		private List<int> GetVisibleLevels()
		{
			return Enumerable.Range(1,Math.Min(amuletModel.Level + 1,levelsCount)).ToList();
		}

		private int PickDefaultAmuletLevel()
		{
			int max = Math.Min(amuletModel.Level + 1,levelsCount);
			for (int level=1;level<=max;level++) if (HasAffordableUpgradeOnLevel(level)) return level;
			return Math.Max(1,amuletModel.Level);
		}

		private bool HasAffordableUpgradeOnLevel(int amuletLevel)
		{
			foreach (var upgrade in config.Upgrades)
			    if (upgrade.RequiredAmuletLevel == amuletLevel && upgradesModel.IsAvailable(upgrade.Key) && upgradesModel.CanAfford(upgrade.Key)) return true;
			return false;
		}

		private void HandleClose()
		{
			audioPlayer.PlaySoundOnce("graph_close");
			Close();
		}

		private void OpenShopGraphTab()
		{
			navigator.Open<ShopView>("Assets/Prefabs/UI/Shop.prefab",false).OpenSoulGraphTab();
		}

		private void OpenTutorial()
		{
			navigator.Open<AmuletTutorialView>("Assets/Prefabs/UI/Amulet/AmuletTutorial.prefab",true);
		}

		private void HandleZoomButton()
		{
			audioPlayer.PlaySoundOnce("tap");
			CleanupTooltip();
			isZoomedOut = !isZoomedOut;
			UpdateZoomIcon();
			AnimateContentScale(isZoomedOut ? 0.5f : 1f);
			FocusOnLevelRoot(false,0.5f);
		}

		private void AnimateContentScale(float targetScaleMultiplier)
		{
			contentRect.DOKill();
			scrollRect.StopMovement();
			foreach (var node in spawnedNodes.Values) { node.transform.DOKill(); node.SetZoomedOut(isZoomedOut); }
			float scale = GetContentScaleMultiplier();
			DOTween.To(()=>scale,value=> {
			    scale=value; contentRect.localScale=contentInitialScale*value;
			    float nodeScale=GetNodeScaleMultiplier(value);
			    foreach(var node in spawnedNodes.Values) node.transform.localScale=Vector3.one*nodeScale;
			},targetScaleMultiplier,0.5f).SetTarget(contentRect).SetEase(Ease.OutQuad);
		}

		private float GetContentScaleMultiplier()
		{
			return contentRect.localScale.x / contentInitialScale.x;
		}

		private float GetNodeScaleMultiplier(float contentScaleMultiplier)
		{
			return Mathf.Max(0f,GetZoomProgress(contentScaleMultiplier)) * 0.5f + 1f;
		}

		private float GetZoomProgress(float contentScaleMultiplier)
		{
			float value=(contentScaleMultiplier-1f)*-2f;
			return value>=0f ? (value<=1f ? value : 1f) : 0f;
		}

		private void UpdateZoomOutButtonVisibility()
		{
			int count=config.Upgrades.Count(upgrade=>IsNodeVisible(upgrade.Key) && IsNodeKnown(upgrade.Key));
			zoomOutButton.gameObject.SetActive(count>=15);
			if(count<15 && isZoomedOut) { isZoomedOut=false; UpdateZoomIcon(); AnimateContentScale(1f); }
		}

		private void UpdateZoomIcon()
		{
			zoomIcon.sprite = isZoomedOut ? zoomInSprite : zoomOutSprite;
		}

		private void ResetContentScale()
		{
			contentRect.DOKill();
			contentRect.localScale = contentInitialScale;
			isZoomedOut = false;
			UpdateZoomIcon();
			foreach (var node in spawnedNodes.Values) {
			    node.transform.DOKill(); node.transform.localScale = Vector3.one; node.SetZoomedOut(false);
			}
		}

		private void HandleLevelButtonClicked(AmuletGraphLevelButton button)
		{
			SelectLevel(button.AmuletLevel);
		}

		public void SelectLevel(int amuletLevel)
		{
			if (selectedAmuletLevel == amuletLevel) return;
			selectedAmuletLevel=amuletLevel;
			CleanupTooltip();
			UpdateLevelButtonSelection();
			UpdateLevelInfo();
			SpawnVisibleElements(false);
			FocusOnLevelRoot(false,0.3f);
		}

		public void TryShowPendingLevelUnlock()
		{
			if (pendingUnlockedLevel < 1) return;
			int level=pendingUnlockedLevel;
			pendingUnlockedLevel=0;
			var view=navigator.Open<AmuletGraphLevelUnlockView>("Assets/Prefabs/UI/Amulet/AmuletGraphLevelUnlockView.prefab",true);
			if (view == null) { pendingUnlockedLevel=level; return; }
			view.Setup(level);
			view.SetUserCloseCallback(()=>SelectLevel(level));
		}

		private void UpdateLevelButtonSelection()
		{
			if(levelButtonsContainerView == null) return;
			foreach(var button in levelButtonsContainerView.Active) button.SetSelected(button.AmuletLevel == selectedAmuletLevel);
		}

		private void UpdateLevelInfo()
		{
			int current=0,max=0;
			foreach(var upgrade in config.Upgrades) {
			    if(upgrade.RequiredAmuletLevel != selectedAmuletLevel) continue;
			    max += upgradesModel.GetMaxLevel(upgrade.Key); current += upgradesModel.GetLevel(upgrade.Key);
			}
			levelProgressFill.fillAmount=max>0 ? (float)current/max : 0f;
			bool maxed=IsAmuletLevelFullyMaxed(selectedAmuletLevel);
			Color color=maxed ? LevelAccentMaxedColor : LevelAccentDefaultColor;
			levelProgressFill.color=color; levelIcon.color=color;
			levelText.text=LocalizationStrings.Get(maxed ? "AMULET_GRAPH_LEVEL_MAX" : "AMULET_GRAPH_LEVEL").Replace("{value}",selectedAmuletLevel.ToString());
		}

		private bool IsAmuletLevelFullyMaxed(int amuletLevel)
		{
			bool found=false;
			foreach(var upgrade in config.Upgrades) {
			    if(upgrade.RequiredAmuletLevel != amuletLevel) continue;
			    found=true; if(!upgradesModel.IsMaxLevel(upgrade.Key)) return false;
			}
			return found;
		}

		private static Color ParseHexColor(string hex)
		{
			ColorUtility.TryParseHtmlString(hex,out var color);
			return color;
		}

		private IEnumerable<string> GetSoulsByRarity()
		{
			var available=amuletModel.Souls;
			return SoulRarityOrder.Where(available.Contains);
		}

		private void CenterScrollOnOpen(string focusUpgradeKey)
		{
			if(!string.IsNullOrEmpty(focusUpgradeKey) && spawnedNodes.TryGetValue(focusUpgradeKey,out var node)) FocusOnNode(node,false,0f);
			else FocusOnLevelRoot(false,0f);
		}

		private void FocusOnNode(AmuletUpgradeNodeView node, bool showTooltip, float scrollDuration = 0.3f)
		{
			var upgrade=config.GetUpgrade(node.UpgradeKey);
			if(upgrade == null) return;
			CenterScrollOnNode(node,scrollDuration);
			if(showTooltip) SetupTooltip(node,upgrade);
		}

		private void FocusOnLevelRoot(bool showTooltip, float scrollDuration = 0.3f)
		{
			string key=FindLevelRootKey(selectedAmuletLevel);
			if(!string.IsNullOrEmpty(key) && spawnedNodes.TryGetValue(key,out var node)) FocusOnNode(node,showTooltip,scrollDuration);
		}

		private string FindCheapestAvailableUpgradeKey()
		{
			int max=Math.Min(amuletModel.Level+1,levelsCount);
			foreach(string soul in SoulRarityOrder) {
			    string best=null; double amount=double.PositiveInfinity;
			    foreach(var upgrade in config.Upgrades) {
			        if(upgrade.RequiredAmuletLevel<1 || upgrade.RequiredAmuletLevel>max || !upgradesModel.IsAvailable(upgrade.Key) || !IsNodeRevealed(upgrade)) continue;
			        if(upgradesModel.GetNextSoulKey(upgrade.Key) != soul) continue;
			        var cost=upgradesModel.GetNextSoulCost(upgrade.Key);
			        if(cost.HasValue && cost.Value.Amount<amount) { best=upgrade.Key; amount=cost.Value.Amount; }
			    }
			    if(best != null) return best;
			}
			return null;
		}

		private void CenterScrollOnLevelRoot()
		{
			FocusOnLevelRoot(false,0.3f);
		}

		private string FindLevelRootKey(int amuletLevel)
		{
			foreach(var upgrade in config.Upgrades) if(upgrade.RequiredAmuletLevel == amuletLevel && IsLevelRoot(upgrade,amuletLevel)) return upgrade.Key;
			return null;
		}

		private bool IsLevelRoot(AmuletUpgradeDef upgrade, int amuletLevel)
		{
			if(upgrade.RequiredUpgradeKeys == null || upgrade.RequiredUpgradeKeys.Count == 0) return true;
			foreach(string key in upgrade.RequiredUpgradeKeys) { var parent=config.GetUpgrade(key); if(parent != null && parent.RequiredAmuletLevel==amuletLevel) return false; }
			return true;
		}

		protected override void Render()
		{
			EnsureLayoutBuilt();
			int version=upgradesModel.Version;
			if(version == lastVersion) return;
			lastVersion=version;
			UpdateLevelInfo();
			bool hadTooltip=!string.IsNullOrEmpty(currentTooltipUpgradeKey);
			SpawnVisibleElements(false);
			if(hadTooltip) tooltipOpenTime=Time.unscaledTime;
		}

		private void EnsureLayoutBuilt()
		{
			bool hadLayout=layoutBuilt;
			if(hadLayout && lastLayoutSeed == config.Seed) return;
			BuildLayout(); layoutBuilt=true; lastLayoutSeed=config.Seed;
			if(hadLayout) ApplyLayoutToSpawnedElements();
		}

		private void BuildLayout()
		{
			if(layoutResolver == null) layoutResolver=new AmuletUpgradeGraphLayoutResolver(config);
			layoutResolver.Build(nodePositions,nodeLevels);
		}

		private void ApplyLayoutToSpawnedElements()
		{
			foreach(var entry in spawnedNodes) if(nodePositions.TryGetValue(entry.Key,out var position)) entry.Value.RectTransform.anchoredPosition=position;
			foreach(var entry in spawnedPaths)
			    if(nodePositions.TryGetValue(entry.Key.parent,out var parent) && nodePositions.TryGetValue(entry.Key.child,out var child))
			        entry.Value.SetPath(parent,child,GetDeterministicPathSeed(entry.Key.parent,entry.Key.child));
			UpdateContentSize();
		}

		private void SpawnVisibleElements(bool withDelay)
		{
			DespawnInvisibleElements();
			SpawnPaths(withDelay); SpawnNodes(withDelay); UpdateContentSize(); KeepTooltipOnTop(); UpdateZoomOutButtonVisibility();
		}

		private void KeepTooltipOnTop()
		{
			if(!string.IsNullOrEmpty(currentTooltipUpgradeKey)) nodeTooltip.transform.SetAsLastSibling();
		}

		private void DespawnInvisibleElements()
		{
			var removedNodes=new List<string>();
			foreach(var entry in spawnedNodes) {
			    if(IsNodeVisible(entry.Key)) continue;
			    entry.Value.OnClicked-=HandleNodeClicked; entry.Value.transform.DOKill(); UnityEngine.Object.Destroy(entry.Value.gameObject); removedNodes.Add(entry.Key);
			}
			foreach(string key in removedNodes) spawnedNodes.Remove(key);
			var removedPaths=new List<(string parent,string child)>();
			foreach(var entry in spawnedPaths) {
			    if(IsNodeVisible(entry.Key.parent) && IsNodeVisible(entry.Key.child)) continue;
			    entry.Value.DOKill(); UnityEngine.Object.Destroy(entry.Value.gameObject); removedPaths.Add(entry.Key);
			}
			foreach(var key in removedPaths) spawnedPaths.Remove(key);
		}

		private void SpawnPaths(bool withDelay)
		{
			foreach(var upgrade in config.Upgrades) {
			    if(upgrade.RequiredUpgradeKeys == null || !IsNodeVisible(upgrade.Key)) continue;
			    foreach(string parentKey in upgrade.RequiredUpgradeKeys) {
			        if(!IsNodeVisible(parentKey)) continue;
			        var key=(parent:parentKey,child:upgrade.Key);
			        Color target=new Color(1f,1f,1f,IsNodeKnown(upgrade.Key) ? 1f : 0.5f);
			        if(spawnedPaths.TryGetValue(key,out var existing)) {
			            existing.DOKill(); existing.DOColor(target,0.3f).SetLink(existing.gameObject); continue;
			        }
			        if(!nodePositions.TryGetValue(parentKey,out var parent) || !nodePositions.TryGetValue(upgrade.Key,out var child)) continue;
			        var path=UnityEngine.Object.Instantiate(pathPrefab,pathsContainer);
			        path.SetThickness(config.PathThickness); path.SetTileScale(config.PathTileScale);
			        path.SetPath(parent,child,GetDeterministicPathSeed(parentKey,upgrade.Key));
			        float delay=withDelay ? nodeLevels.GetValueOrDefault(upgrade.Key)*0.2f : 0f;
			        path.color=new Color(1f,1f,1f,0f);
			        path.DOColor(target,0.3f).SetDelay(delay).SetLink(path.gameObject);
			        spawnedPaths[key]=path;
			    }
			}
		}

		private int GetDeterministicPathSeed(string parentKey, string childKey)
		{
			return unchecked((config.Seed*31 + parentKey.GetHashCode())*31 + childKey.GetHashCode());
		}

		private void SpawnNodes(bool withDelay)
		{
			foreach(var upgrade in config.Upgrades) {
			    if(!IsNodeVisible(upgrade.Key) || spawnedNodes.ContainsKey(upgrade.Key) || !nodePositions.TryGetValue(upgrade.Key,out var position)) continue;
			    var node=UnityEngine.Object.Instantiate(nodePrefab,nodesContainer);
			    Injector.InjectInto(node); node.Setup(upgrade.Key,upgrade.Icon,position); node.OnClicked+=HandleNodeClicked;
			    float delay=withDelay ? nodeLevels.GetValueOrDefault(upgrade.Key)*0.2f : 0f;
			    float scale=GetNodeScaleMultiplier(GetContentScaleMultiplier()); node.SetZoomedOut(isZoomedOut);
			    node.transform.localScale=Vector3.zero;
			    node.transform.DOScale(Vector3.one*scale,0.3f).SetDelay(delay).SetEase(Ease.OutBack).SetLink(node.gameObject);
			    spawnedNodes[upgrade.Key]=node;
			}
		}

		private void UpdateContentSize()
		{
			if(contentRect == null) return;
			var points=config.Upgrades.Where(upgrade=>IsNodeVisible(upgrade.Key) && nodePositions.ContainsKey(upgrade.Key)).Select(upgrade=>nodePositions[upgrade.Key]).ToList();
			if(points.Count==0) return;
			float width=(Mathf.Max(Mathf.Abs(points.Min(p=>p.x)),Mathf.Abs(points.Max(p=>p.x)))+contentPadding)*2f;
			float height=(Mathf.Max(Mathf.Abs(points.Min(p=>p.y)),Mathf.Abs(points.Max(p=>p.y)))+contentPadding)*2f;
			contentRect.sizeDelta=new Vector2(width,height);
		}

		private bool IsNodeKnown(string upgradeKey)
		{
			if(upgradesModel.GetLevel(upgradeKey)>0) return true;
			var upgrade=config.GetUpgrade(upgradeKey); if(upgrade == null) return false;
			if(upgrade.RequiredUpgradeKeys == null || upgrade.RequiredUpgradeKeys.Count==0) return true;
			foreach(string key in upgrade.RequiredUpgradeKeys) if(upgradesModel.GetLevel(key)<1) return false;
			return true;
		}

		private bool IsNodeVisible(string upgradeKey)
		{
			var upgrade=config.GetUpgrade(upgradeKey);
			return upgrade != null && upgrade.RequiredAmuletLevel==selectedAmuletLevel && HasUnlockedAncestorWithin(upgrade,2);
		}

		private bool IsNodeRevealed(AmuletUpgradeDef upgrade)
		{
			return HasUnlockedAncestorWithin(upgrade,2);
		}

		private bool HasUnlockedAncestorWithin(AmuletUpgradeDef upgrade, int depth)
		{
			if(upgradesModel.GetLevel(upgrade.Key)>0) return true;
			if(depth<1) return false;
			if(upgrade.RequiredUpgradeKeys == null || upgrade.RequiredUpgradeKeys.Count==0) return true;
			foreach(string key in upgrade.RequiredUpgradeKeys) { var parent=config.GetUpgrade(key); if(parent != null && HasUnlockedAncestorWithin(parent,depth-1)) return true; }
			return false;
		}

		private void HandleNodeClicked(AmuletUpgradeNodeView node)
		{
			nodeClickedThisFrame=true;
			var upgrade=config.GetUpgrade(node.UpgradeKey); if(upgrade == null) return;
			if(nodeTooltip.gameObject.activeSelf && currentTooltipUpgradeKey == node.UpgradeKey) { CleanupTooltip(); return; }
			float duration=0.3f;
			if(contentRect.localScale != contentInitialScale) { isZoomedOut=false; UpdateZoomIcon(); AnimateContentScale(1f); duration=0.5f; }
			FocusOnNode(node,true,duration);
		}

		private void CenterScrollOnNode(AmuletUpgradeNodeView node, float scrollDuration = 0.3f)
		{
			if(scrollRect == null) return;
			var content=scrollRect.content; var viewport=scrollRect.viewport != null ? scrollRect.viewport : (RectTransform)scrollRect.transform;
			if(content == null || viewport == null) return;
			Canvas.ForceUpdateCanvases(); LayoutRebuilder.ForceRebuildLayoutImmediate(content);
			Vector2 contentSize=content.rect.size,viewportSize=viewport.rect.size;
			Vector2 position=GetNodeLocalPositionInContent(node,content,contentSize);
			var target=new Vector2(Mathf.Clamp01((position.x-viewportSize.x*0.5f)/Mathf.Max(0.0001f,contentSize.x-viewportSize.x)),Mathf.Clamp01((position.y-viewportSize.y*0.5f)/Mathf.Max(0.0001f,contentSize.y-viewportSize.y)));
			DOTween.Kill(scrollRect); suppressScrollCleanup=true;
			if(scrollDuration>0f) DOTween.To(()=>scrollRect.normalizedPosition,value=>scrollRect.normalizedPosition=value,target,scrollDuration).SetTarget(scrollRect).SetEase(Ease.OutQuad).OnComplete(()=>suppressScrollCleanup=false);
			else { scrollRect.normalizedPosition=target; suppressScrollCleanup=false; }
		}

		private Vector2 GetNodeLocalPositionInContent(AmuletUpgradeNodeView node, RectTransform content, Vector2 contentSize)
		{
			Vector2 position=nodePositions.TryGetValue(node.UpgradeKey,out var stored) ? stored : node.RectTransform.anchoredPosition;
			return new Vector2(position.x+contentSize.x*content.pivot.x,position.y+contentSize.y*content.pivot.y);
		}

		private void SetupTooltip(AmuletUpgradeNodeView node, AmuletUpgradeDef upgrade)
		{
			bool wasClosed=string.IsNullOrEmpty(currentTooltipUpgradeKey);
			nodeTooltip.OnUpgradeClicked-=HandleTooltipUpgrade; nodeTooltip.OnBuyCurrencyPopupClosed-=HandleBuyCurrencyPopupClosed;
			nodeTooltip.Cleanup(); currentTooltipUpgradeKey=upgrade.Key;
			nodeTooltip.Setup(upgrade.Key,upgrade.Icon);
			nodeTooltip.OnUpgradeClicked+=HandleTooltipUpgrade; nodeTooltip.OnBuyCurrencyPopupClosed+=HandleBuyCurrencyPopupClosed;
			PositionTooltipAtNode(node); nodeTooltip.transform.SetAsLastSibling(); nodeTooltip.Show();
			if(wasClosed) audioPlayer.PlaySoundOnce("node_open");
			tooltipOpenTime=Time.unscaledTime;
		}

		private void PositionTooltipAtNode(AmuletUpgradeNodeView node)
		{
			var rect=node.RectTransform;
			nodeTooltip.RectTransform.anchoredPosition=rect.anchoredPosition+new Vector2(0f,rect.rect.height);
		}

		private void HandleScrollChanged(Vector2 _)
		{
			if(!suppressScrollCleanup && !IsTooltipSettling && Input.GetMouseButton(0) && nodeTooltip.gameObject.activeSelf) CleanupTooltip();
		}

		private void HandleTooltipUpgrade()
		{
			if(string.IsNullOrEmpty(currentTooltipUpgradeKey)) return;
			int before=amuletModel.Level;
			if(upgradesModel.TryUpgrade(currentTooltipUpgradeKey)) analytics.Send(new GraphUpgradeEvent(currentTooltipUpgradeKey));
			if(amuletModel.Level>before) pendingUnlockedLevel=amuletModel.Level;
		}

		private void LateUpdate()
		{
			if(!nodeTooltip.gameObject.activeSelf || !Input.GetMouseButtonUp(0)) return;
			if(nodeClickedThisFrame) { nodeClickedThisFrame=false; return; }
			if(!IsTooltipSettling && navigator.ActivePopupCount<2 && !IsPointerOverTooltip()) CleanupTooltip();
		}

		private void HandleBuyCurrencyPopupClosed()
		{
			tooltipOpenTime=Time.unscaledTime;
		}

		private bool IsPointerOverTooltip()
		{
			var pointer=new PointerEventData(EventSystem.current) { position=Input.mousePosition };
			raycastResults.Clear(); EventSystem.current.RaycastAll(pointer,raycastResults);
			foreach(var hit in raycastResults) if(hit.gameObject == nodeTooltip.gameObject || hit.gameObject.transform.IsChildOf(nodeTooltip.transform)) return true;
			return false;
		}

		private void ClearGraph()
		{
			CleanupTooltip();
			foreach(var node in spawnedNodes.Values) { node.OnClicked-=HandleNodeClicked; node.transform.DOKill(); UnityEngine.Object.Destroy(node.gameObject); }
			foreach(var path in spawnedPaths.Values) { path.DOKill(); UnityEngine.Object.Destroy(path.gameObject); }
			spawnedNodes.Clear(); spawnedPaths.Clear(); nodePositions.Clear(); nodeLevels.Clear(); lastLayoutSeed=int.MinValue;
		}

		private void CleanupTooltip()
		{
			bool wasOpen=!string.IsNullOrEmpty(currentTooltipUpgradeKey);
			nodeTooltip.OnUpgradeClicked-=HandleTooltipUpgrade;
			nodeTooltip.OnBuyCurrencyPopupClosed-=HandleBuyCurrencyPopupClosed;
			nodeTooltip.Cleanup(); nodeTooltip.Hide(); currentTooltipUpgradeKey=null;
			if(wasOpen) audioPlayer.PlaySoundOnce("node_close");
		}

		public void FocusOnUpgrade(string upgradeKey)
		{
			if(string.IsNullOrEmpty(upgradeKey)) return;
			var upgrade=config.GetUpgrade(upgradeKey); if(upgrade == null) return;
			if(selectedAmuletLevel != upgrade.RequiredAmuletLevel) SelectLevel(upgrade.RequiredAmuletLevel);
			if(spawnedNodes.TryGetValue(upgradeKey,out var node)) FocusOnNode(node,true,0.3f);
		}

		public override void Cleanup()
		{
			base.Cleanup(); ResetContentScale(); pendingUnlockedLevel=0; ClearGraph(); layoutBuilt=false; lastLayoutSeed=int.MinValue;
		}
	}
}
