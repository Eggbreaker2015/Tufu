using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI
{
	public class GlobalUpdateFeatureView : MonoBehaviour
	{
		[SerializeField]
		private Image image;

		[SerializeField]
		private TMP_Text tipText;

		[SerializeField]
		private TMP_Text descriptionText;

		[SerializeField]
		private CanvasGroup canvasGroup;

		public CanvasGroup CanvasGroup => this.canvasGroup;

		public void Setup(Sprite sprite, string tipKey, string descriptionKey)
		{
		}

		public void HideImmediate()
		{
		}

		public void PrepareFadeIn()
		{
		}
	}
}
