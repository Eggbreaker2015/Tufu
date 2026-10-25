using BrunoMikoski.AnimationSequencer;
using Codebase.BossTower;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Player.Models;
using TMPro;
using UniMob;
using UnityEngine;
using UnityEngine.UI;
using Codebase.UI.BossTower;

namespace Codebase.UI.World
{
	public class PortalWorldView : LifetimeMonoBehaviour
	{
		[SerializeField]
		private GameObject progressRoot;
		[SerializeField]
		private TMP_Text progressText;
		[SerializeField]
		private GameObject buttonRoot;
		[SerializeField]
		private Button openBossTowerButton;
		[SerializeField]
		private AnimationSequencerController hasTicketsAnim;
		[SerializeField]
		private TextMeshProUGUI ticketsTxt;
		[Inject]
		private PlayerModel playerModel;
		[Inject]
		private BossTowerModel bossTowerModel;
		[Inject]
		private UINavigator navigator;
		private int totalGems;
		private void Awake()
		{
			openBossTowerButton.onClick.AddListener(OpenBossTower);
		}

		public void Initialize(int totalGems)
		{
			this.totalGems = totalGems;
			Atom.Reaction(Lifetime, Render);
		}

		private void Render()
		{
			int collected = playerModel.PortalGemsCollected;
			progressRoot.SetActive(collected < totalGems);
			if (collected < totalGems)
			{
				buttonRoot.SetActive(false);
				progressText.text = string.Format("{0}/{1}", collected, totalGems);
			}
			else
				buttonRoot.SetActive(playerModel.IsUnlockAnimationShown);
			ticketsTxt.text = string.Format("{0}/{1}", bossTowerModel.Tickets, bossTowerModel.MaxTickets);
		}

		private void Update()
		{
			if (bossTowerModel == null)
				return;
			bool hasTickets = bossTowerModel.HasEnoughTickets(1);
			if (hasTickets == hasTicketsAnim.IsPlaying)
				return;
			if (hasTickets)
				hasTicketsAnim.Play();
			else
			{
				hasTicketsAnim.Pause();
				hasTicketsAnim.ResetToInitialState();
			}
		}

		private void OpenBossTower()
		{
			if (!playerModel.Data.SeenBossTutorial.Value)
				navigator.Open<BossTutorialView>("Assets/Prefabs/UI/BossTower/BossTutorialView.prefab").Setup(true);
			else
				navigator.Open<BossTowerHubView>("Assets/Prefabs/UI/BossTower/BossTowerHub.prefab").Setup(null);
		}
	}
}
