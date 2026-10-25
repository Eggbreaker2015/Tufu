using System;
using System.Collections.Generic;

namespace Codebase.CloudSave
{
	public sealed class DevourSaveSummary
	{
		public DateTime SavedAtUtc { get; private set; }

		public int PlaytimeMinutes { get; private set; }

		public double Gems { get; private set; }

		public int LocationNumber { get; private set; }

		public int ItemsCount { get; private set; }

		public int ByteSize { get; private set; }

		public IReadOnlyDictionary<string, double> CurrencyBalances { get; private set; }

		public static bool TryParse(byte[] bytes, out DevourSaveSummary summary)
		{
			summary = null;
			return false;
		}

		private static DateTime ReadSavedAtUtc(ReadOnlyMemory<byte> input)
		{
			return default;
		}
	}
}
