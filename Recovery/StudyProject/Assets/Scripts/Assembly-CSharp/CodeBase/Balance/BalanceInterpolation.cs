using System.Globalization;
using System.Linq;
using System;
using System.Collections.Generic;

namespace Codebase.Balance
{
	[Serializable]
	public struct BalanceInterpolation
	{
		public bool Use;

		public string Name;

		public BalanceInterpolationType Type;

		public string IslandPoints;

		public string TimingPoints;

		public string CoefPoints;

		public float Evaluate(float timing)
		{
// Original ELF VA 0x1579ed4. Ordered comparisons deliberately preserve NaN behavior.
            var timings = ParsePoints(TimingPoints);
            var coefficients = ParsePoints(CoefPoints);
            if (timings.Count < 2 || coefficients.Count < 2 || timings.Count != coefficients.Count)
            {
                UnityEngine.Debug.LogError("Invalid interpolation points: " + Name + ". TimingPoints count must match CoefPoints count and be at least 2.");
                return 0f;
            }
            if (timings[0] >= timing) return coefficients[0];
            for (int i = 0; i < timings.Count - 1; i++)
            {
                float left = timings[i];
                float right = timings[i + 1];
                if (right < timing) continue;
                float progress = 0f;
                if (left != right)
                {
                    float raw = (timing - left) / (right - left);
                    float capped = raw > 1f ? 1f : raw;
                    progress = raw < 0f ? 0f : capped;
                }
                float first = coefficients[i];
                float second = coefficients[i + 1];
                float factor = progress < 0f ? 0f : progress;
                return first + factor * (second - first);
            }
            return coefficients[coefficients.Count - 1];
		}

		public void ApplyIslandTimings()
		{
		}

		private static List<float> ParsePoints(string value)
		{
// Original ELF VA 0x157a65c and three original delegate bodies.
            if (string.IsNullOrWhiteSpace(value)) return new List<float>();
            return value.Split(';').Select(part => part.Trim()).Where(part => !string.IsNullOrEmpty(part))
                .Select(part => float.Parse(part, CultureInfo.InvariantCulture)).ToList();
		}

		private static string FormatPoint(float value)
		{
			return null;
		}
	}
}
