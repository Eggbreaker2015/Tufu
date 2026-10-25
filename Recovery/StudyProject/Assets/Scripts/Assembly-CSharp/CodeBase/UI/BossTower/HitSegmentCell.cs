using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.BossTower
{
	public class HitSegmentCell : MonoBehaviour
	{
		[SerializeField]
		private Image activeImage;

		[SerializeField]
		private Image inactiveImage;

		[SerializeField]
		private Image isFinalImage;

		private bool isFilled;

		private void OnEnable()
		{
			// Native ELF VA 0x13c828c; isFilled is deliberately not reset here.
			if (isFinalImage != null) isFinalImage.enabled = false;
			inactiveImage.enabled = true;
			activeImage.enabled = false;
		}

		public void SetFilled(bool filled)
		{
			// Native ELF VA 0x13c7f64; original 0.3/0.25/5/0.5 punch arguments.
			if (filled && !isFilled) { transform.DOKill(); transform.localScale = Vector3.one; transform.DOPunchScale(Vector3.one * .3f,.25f,5,.5f); }
			isFilled = filled; activeImage.enabled = filled; inactiveImage.enabled = !filled;
		}

		public void SetFinal(bool isFinal)
		{
			// Native ELF VA 0x13c80a8; does not assign isFilled.
			if (isFinal && !isFilled) { transform.DOKill(); transform.localScale = Vector3.one; transform.DOPunchScale(Vector3.one * .3f,.25f,5,.5f); }
			activeImage.enabled = !isFinal; inactiveImage.enabled = !isFinal; isFinalImage.enabled = isFinal;
		}
	}
}
