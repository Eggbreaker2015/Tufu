using Codebase.Unit.Events;
using Codebase.Utilities;
using TMPro;
using UnityEngine;

namespace Codebase.UI.World
{
	// Bodies not recovered (empty stubs); pet mode: implemented as a rising, fading number over a world point.
	public class UnitDamageView : MonoBehaviour
	{
		private const float DefaultTime = 1f;

		private const float FadeTime = 0.3f;

		private const float RiseSpeed = 1.5f;

		[SerializeField]
		private CanvasGroup canvasGroup;

		[SerializeField]
		private TMP_Text field;

		[SerializeField]
		private Color playerColor;

		[SerializeField]
		private Color inefficientColor;

		private float timer;

		private float duration;

		private DynamicWorldCanvas canvas;

		private Vector3 worldPosition;

		public void Setup(string reward, float time = 1f)
		{
			Show(reward, Color.white, time);
		}

		public void Setup(WeaponHitEvent data, double value, double resist)
		{
			string text = value > 0.0 ? "-" + value.Format() : LocalizationStrings.Get("DAMAGE_EVENT_MISS");
			Show(text, resist > 0.1 ? inefficientColor : playerColor, DefaultTime);
		}

		public void Place(DynamicWorldCanvas owner, Vector3 position)
		{
			canvas = owner;
			worldPosition = position;
			UpdatePosition();
		}

		private void Show(string text, Color color, float time)
		{
			if (field != null)
			{
				field.text = text;
				field.color = color;
			}
			duration = Mathf.Max(time, 0.01f);
			timer = duration;
			if (canvasGroup != null)
				canvasGroup.alpha = 1f;
		}

		private void LateUpdate()
		{
			timer -= Time.deltaTime;
			if (timer <= 0f)
			{
				if (canvas != null)
					canvas.Despawn(this);
				else
					gameObject.SetActive(false);
				return;
			}
			if (canvasGroup != null)
				canvasGroup.alpha = Mathf.Clamp01(timer / FadeTime);
			UpdatePosition();
		}

		private void UpdatePosition()
		{
			if (canvas == null || canvas.mainCamera == null)
				return;
			Vector3 position = worldPosition + Vector3.up * (RiseSpeed * (duration - timer));
			Vector3 screen = canvas.mainCamera.WorldToScreenPoint(position);
			transform.position = new Vector3(screen.x, screen.y, 0f);
		}
	}
}
