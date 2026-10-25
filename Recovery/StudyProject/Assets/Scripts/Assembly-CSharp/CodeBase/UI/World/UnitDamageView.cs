using Codebase.Unit.Events;
using TMPro;
using UnityEngine;

namespace Codebase.UI.World
{
	public class UnitDamageView : MonoBehaviour
	{
		[SerializeField]
		private CanvasGroup canvasGroup;

		[SerializeField]
		private TMP_Text field;

		[SerializeField]
		private Color playerColor;

		[SerializeField]
		private Color inefficientColor;

		private float timer;

		public void Setup(string reward, float time = 1f)
		{
		}

		public void Setup(WeaponHitEvent data, double value, double resist)
		{
		}

		private void LateUpdate()
		{
		}
	}
}
