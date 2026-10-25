using Codebase.Utilities;
using BrunoMikoski.AnimationSequencer;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Player.Models;
using Codebase.UI.Map;
using TMPro;
using UnityEngine;

namespace Codebase.UI.IslandNotification
{
	public class IslandNotificationView : View
	{
		[SerializeField]
		private TMP_Text text;

		[SerializeField]
		private GameObject root;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private MiniMapView miniMapView;

		[SerializeField]
		private IslandNotificationMapInfoView weaknessInfo;

		[SerializeField]
		private IslandNotificationMapInfoView attackInfo;

		[SerializeField]
		private Color weaknessColor;

		[SerializeField]
		private Color attackColor;

		[SerializeField]
		private AnimationSequencerController animationSequencerController;

		protected override void Awake()
		{
			// Original ELF0x1648b64, Initialize virtual slot8.
			base.Awake();
			Initialize();
		}

		protected override void Render()
		{
			// Original ELF0x1648b88; native Sequencer slots17,22,8 are Complete/Reset/Play.
			var location = playerModel.CurrentLocation;
			root.SetActive(!miniMapView.IsZoomed);
			if (miniMapView.IsZoomed) return;
			animationSequencerController.Complete(true);
			animationSequencerController.ClearPlayingSequence();
			animationSequencerController.ResetToInitialState();
			animationSequencerController.Play();
			text.text = string.Format("{0} - {1}", LocalizationStrings.Get(string.Format("ISLAND_NAME_{0}", location.Number)), location.Number);
			weaknessInfo.gameObject.SetActive(location.Number > 1 && location.Number != 10);
			weaknessInfo.Setup(location.PreferredArmorType, "ISLAND_WEAKNESS", weaknessColor);
			attackInfo.Setup(location.PreferredWeaponType, "ISLAND_ATTACK", attackColor);
		}
	}
}
