using Codebase.Core;
using Codebase.Player.Models;
using Codebase.Player.Systems;
using Codebase.Services.Time;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using Codebase.Localization;

namespace Codebase.UI.Settings
{
	public class SettingUnstackButton : MonoBehaviour
	{
		[Inject]
		private PlayerModel playerModel;
		[Inject]
		private UnstackSystem unstackSystem;
		[Inject]
		private ITimeService timeService;
		[SerializeField]
		private Button button;
		[SerializeField]
		private TMP_Text cooldownText;
		[SerializeField]
		private GameObject panel;
		private const long CooldownSeconds = 600L;
		private void Awake()
		{
			button.onClick.AddListener(OnClick);
		}

		private void Update()
		{
			long remaining = GetRemainingSeconds();
			if (remaining > 0L)
			{
				button.interactable = false;
				cooldownText.text = string.Format("{0:00}:{1:00}", remaining / 60L, remaining % 60L);
			}
			else
			{
				button.interactable = true;
				cooldownText.text = LocalizationService.Get("BTN_UNSTACK");
			}
		}

		private void OnClick()
		{
			if (GetRemainingSeconds() > 0L)
				return;
			playerModel.UnstackCooldownTimestamp = unchecked(GetNowSeconds() + CooldownSeconds);
			unstackSystem.RequestUnstack();
			panel.SetActive(false);
		}

		private long GetRemainingSeconds()
		{
			return Math.Max(0L, unchecked(playerModel.UnstackCooldownTimestamp - GetNowSeconds()));
		}

		private long GetNowSeconds()
		{
			return new DateTimeOffset(timeService.Now.ToDateTime().ToUniversalTime()).ToUnixTimeSeconds();
		}
	}
}
