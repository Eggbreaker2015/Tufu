using System;
using System.Collections.Generic;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Enemy;
using Codebase.UI.World;
using Codebase.Utilities;
using TMPro;
using UniMob;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI
{
	public class SpawnerView : LifetimeMonoBehaviour
	{
		[SerializeField]
		private GameObject root;
		[SerializeField]
		private TMP_Text titleField;
		[SerializeField]
		private TMP_Text cooldownField;
		[SerializeField]
		private Image progress;
		[Header("Charges")]
		[SerializeField]
		private Transform chargesContainer;
		[SerializeField]
		private ChargeView chargePrefab;
		[SerializeField]
		private Color chargeNormalColor = Color.white;
		[SerializeField]
		private Color chargeBonusColor = Color.yellow;
		[SerializeField]
		private Color chargeInactiveColor = Color.gray;
		[SerializeField]
		private int maxVisibleCharges = 8;
		private EnemySpawner spawner;
		private Transform spawnerTransform;
		private float timer;
		private ViewContainer<ChargeView, Color> chargesViewContainer;

		public void Initialize(EnemySpawner spawner)
		{
			// Native ELF VA 0x15fb65c; original serialized field names are retained.
			this.spawner = spawner;
			spawnerTransform = spawner.transform;
			if (spawner.ChargesTotal < 2)
				chargesContainer.gameObject.SetActive(false);
			else
			{
				chargesContainer.gameObject.SetActive(true);
				if (chargesViewContainer == null)
					chargesViewContainer = new ViewContainer<ChargeView, Color>(chargesContainer, chargePrefab);
				chargesViewContainer.BindTo(Lifetime, GetChargeColors)
					.WithEqualityCheck((first, second) => first == second)
					.Setup((view, color) => view.SetColor(color));
			}
			Render();
		}

		private IEnumerable<Color> GetChargeColors()
		{
			// Native iterator MoveNext ELF VA 0x15fc060: inactive, normal, then bonus.
			int left = spawner.ChargesLeft;
			int baseCharges = spawner.ChargesBase;
			int bonus = Math.Max(0, left - baseCharges);
			int bonusToShow = Math.Min(bonus, maxVisibleCharges);
			int normal = left - bonus;
			int normalToShow = Math.Min(normal, maxVisibleCharges - bonusToShow);
			int inactiveToShow = Math.Min(baseCharges - normal, maxVisibleCharges - bonusToShow - normalToShow);
			for (int index = 0; index < inactiveToShow; index++) yield return chargeInactiveColor;
			for (int index = 0; index < normalToShow; index++) yield return chargeNormalColor;
			for (int index = 0; index < bonusToShow; index++) yield return chargeBonusColor;
		}

		private void LateUpdate()
		{
			// Native ELF VA 0x15fbc2c.
			if (spawner.Cooldown <= 0f || spawner.AnyoneAlive ||
				(spawner.Config.DisableAfterFirstClear && spawner.HasBeenClearedOnce))
			{
				Hide();
				return;
			}
			Show();
			timer -= Time.deltaTime;
			if (timer > 0f) return;
			timer = 0.5f;
			Render();
		}

		private void Render()
		{
			// Native ELF VA 0x15fb9b4; FRINTP/FCVTPS implements Ceiling, not truncation.
			titleField.text = spawner.name;
			int seconds = (int)Math.Ceiling(spawner.Cooldown);
			cooldownField.text = seconds > 0 ? TimeLocalizer.GetFormatTime(seconds, true) : string.Empty;
			float fullCooldown = spawner.IsBigCooldownActive ? spawner.BigCooldownSeconds : spawner.BaseCooldown;
			progress.fillAmount = fullCooldown > 0f ? Mathf.Clamp01(spawner.Cooldown / fullCooldown) : 0f;
			chargesViewContainer?.Refresh();
		}

		private void Show()
		{
			// Native ELF VA 0x15fbd14.
			if (root.activeSelf) return;
			root.SetActive(true);
			Render();
		}

		private void Hide()
		{
			// Native ELF VA 0x15fbcd4.
			if (root.activeSelf) root.SetActive(false);
		}

		public void Dispose()
		{
			// Native ELF VA 0x15fbd5c has the Unity destroyed-object guard before canvas access.
			if (this == null) return;
			DynamicWorldCanvas worldCanvas = GameContext.Get<DynamicWorldCanvas>();
			worldCanvas.RemoveTarget(spawnerTransform);
			worldCanvas.Despawn(this);
		}
	}
}
