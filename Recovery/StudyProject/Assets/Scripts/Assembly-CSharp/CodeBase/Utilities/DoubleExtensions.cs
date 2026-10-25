using System.Text;
using System;
using System.Globalization;

namespace Codebase.Utilities
{
	public static class DoubleExtensions
	{
		public readonly ref struct Formattable
		{
			public StringBuilder Builder { get; }

			public Formattable(StringBuilder builder)
			{
				Builder = builder;
			}

			public static implicit operator StringBuilder(Formattable f)
			{
				return f.Builder;
			}
		}

		private static readonly StringBuilder SharedStringBuilder = new StringBuilder();

		private static readonly string[] Suffixes = { "", "K", "M", "B", "T", "aa", "bb", "cc", "dd", "ee", "ff", "gg", "hh", "ii", "jj", "kk", "ll", "mm", "nn", "oo", "pp", "qq" };

		public static string Format(this double number)
		{
			// Native ELF VA 0x15c44cc.
			SharedStringBuilder.Clear();
			Format(number, SharedStringBuilder);
			return SharedStringBuilder.ToString();
		}

		public static Formattable FormatNonAlloc(this double number)
		{
			// Native ELF VA 0x15c48c8, preserving the shared mutable builder lifetime.
			SharedStringBuilder.Clear();
			Format(number, SharedStringBuilder);
			return new Formattable(SharedStringBuilder);
		}

		public static void Format(double number, StringBuilder sb)
		{
			// Native ELF VA 0x15c456c; literal format strings and 22 suffixes
			// were resolved through original ELF RELA pointers.
			string format;
			int suffix = 0;
			if (double.IsNaN(number) || double.IsInfinity(number)) format = "0";
			else if (number >= 100.0)
			{
				if (number >= 1000.0)
				{
					suffix = Math.Min(Math.Max((int)(Math.Log10(number) / 3.0), 0), Suffixes.Length - 1);
					number /= Math.Pow(1000.0, suffix);
				}
				format = number >= 100.0 ? "0" : number >= 10.0 ? "0.#" : "0.##";
			}
			else format = number < 0.0 ? "0.###" : number < 10.0 ? "0.##" : "0.#";
			Span<char> buffer = stackalloc char[32];
			if (number.TryFormat(buffer, out int written, format.AsSpan(), CultureInfo.InvariantCulture)) sb.Append(buffer.Slice(0, written));
			else sb.Append(number.ToString(format, CultureInfo.InvariantCulture));
			if (suffix != 0) sb.Append(Suffixes[suffix]);
		}

		public static bool Roll(this double number)
		{
			// Native ELF VA 0x15c4bf4.
			return UnityEngine.Random.Range(0f, 1f) < number;
		}

		public static int PrettyRound(this double number, int roundingDigits = 2, int firstDigitBoundary = 1, int secondDigitBoundary = 4)
		{
			// Native ELF VA 0x15c4c28 uses midpoint-to-even Math.Round.
			if (number < 10.0) return (int)Math.Round(number);
			double scale = Math.Pow(10.0, (int)Math.Log10(number) - roundingDigits + 1);
			int firstDigit = (int)(number / (scale * 10.0));
			double step = scale * 10.0;
			if (firstDigit <= secondDigitBoundary) step = scale * 5.0;
			if (firstDigit <= firstDigitBoundary) step = scale;
			return (int)(Math.Round(number / step) * step);
		}
	}
}
