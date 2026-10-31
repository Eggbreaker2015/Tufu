using System;
using Codebase.Utilities;
using System.Collections.Generic;
using BrunoMikoski.AnimationSequencer;
using Codebase.Camera;
using Codebase.Core;
using Codebase.Location;
using Codebase.Map.Components;
using Codebase.Map.Systems;
using Codebase.Player;
using Codebase.Player.Models;
using Codebase.Quests.Systems;
using Codebase.UI.IslandMastery;
using Scellecs.Morpeh;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Map
{
	public class MiniMapView : MonoBehaviour
	{
		private sealed class MiniMapIconPriorityComparer : IComparer<MiniMapIcon>
		{
			public int Compare(MiniMapIcon left, MiniMapIcon right)
			{
			return right.PointOfInterest.Priority.CompareTo(left.PointOfInterest.Priority);
		}
		}

		[SerializeField]
		private Image miniMapImage;

		[SerializeField]
		private RectTransform staticIconsContainer;

		[SerializeField]
		private RectTransform dynamicIconsContainer;

		[SerializeField]
		private MiniMapIcon iconPrefab;

		[SerializeField]
		private MiniMapPlayerIcon playerIcon;

		[SerializeField]
		private Vector2 mapOffset;

		[SerializeField]
		private Vector2 worldCenter;

		[SerializeField]
		private float worldToMapMultiplier = 1f;

		[SerializeField]
		private float dragBoundsMultiplier = .33f;

		[SerializeField]
		private Button zoomButton;

		[SerializeField]
		private AnimationSequencerController zoomAnimation;

		[SerializeField]
		private MiniMapDragHandler dragHandler;

		[SerializeField]
		private Image zoomImage;

		[SerializeField]
		private Sprite zoomInSprite;

		[SerializeField]
		private Sprite zoomOutSprite;

		[SerializeField]
		private Image enemyWeaknessImage;

		[SerializeField]
		private Image enemyWeaponImage;

		[SerializeField]
		private IslandMedalTierView medalTierView;

		[SerializeField]
		private RectTransform fogRoot;

		[SerializeField]
		private Image[] fogs;

		[SerializeField]
		private AnimationSequencerController[] legends;

		private RectTransform mapRectTransform;

		private Vector2 initialAnchoredPosition;

		private MiniMapIcon[] iconsCache = Array.Empty<MiniMapIcon>();

		private MiniMapIcon[] iconsSortingBuffer = Array.Empty<MiniMapIcon>();

		private Vector2 dragOffset;

		private Vector2 dragOffsetTarget;

		private float recenterProgress;

		private bool isRecentering;

		private bool wasDraggedAfterZoom;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private StaticMiniMapPoiSystem poiSystem;

		[Inject]
		private QuestMiniMapPoiSystem questMiniMapPoiSystem;

		[Inject]
		private DynamicMinimapPoiSystem dynamicPoiSystem;

		[Inject]
		private IconStyleConfig iconStyle;

		[Inject]
		private IslandMasteryModel islandMasteryModel;

		[Inject]
		private CameraFollowSystem cameraFollowSystem;

		private bool initialized;

		private Stack<MiniMapIcon> dynamicIconsPool;

		private Dictionary<Entity, MiniMapIcon> activeDynamicIcons;

		private List<Entity> entitiesToRemoveBuffer;

		private int lastDynamicPoiVersion;

		private const int DynamicPoolInitialCapacity = 32;

		private static readonly MiniMapIconPriorityComparer iconPriorityComparer = new MiniMapIconPriorityComparer();

		private int lastStaticPoiVersion = -1;

		private int lastOpenedLocation = -1;

		private int currentLocation = -1;

		private int medalLocation = -1;

		private int medalStateVersion = -1;

		private IslandMedalMaxLevelView maxLevelMedalView;

		public bool IsZoomed { get; private set; }

		private Transform player => global::Codebase.Rpg.RpgControl.ControlledUnit(playerModel) != null ? global::Codebase.Rpg.RpgControl.ControlledUnit(playerModel).transform : null;

		private void Awake()
		{
            global::Codebase.Core.GameContext.Set(this);
            mapRectTransform = miniMapImage.rectTransform;
            initialAnchoredPosition = mapRectTransform.anchoredPosition;
            SetupDragHandler();
            SetupZoomButton();
        }

		private void Start()
		{
			maxLevelMedalView = GetComponentInChildren<IslandMedalMaxLevelView>(true);
			Injector.InjectInto(maxLevelMedalView);
			maxLevelMedalView.Setup();
			Injector.InjectInto(medalTierView);
			medalTierView.gameObject.SetActive(true);
			UpdateIslandMedal();
			for (int i = 0; i < legends.Length; i++) legends[i].GetComponent<LocalizedText>().SetKey("ISLAND_NAME_" + (i + 1));
		}

		private void SetupDragHandler()
		{
			if (dragHandler == null) return;
			dragHandler.OnDragDelta += HandleDragDelta;
		}

		private void HandleDragDelta(Vector2 delta)
		{
			if (!wasDraggedAfterZoom)
			{
			    wasDraggedAfterZoom = true;
			    dragOffset = -GetPlayerMapOffset();
			}
			dragOffset += delta;
			ClampDragOffset();
		}

		private void ClampDragOffset()
		{
			Vector2 bounds = mapRectTransform.rect.size * dragBoundsMultiplier;
			float x = dragOffset.x > bounds.x ? bounds.x : dragOffset.x;
			float y = dragOffset.y > bounds.y ? bounds.y : dragOffset.y;
			if (dragOffset.x < -bounds.x) x = -bounds.x;
			if (dragOffset.y < -bounds.y) y = -bounds.y;
			dragOffset = new Vector2(x, y);
		}

		private void SetupZoomButton()
		{
			zoomButton.onClick.AddListener(() =>
			{
			    if (zoomAnimation.IsPlaying) return;
			    if (!IsZoomed)
			    {
			        zoomAnimation.Play();
			        SetDragEnabled(true);
			    }
			    else
			    {
			        zoomAnimation.PlayBackwards();
			        SetDragEnabled(false);
			        if (wasDraggedAfterZoom) StartRecentering();
			        wasDraggedAfterZoom = false;
			    }
			    IsZoomed = !IsZoomed;
			    zoomImage.sprite = IsZoomed ? zoomOutSprite : zoomInSprite;
			});
		}

		private void StartRecentering()
		{
			dragOffsetTarget = -GetPlayerMapOffset();
			recenterProgress = 0f;
			isRecentering = true;
		}

		private Vector2 GetPlayerMapOffset()
		{
			return ConvertWorldToMapPosition(cameraFollowSystem.PositionOfAttack);
		}

		private void SetDragEnabled(bool enabled)
		{
			if (dragHandler != null) dragHandler.IsDragEnabled = enabled;
		}

		private void LateUpdate()
		{
			UpdateIslandMedal();
			if (mapRectTransform == null || player == null) return;
			if (poiSystem.PointsOfInterest == null) return;
			if (!initialized)
			{
			    InitializeDynamicIconsPool();
			    initialized = true;
			}
			if (IsZoomed) poiSystem.IncludeOnlyNearPlayer = false;
			else if (!zoomAnimation.IsPlaying) poiSystem.IncludeOnlyNearPlayer = true;
			UpdateStaticIcons();
			UpdateDynamicIcons();
			UpdateRecenter();
			UpdateMiniMapPosition();
			UpdatePlayerIconPosition();
			UpdateIconsVisuals();
			UpdateFog();
			UpdateLegend();
			var weaponType = playerModel.CurrentLocation.PreferredWeaponType;
			var armorType = playerModel.CurrentLocation.PreferredArmorType;
			enemyWeaponImage.sprite = iconStyle.ResistStyles.FirstOrDefaultNonAlloc((style, type) => style.WeaponType == type, weaponType).Ineffective.High;
			enemyWeaknessImage.sprite = iconStyle.ResistStyles.FirstOrDefaultNonAlloc((style, type) => style.WeaponType == type, armorType).Effective.Medium;
			enemyWeaknessImage.enabled = playerModel.CurrentLocation.Number != 1 && playerModel.CurrentLocation.Number != 10;
		}

		private void UpdateIslandMedal()
		{
			{ medalTierView.SetVisible(false); maxLevelMedalView.SetMapVisible(false); return; }
		}

		private void UpdateLegend()
		{
			if (currentLocation == playerModel.CurrentLocation.Number) return;
			currentLocation = playerModel.CurrentLocation.Number;
			if (!IsZoomed) return;
			var legend = legends[currentLocation - 1];
			legend.Complete(true);
			legend.ClearPlayingSequence();
			legend.ResetToInitialState();
			legend.Play();
		}

		private void UpdateRecenter()
		{
			if (!isRecentering) return;
			float dt = Time.deltaTime;
			recenterProgress = recenterProgress + (dt + dt);
			if (recenterProgress >= 1f)
			{
			    isRecentering = false;
			    dragOffset = dragOffsetTarget;
			}
			else dragOffset = Vector2.Lerp(dragOffset, dragOffsetTarget, EaseOutQuad(recenterProgress));
		}

		private float EaseOutQuad(float t)
		{
			return 1f - (1f - t) * (1f - t);
		}

		private void UpdatePlayerIconPosition()
		{
			if (playerIcon == null) return;
			playerIcon.RectTransform.anchoredPosition = ConvertWorldToMapPosition(player.position);
			playerIcon.RectTransform.localRotation = Quaternion.Euler(0f, 0f, 180f - player.eulerAngles.y);
		}

		private void UpdateFog()
		{
			if (lastOpenedLocation == playerModel.LastUnlockedLocation.Number) return;
			lastOpenedLocation = playerModel.LastUnlockedLocation.Number;
			for (int i = 0; i < fogs.Length; i++) fogs[i].enabled = lastOpenedLocation <= i;
		}

		private void UpdateMiniMapPosition()
		{
			mapRectTransform.anchoredPosition = !isRecentering && (!IsZoomed || !wasDraggedAfterZoom)
			    ? initialAnchoredPosition - GetPlayerMapOffset()
			    : initialAnchoredPosition + dragOffset;
		}

		private void UpdateStaticIcons()
		{
			int version = GetCombinedStaticPoiVersion();
			if (version == lastStaticPoiVersion) return;
			lastStaticPoiVersion = version;
			int count = GetCombinedStaticPoiCount();
			int oldLength = iconsCache.Length;
			if (oldLength < count)
			{
			    Array.Resize(ref iconsCache, count);
			    Array.Resize(ref iconsSortingBuffer, count);
			    for (int i = oldLength; i < count; i++)
			    {
			        iconsCache[i] = InstantiateIcon(staticIconsContainer);
			        Injector.InjectInto(iconsCache[i]);
			    }
			}
			for (int i = 0; i < count; i++)
			{
			    MiniMapIcon icon = iconsCache[i];
			    icon.SetVisible(true);
			    PointOfInterest poi = GetCombinedStaticPoiAt(i);
			    icon.RectTransform.anchoredPosition = ConvertWorldToMapPosition(poi.Component.WorldPosition);
			    if (icon.PointOfInterest.Reference != poi.Component.Reference) icon.ForceSetup(poi.Component);
			}
			for (int i = count; i < oldLength; i++) iconsCache[i].SetVisible(false);
			Array.Copy(iconsCache, iconsSortingBuffer, count);
			Array.Sort(iconsSortingBuffer, 0, count, iconPriorityComparer);
			for (int i = 0; i < count; i++) iconsSortingBuffer[i].RectTransform.SetAsFirstSibling();
			fogRoot.SetAsFirstSibling();
		}

		private void UpdateIconsVisuals()
		{
			int count = GetCombinedStaticPoiCount();
			for (int i = 0; i < count; i++)
			{
			    PointOfInterest poi = GetCombinedStaticPoiAt(i);
			    iconsCache[i].Setup(ref poi.Component);
			}
		}

		private int GetCombinedStaticPoiVersion()
		{
			return unchecked(poiSystem.PointsOfInterestVersion + questMiniMapPoiSystem.PointsOfInterestVersion * 1000);
		}

		private int GetCombinedStaticPoiCount()
		{
			return poiSystem.PointsOfInterest.length + questMiniMapPoiSystem.PointsOfInterest.length;
		}

		private PointOfInterest GetCombinedStaticPoiAt(int index)
		{
			int count = poiSystem.PointsOfInterest.length;
			return index < count ? poiSystem.PointsOfInterest.data[index] : questMiniMapPoiSystem.PointsOfInterest.data[index - count];
		}

		private Vector2 ConvertWorldToMapPosition(Vector3 worldPosition)
		{
			return mapOffset + (new Vector2(worldPosition.x, worldPosition.z) - worldCenter) * worldToMapMultiplier;
		}

		private MiniMapIcon InstantiateIcon(RectTransform parent)
		{
			MiniMapIcon icon = Instantiate(iconPrefab, parent);
			icon.SetVisible(false);
			return icon;
		}

		private void InitializeDynamicIconsPool()
		{
			dynamicIconsPool = new Stack<MiniMapIcon>(32);
			activeDynamicIcons = new Dictionary<Entity, MiniMapIcon>(32);
			entitiesToRemoveBuffer = new List<Entity>(32);
			lastDynamicPoiVersion = -1;
			for (int i = 0; i < 32; i++) dynamicIconsPool.Push(InstantiateIcon(dynamicIconsContainer));
		}

		private void UpdateDynamicIcons()
		{
			var pois = dynamicPoiSystem.ActivePois;
			if (pois == null) return;
			int version = dynamicPoiSystem.Version;
			if (version != lastDynamicPoiVersion)
			{
			    SyncDynamicIcons(pois);
			    lastDynamicPoiVersion = version;
			}
			UpdateDynamicIconPositions(pois);
		}

		private void SyncDynamicIcons(Dictionary<Entity, DynamicPoiData> pois)
		{
			foreach (var pair in pois)
			    if (!activeDynamicIcons.ContainsKey(pair.Key)) SpawnDynamicIcon(pair.Key, pair.Value);
			entitiesToRemoveBuffer.Clear();
			foreach (var pair in activeDynamicIcons)
			    if (!pois.ContainsKey(pair.Key)) entitiesToRemoveBuffer.Add(pair.Key);
			foreach (Entity entity in entitiesToRemoveBuffer) DespawnDynamicIcon(entity);
		}

		private void SpawnDynamicIcon(Entity entity, DynamicPoiData data)
		{
			MiniMapIcon icon = dynamicIconsPool.Count > 0 ? dynamicIconsPool.Pop() : InstantiateIcon(dynamicIconsContainer);
			icon.SetVisible(true);
			icon.RectTransform.anchoredPosition = ConvertWorldToMapPosition(data.WorldPosition);
			icon.ForceSetup(new PointOfInterestComponent { Sprite = data.Sprite, Color = data.Color, Size = data.Size, IsDirty = true, Priority = 3 });
			activeDynamicIcons[entity] = icon;
		}

		private void DespawnDynamicIcon(Entity entity)
		{
			if (!activeDynamicIcons.TryGetValue(entity, out MiniMapIcon icon)) return;
			icon.SetVisible(false);
			dynamicIconsPool.Push(icon);
			activeDynamicIcons.Remove(entity);
		}

		private void UpdateDynamicIconPositions(Dictionary<Entity, DynamicPoiData> pois)
		{
			foreach (var pair in pois)
			    if (activeDynamicIcons.TryGetValue(pair.Key, out MiniMapIcon icon)) icon.RectTransform.anchoredPosition = ConvertWorldToMapPosition(pair.Value.WorldPosition);
		}
	}
}
