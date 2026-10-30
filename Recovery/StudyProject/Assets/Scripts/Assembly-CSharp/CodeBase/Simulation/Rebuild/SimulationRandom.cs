using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Codebase.Simulation.Rebuild
{
	// The only source of randomness of the rebuilt simulator (design 1.5 C# rule 4, 4.7; unit C3c). Every draw
	// belongs to a named stream (an item Key for fractional copies), and streams never share state: adding,
	// removing or changing the calls of one stream leaves every other stream's results unchanged.
	//   Carry (default): Roll(stream, x) = floor(x) plus the stream's carry of the fractional parts, so the
	//     results are integers, deterministic, and for any sequence 0 <= sum(x) - sum(n) < 1 per stream
	//     (up to double rounding of the accumulated fractions).
	//   Seeded: Roll(stream, x) = floor(x) + (u < frac(x)), u uniform in [0, 1) from the stream's SplitMix64,
	//     seeded by StreamSeed(run seed, stream name). Every Roll draws one u, whatever x is.
	// NextDouble(stream) is a uniform draw of the stream's SplitMix64 in either mode (Carry rolls never touch it).
	// Python port: tufu_balance.unity.simrandom (tests/test_random.py runs both on the same calls).
	public sealed class SimulationRandom
	{
		private const ulong Golden = 0x9E3779B97F4A7C15UL;

		private sealed class Stream
		{
			public double Carry;
			public ulong State;
			public long Rolls;
			public long Draws;
		}

		private readonly SortedDictionary<string, Stream> streams = new SortedDictionary<string, Stream>(StringComparer.Ordinal);

		public SimulationRandom(RollMode mode, int seed)
		{
			if (!Enum.IsDefined(typeof(RollMode), mode)) throw new ArgumentException("unknown RollMode " + (int)mode);
			Mode = mode;
			Seed = seed;
		}

		public RollMode Mode { get; }

		public int Seed { get; }

		public int StreamCount => streams.Count;

		// Integer count for an expected count x >= 0 (negative x works the same way).
		public int Roll(string stream, double x)
		{
			if (double.IsNaN(x) || double.IsInfinity(x)) throw new ArgumentException("Roll(" + stream + "): x = " + x);
			Stream s = Get(stream);
			s.Rolls++;
			double whole = Math.Floor(x);
			double fraction = x - whole;
			double extra;
			if (Mode == RollMode.Carry)
			{
				double carry = s.Carry + fraction;
				extra = Math.Floor(carry);
				s.Carry = carry - extra;
			}
			else extra = Uniform(s) < fraction ? 1.0 : 0.0;
			double n = whole + extra;
			if (n > int.MaxValue || n < int.MinValue) throw new OverflowException("Roll(" + stream + "): " + n + " does not fit an int");
			return (int)n;
		}

		public double NextDouble(string stream) => Uniform(Get(stream));

		// The accumulated fraction of a stream (Carry), 0 for an unused one.
		public double Carried(string stream) => streams.TryGetValue(stream, out Stream s) ? s.Carry : 0.0;

		// Per stream, sorted by name: rolls, draws, carry bits, generator state. Part of the daily digest.
		public string StateText()
		{
			var sb = new StringBuilder();
			sb.Append("mode=").Append((int)Mode).Append(",seed=").Append(Seed.ToString(CultureInfo.InvariantCulture)).Append('\n');
			foreach (var pair in streams)
				sb.Append(pair.Key).Append('=').Append(pair.Value.Rolls.ToString(CultureInfo.InvariantCulture)).Append(',')
					.Append(pair.Value.Draws.ToString(CultureInfo.InvariantCulture)).Append(',')
					.Append(BitConverter.DoubleToInt64Bits(pair.Value.Carry).ToString("x16", CultureInfo.InvariantCulture)).Append(',')
					.Append(pair.Value.State.ToString("x16", CultureInfo.InvariantCulture)).Append('\n');
			return sb.ToString();
		}

		public IEnumerable<string> StreamNames => streams.Keys.ToList();

		// SplitMix64 start state of a stream: the run seed and the FNV-1a 64 hash of the UTF-8 name, mixed.
		public static ulong StreamSeed(int seed, string stream)
		{
			ulong h = 0xCBF29CE484222325UL;
			foreach (byte b in new UTF8Encoding(false).GetBytes(stream))
			{
				h ^= b;
				h = unchecked(h * 0x100000001B3UL);
			}
			return Mix(unchecked(h + (ulong)(uint)seed * Golden));
		}

		private Stream Get(string stream)
		{
			if (stream == null) throw new ArgumentNullException(nameof(stream));
			if (!streams.TryGetValue(stream, out Stream s))
			{
				s = new Stream { State = StreamSeed(Seed, stream) };
				streams[stream] = s;
			}
			return s;
		}

		private static double Uniform(Stream s)
		{
			s.Draws++;
			s.State = unchecked(s.State + Golden);
			return (Mix(s.State) >> 11) * (1.0 / 9007199254740992.0);
		}

		private static ulong Mix(ulong z)
		{
			unchecked
			{
				z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
				z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
				return z ^ (z >> 31);
			}
		}
	}
}
