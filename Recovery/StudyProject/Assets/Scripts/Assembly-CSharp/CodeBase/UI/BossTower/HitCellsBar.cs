using UnityEngine;

namespace Codebase.UI.BossTower
{
	public class HitCellsBar : MonoBehaviour
	{
		[SerializeField]
		private HitSegmentCell[] cells;

		private int previousMax;

		public void SetValue(int filledCount)
		{
			// Native ELF VA 0x13c2030.
			for (int i = 0; i < cells.Length; i++) cells[i].SetFilled(i < filledCount);
		}

		public void SetFinal(int finalIndex)
		{
			// Native ELF VA 0x13c2178.
			if (finalIndex >= 0 && finalIndex < cells.Length) cells[finalIndex].SetFinal(true);
		}

		public void SetMax(int max)
		{
			// Native ELF VA 0x13c81fc.
			if (previousMax == max) return;
			previousMax = max;
			for (int i = 0; i < cells.Length; i++) cells[i].gameObject.SetActive(i < max);
		}
	}
}
