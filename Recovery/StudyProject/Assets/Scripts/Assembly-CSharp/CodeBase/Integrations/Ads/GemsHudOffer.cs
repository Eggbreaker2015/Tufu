using System;
using Codebase.Unit;

namespace Codebase.Integrations.Ads
{
	public class GemsHudOffer
	{
		public StatType StatType;

		public int Cost;

		public int BaseCost;

		public double Value;

		public GemsHudOfferTier Tier;

		public double Rpm;

		public int DiscountPercent { get {
            // Native14c9978, signed native subtraction and Math.Round midpoint-to-even.
            int difference = unchecked(BaseCost - Cost); if (difference == 0 || BaseCost < Cost) return 0;
            return NativeInt(Math.Round((double)difference * 100.0 / (double)BaseCost));
        } }
        // ARM FCVTZS plus explicit original +Infinity guard; normal inputs are original Math.Round outputs.
        private static int NativeInt(double value) => double.IsNaN(value) ? 0 : double.IsPositiveInfinity(value) ? int.MinValue : value >= int.MaxValue ? int.MaxValue : value <= int.MinValue ? int.MinValue : (int)value;

	}
}
