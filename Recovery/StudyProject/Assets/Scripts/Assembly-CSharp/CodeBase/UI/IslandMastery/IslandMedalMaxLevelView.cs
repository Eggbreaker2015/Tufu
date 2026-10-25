using Codebase.Utilities;
using DG.Tweening;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CodeBase.Audio;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Location;
using Codebase.Player.Models;
using Codebase.UI.World;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using TMPro;
using UnityEngine;

namespace Codebase.UI.IslandMastery
{
	public class IslandMedalMaxLevelView : View
	{

		[SerializeField]
		private IslandMedalTierView medalTier;

		[SerializeField]
		private TMP_Text islandText;

		[Inject]
		private IslandMasteryModel islandMasteryModel;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private IAudioPlayer audioPlayer;

		[Inject]
		private WeaponRewardView weaponRewardView;

		private CanvasGroup visibilityCanvasGroup;

		private GameObject[] contentRoots;

		private int observedClearVersion;

		private bool isVisible;

		private bool isMapVisible = true;

		private const float FadeDuration = 0.25f;

		private const float FadeInDelay = 0.5f;

		public bool IsVisible => this.isVisible;

		protected override void Awake()
		{
			base.Awake();
			visibilityCanvasGroup = GetComponent<CanvasGroup>();
			contentRoots = new GameObject[transform.childCount];
			for (int i = 0; i < transform.childCount; i++) contentRoots[i] = transform.GetChild(i).gameObject;
			SetVisible(false);
		}

		public void SetMapVisible(bool value)
		{
			if (isMapVisible == value) return;
			isMapVisible = value;
			visibilityCanvasGroup.DOKill();
			visibilityCanvasGroup.DOFade(value ? 1f : 0f, .25f).SetDelay(value ? .5f : 0f).SetEase(Ease.OutQuad);
		}

		public void Setup()
		{
			Injector.InjectInto(medalTier);
			observedClearVersion = islandMasteryModel.MaxLevelClearVersion;
			SetVisible(false);
			Initialize();
		}

		protected override void Render()
		{
			_ = islandMasteryModel.StateVersion;
			int location = playerModel.CurrentLocation.Number;
			int clearLocation = islandMasteryModel.MaxLevelClearLocationNumber;
			int clearVersion = islandMasteryModel.MaxLevelClearVersion;
			int level = islandMasteryModel.GetMedalLevel(location);
			bool full = islandMasteryModel.IsFullyCleared(location);
			bool show = location == clearLocation && level == 5 && full;
			if (isVisible && !show) SetVisible(false);
			if (observedClearVersion == clearVersion) return;
			observedClearVersion = clearVersion;
			if (show) ShowWhenWeaponRewardFinished(clearVersion, location).Forget();
			else SetVisible(false);
		}

		public void PlayFullClearSound()
		{
			audioPlayer.PlaySoundOnce("features_pop_up");
		}

		private async UniTask ShowWhenWeaponRewardFinished(int clearVersion, int clearedLocationNumber)
		{
			if (weaponRewardView.IsVisible)
			{
			    bool cancelled = await UniTask.WaitUntil(() => !weaponRewardView.IsVisible, PlayerLoopTiming.Update, destroyCancellationToken).SuppressCancellationThrow();
			    if (cancelled) return;
			}
			int location = playerModel.CurrentLocation.Number;
			if (islandMasteryModel.MaxLevelClearVersion != clearVersion || location != clearedLocationNumber) return;
			if (islandMasteryModel.GetMedalLevel(location) != 5 || !islandMasteryModel.IsFullyCleared(location)) return;
			medalTier.SetupForLocation(clearedLocationNumber);
			islandText.text = string.Format("{0} - {1}", LocalizationStrings.Get(string.Format("ISLAND_NAME_{0}", clearedLocationNumber)), clearedLocationNumber);
			SetVisible(true);
		}

		private void SetVisible(bool value)
		{
			isVisible = value;
			foreach (GameObject root in contentRoots) root.SetActive(value);
		}
	}
}
