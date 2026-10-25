using UnityEngine;

namespace Codebase.Enemy
{
	public class SpawnerOptimumTimeDaysConverter : MonoBehaviour
	{
		[SerializeField]
		private int locationFrom;
		private void Apply()
		{
		}

		public static float ConvertOptimumToDays(float optimumTime)
		{ // Original ELF 0x150ced0; FRINTM/FRINTP assembly establishes floating rounding. Candidate only.
			float divided = optimumTime / 30f;
			float scaled = divided * 10f;
			double integral = System.Math.Truncate((double)scaled);
			double fraction = (double)scaled - integral;
			float rounded;
			if (scaled >= 0f)
			{
				if (fraction == 0.5)
					rounded = (((long)integral & 1L) == 0L) ? (float)integral : (float)integral + 1f;
				else
				{
					float shifted = scaled + 0.5f;
					rounded = (float)System.Math.Floor((double)shifted);
				}
			}
			else
			{
				if (fraction == -0.5)
					rounded = (((long)integral & 1L) == 0L) ? (float)integral : (float)integral - 1f;
				else
				{
					float shifted = scaled - 0.5f;
					rounded = (float)System.Math.Ceiling((double)shifted);
				}
			}

			return rounded / 10f;
		}

		public static float ConvertDaysToOptimum(float days)
		{ // Original ELF 0x150cf78; FRINTM/FRINTP assembly establishes floating rounding. Candidate only.
			return days * 30f;
		}
	}
}
