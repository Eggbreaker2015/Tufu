using Codebase.Utilities;
using Codebase.Analytics.Events;
using Codebase.BossTower.Commands;
using System;
using Codebase.Analytics;
using Codebase.BossTower;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.BossTower
{
	public class BossTowerHubView : PopupView
	{
		[SerializeField]
		private Button closeButton;

		[SerializeField]
		private Transform bossesContainer;

		[SerializeField]
		private BossTowerBossItemView bossPrefab;

		[SerializeField]
		private TextMeshProUGUI timerTxt;

		[SerializeField]
		private TextMeshProUGUI ticketsCurrencyTxt;

		[SerializeField]
		private TextMeshProUGUI bossWeaknessMessage;

		[SerializeField]
		private Button infoBtn;

		[Inject]
		private BossTowerModel bossTowerModel;

		[Inject]
		private MetaStatsModel metaStatsModel;

		[Inject]
		private UINavigator navigator;

		[Inject]
		private IAnalyticsWrapper analytics;

		private ViewContainer<BossTowerBossItemView, BossModel> bossesViewContainer;

		private Action onClose;

		private MetaStat weaknessStat;

		protected override void Awake()
		{
			// Native ELF VA 0x13c73a0; close button deliberately does not invoke external callback.
			base.Awake();
			closeButton.onClick.AddListener(Close);
			infoBtn.onClick.AddListener(OpenInfo);
		}

		private void OpenInfo()
		{
			// Native ELF VA 0x13c7494.
			navigator.Open<BossTutorialView>("Assets/Prefabs/UI/BossTower/BossTutorialView.prefab").Setup(false);
		}

		public void Setup(Action onClose)
		{
			// Native ELF VA 0x13c750c.
			this.onClose = onClose;
			weaknessStat = metaStatsModel.Get("tower_boss_weakness");
			App.ExecuteCommand(new RebakeBossTowerRewardsCommand());
			analytics.Send(new BossTowerOpenedEvent());
		}

		protected override void Start()
		{
			// Native ELF VA 0x13c7694; callbacks 0x13c7a08/0x13c7a20.
			base.Start();
			bossesViewContainer = new ViewContainer<BossTowerBossItemView, BossModel>(bossesContainer, bossPrefab)
			    .BindTo(Lifetime, () => bossTowerModel.Bosses).Setup((view, model) => view.Setup(model, CloseExternal));
		}

		protected override void Render()
		{
			// Native ELF VA 0x13c782c; exact original unclosed size/color markup.
			ticketsCurrencyTxt.text = string.Format("{0}<size=90%><color=#A9A5A3>/{1}", bossTowerModel.Tickets, bossTowerModel.MaxTickets);
			timerTxt.text = TimeLocalizer.GetFormatTime((float)bossTowerModel.TimeToReset.TotalSeconds, true);
			bossWeaknessMessage.gameObject.SetActive(weaknessStat.Value > 0.0);
		}

		private void CloseExternal()
		{
			// Native ELF VA 0x13c79cc.
			Close();
			onClose?.Invoke();
		}
	}
}
