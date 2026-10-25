using UnityEngine;
using UnityEngine.EventSystems;
using Codebase.Core;

namespace Codebase.Player.Input
{
	public class Joystick : MonoBehaviour, IPointerDownHandler, IEventSystemHandler, IPointerUpHandler, IDragHandler
	{
		[SerializeField]
		private RectTransform pad;

		[SerializeField]
		private RectTransform stick;

		private Canvas canvas;

		private UnityEngine.Camera uiCamera;

		private Vector2 inputVector;

		private float radius;

		private Vector3 defaultPosition;

		private void Awake()
		{
			// Native ELF VA 0x14663ac.
			canvas = GetComponentInParent<Canvas>();
			uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
			radius = pad.sizeDelta.x * 0.5f;
			defaultPosition = pad.anchoredPosition;
			GameContext.Set(this);
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			// Native ELF VA 0x14664d8.
			RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, eventData.position, uiCamera, out Vector2 position);
			pad.anchoredPosition = position;
			stick.anchoredPosition = Vector2.zero;
			pad.gameObject.SetActive(true);
		}

		public void OnDrag(PointerEventData eventData)
		{
			// Native ELF VA 0x1466630.
			RectTransformUtility.ScreenPointToLocalPointInRectangle(pad, eventData.position, uiCamera, out Vector2 position);
			if (position.sqrMagnitude > radius * radius) position = position.normalized * radius;
			stick.anchoredPosition = position;
			inputVector = position / radius;
		}

		public void OnPointerUp(PointerEventData eventData)
		{
			// Native ELF VA 0x1466770: returns pad to its initial anchored position.
			ResetPointerInput();
		}

		public Vector2 GetInput()
		{
			// Learning project extension: share the original movement/animation path.
			if (!isActiveAndEnabled || !Application.isFocused || IsEditingText())
				return Vector2.zero;

			// The legacy axes already map WASD and arrow keys in InputManager.asset.
			Vector2 keyboard = new Vector2(UnityEngine.Input.GetAxisRaw("Horizontal"), UnityEngine.Input.GetAxisRaw("Vertical"));
			return keyboard.sqrMagnitude > 0f ? Vector2.ClampMagnitude(keyboard, 1f) : inputVector;
		}

		private static bool IsEditingText()
		{
#if UNITY_EDITOR
			if (UnityEditor.EditorGUIUtility.editingTextField) return true;
#endif
			GameObject selected = EventSystem.current == null ? null : EventSystem.current.currentSelectedGameObject;
			if (selected == null) return false;
			UnityEngine.UI.InputField legacyField = selected.GetComponentInParent<UnityEngine.UI.InputField>();
			if (legacyField != null && legacyField.isFocused) return true;
			TMPro.TMP_InputField textField = selected.GetComponentInParent<TMPro.TMP_InputField>();
			return textField != null && textField.isFocused;
		}

		private void OnDisable()
		{
			ResetPointerInput();
		}

		private void OnApplicationFocus(bool hasFocus)
		{
			if (!hasFocus) ResetPointerInput();
		}

		private void OnApplicationPause(bool paused)
		{
			if (paused) ResetPointerInput();
		}

		private void ResetPointerInput()
		{
			inputVector = Vector2.zero;
			if (stick != null) stick.anchoredPosition = Vector2.zero;
			if (pad != null) pad.anchoredPosition = defaultPosition;
		}
	}
}
