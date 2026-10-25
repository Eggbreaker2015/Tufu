using System;

namespace Codebase.ABTests
{
	[Serializable]
	public class GemsHudOfferData
	{
		public float showDuration = 60f;

		public float hideDuration = 60f;

		public double baseGemValue = 0.65;

		public double highCostThreshold = 200.0;

		public double highCostLogBase = 0.07;

		public double costMultSmall = 0.04;

		public double costMultMedium = 0.1;

		public double costMultLarge = 0.25;

		public float tierWeightSmall = 0.5f;

		public float tierWeightMedium = 0.30000001192092896f;

		public float tierWeightLarge = 0.20000000298023224f;

		public float minExtraValuePercent = 0.05000000074505806f;

		public float maxExtraValuePercent = 0.2f;

		public bool fixedDiscountInUi = true;

		public int fixedDiscountPercent = 15;
	}
}
