using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Codebase.Simulation.Rebuild
{
	// Fractional item counts (design 4.7): a per-stream accumulator, or SplitMix64 draws.
	public enum RollMode
	{
		Carry = 0,
		Seeded = 1
	}

	// Day-change order (design 4.4, P07). The data fixes RpmFirst; the others are ablations only.
	public enum DpsCacheProvenance
	{
		// RPM and DpsData under K2/K3(d-1), then the K(d) modifiers, then the forced snapshot.
		RpmFirst = 0,
		// K(d) modifiers first, then RPM and DpsData.
		ModifierFirst = 1,
		// RPM under K2/K3(d-1) from the last tick's DpsData/DefenseData caches (not recomputed).
		StaleTickCache = 2
	}

	// Evolution tables (design 4.5, Q16): the shipped tables, or the original co-evolution rules.
	public enum AutoBalanceMode
	{
		Frozen = 0,
		Regenerate = 1
	}

	// Calibratable parameters of the rebuilt simulator (design 4.8); core fields with code defaults (unit C3b).
	// Module units add fields in SimulationRebuildParams.<Module>.cs; defaults mirror Balance/sim/params/defaults/*.json.
	// Field types stay JsonUtility-compatible (int, float, double, bool, string, enums and their arrays).
	// No UnityEngine here (Balance/tools/rebuildcheck compiles this file under .NET); the parameter asset and
	// the loader are in SimulationRebuildParamsAsset.cs (unit C3c).
	[Serializable]
	public partial class SimulationRebuildParams
	{
		public int SchemaVersion = 1;
		public RollMode RollMode = RollMode.Carry;
		public int Seed = 1;
		public DpsCacheProvenance DpsCacheProvenance = DpsCacheProvenance.RpmFirst;
		// P06: early-list cadence; EarlySnapshotIntervals[i] minutes applies from P >= EarlySnapshotIntervalStarts[i].
		public float[] EarlySnapshotIntervalStarts = { 0f, 20f };
		public float[] EarlySnapshotIntervals = { 1f, 5f };
		// P05: fast-phase offline step until the day index changes.
		public float FastAfkStepMinutes = 120f;
		public AutoBalanceMode AutoBalanceMode = AutoBalanceMode.Frozen;

		// The request's params object over the code defaults; unknown members are errors.
		public static SimulationRebuildParams FromJson(JObject json)
		{
			return FromJson(json, false, out _);
		}

		// As above; `missing` lists the fields left at their code defaults, an error when `requireComplete`
		// (the parameter importer takes complete objects only).
		public static SimulationRebuildParams FromJson(JObject json, bool requireComplete, out List<string> missing)
		{
			var result = new SimulationRebuildParams();
			missing = FieldNames().Where(name => json == null || json.Property(name) == null).ToList();
			if (requireComplete && missing.Count > 0)
				throw new JsonSerializationException("params is missing " + string.Join(", ", missing) + " (a complete object is required)");
			if (json == null) return result;
			CheckTokens(json);
			var settings = new JsonSerializerSettings
			{
				MissingMemberHandling = MissingMemberHandling.Error,
				ObjectCreationHandling = ObjectCreationHandling.Replace,
			};
			JsonConvert.PopulateObject(json.ToString(Formatting.None), result, settings);
			return result;
		}

		// Newtonsoft matches member names ignoring case and converts strings to numbers and enums; the request's
		// params_hash covers the text as written, so only exact names and JSON types that fit the field pass.
		private static void CheckTokens(JObject json)
		{
			var fields = Fields().ToDictionary(f => f.Name, StringComparer.Ordinal);
			foreach (JProperty property in json.Properties())
			{
				if (!fields.TryGetValue(property.Name, out FieldInfo field))
				{
					string near = fields.Keys.FirstOrDefault(k => string.Equals(k, property.Name, StringComparison.OrdinalIgnoreCase));
					throw new JsonSerializationException("Could not find member '" + property.Name + "' on SimulationRebuildParams"
						+ (near != null ? " (field names are case-sensitive: " + near + ")" : ""));
				}
				string problem = TokenProblem(property.Value, field.FieldType);
				if (problem != null) throw new JsonSerializationException("params." + property.Name + ": " + problem);
			}
		}

		private static string TokenProblem(JToken token, Type type)
		{
			JTokenType kind = token.Type;
			if (type.IsArray)
			{
				if (kind != JTokenType.Array) return "expected an array, got " + kind;
				foreach (JToken item in (JArray)token)
				{
					string problem = TokenProblem(item, type.GetElementType());
					if (problem != null) return problem;
				}
				return null;
			}
			if (type == typeof(string)) return kind == JTokenType.String || kind == JTokenType.Null ? null : "expected a string, got " + kind;
			if (type == typeof(bool)) return kind == JTokenType.Boolean ? null : "expected true or false, got " + kind;
			if (type.IsEnum || type == typeof(int) || type == typeof(long))
				return kind == JTokenType.Integer ? null : "expected an integer" + (type.IsEnum ? " (" + type.Name + " member value)" : "") + ", got " + kind;
			if (type == typeof(float) || type == typeof(double))
				return kind == JTokenType.Integer || kind == JTokenType.Float ? null : "expected a number, got " + kind;
			return "field type " + type.Name + " is not a parameter type (design 4.8)";
		}

		// Null when usable, else the reason.
		public string Validate()
		{
			foreach (FieldInfo field in Fields())
			{
				object value = field.GetValue(this);
				IEnumerable<object> numbers = value is float f ? new object[] { f } : value is double d ? new object[] { d }
					: value is float[] fs ? fs.Cast<object>() : value is double[] ds ? ds.Cast<object>() : Enumerable.Empty<object>();
				if (numbers.Any(x => x is float y ? float.IsNaN(y) || float.IsInfinity(y) : double.IsNaN((double)x) || double.IsInfinity((double)x)))
					return "params." + field.Name + " must be finite";
			}
			if (SchemaVersion != 1) return "params.SchemaVersion " + SchemaVersion + " is not supported";
			if (!Enum.IsDefined(typeof(DpsCacheProvenance), DpsCacheProvenance)) return "params.DpsCacheProvenance " + (int)DpsCacheProvenance + " is unknown";
			if (!Enum.IsDefined(typeof(RollMode), RollMode)) return "params.RollMode " + (int)RollMode + " is unknown";
			if (!Enum.IsDefined(typeof(AutoBalanceMode), AutoBalanceMode)) return "params.AutoBalanceMode " + (int)AutoBalanceMode + " is unknown";
			if (EarlySnapshotIntervalStarts == null || EarlySnapshotIntervals == null || EarlySnapshotIntervalStarts.Length == 0
			    || EarlySnapshotIntervalStarts.Length != EarlySnapshotIntervals.Length || EarlySnapshotIntervalStarts[0] != 0f)
				return "params.EarlySnapshotIntervalStarts must start at 0 and match EarlySnapshotIntervals";
			for (int i = 0; i < EarlySnapshotIntervals.Length; i++)
			{
				if (!(EarlySnapshotIntervals[i] > 0f)) return "params.EarlySnapshotIntervals must be > 0";
				if (i > 0 && !(EarlySnapshotIntervalStarts[i] > EarlySnapshotIntervalStarts[i - 1])) return "params.EarlySnapshotIntervalStarts must increase";
			}
			if (!(FastAfkStepMinutes > 0f)) return "params.FastAfkStepMinutes must be > 0";
			return null;
		}

		// The parameter object: the public instance fields of every partial file.
		public static IEnumerable<string> FieldNames() => Fields().Select(f => f.Name);

		private static IEnumerable<FieldInfo> Fields() =>
			typeof(SimulationRebuildParams).GetFields(BindingFlags.Instance | BindingFlags.Public).OrderBy(f => f.Name, SimulationCanonicalJson.CodePointComparer);

		// Canonical JSON of the effective values as Python writes the same object (tufu_balance.calibrate.params):
		// enums as integers, a float field as the shortest decimal of its float32 value.
		public string EffectiveCanonicalJson()
		{
			var sb = new StringBuilder("{");
			bool first = true;
			foreach (FieldInfo field in Fields())
			{
				if (!first) sb.Append(',');
				first = false;
				SimulationCanonicalJson.WriteString(sb, field.Name);
				sb.Append(':');
				WriteValue(sb, field.GetValue(this), field.FieldType, field.Name);
			}
			return sb.Append('}').ToString();
		}

		// Equals the request's params_hash whenever the request was complete and each of its floats is the
		// shortest text of a float32 (what Python layers hold in practice).
		public string EffectiveHash() => SimulationCanonicalJson.Sha256(EffectiveCanonicalJson());

		public JObject EffectiveJObject() => SimulationCanonicalJson.Parse(EffectiveCanonicalJson());

		public SimulationRebuildParams Clone() => FromJson(EffectiveJObject());

		private static void WriteValue(StringBuilder sb, object value, Type type, string name)
		{
			if (value == null)
			{
				sb.Append("null");
				return;
			}
			if (type.IsArray)
			{
				sb.Append('[');
				int i = 0;
				foreach (object item in (IEnumerable)value)
				{
					if (i++ > 0) sb.Append(',');
					WriteValue(sb, item, type.GetElementType(), name);
				}
				sb.Append(']');
				return;
			}
			if (type.IsEnum)
			{
				sb.Append(Convert.ToInt64(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture));
				return;
			}
			switch (value)
			{
				case int i: sb.Append(i.ToString(CultureInfo.InvariantCulture)); return;
				case long l: sb.Append(l.ToString(CultureInfo.InvariantCulture)); return;
				case bool b: sb.Append(b ? "true" : "false"); return;
				case string s: SimulationCanonicalJson.WriteString(sb, s); return;
				case float f: sb.Append(SimulationCanonicalJson.FormatFloat(f)); return;
				case double d: sb.Append(SimulationCanonicalJson.FormatDouble(d)); return;
			}
			throw new NotSupportedException("params." + name + ": field type " + type.Name + " is not a parameter type (design 4.8)");
		}
	}

	// Python's json.dumps(value, sort_keys=True, separators=(",", ":"), ensure_ascii=False, allow_nan=True) and the
	// sha256 of its UTF-8 bytes: params_hash of tufu_balance.unity.request and tufu_balance.calibrate.params.
	// Numbers follow float.__repr__ (shortest round-trip digits; exponent form below 1e-4 and from 1e16 on),
	// computed with exact integer arithmetic, so the text does not depend on the runtime's number formatting.
	public static class SimulationCanonicalJson
	{
		public static string Hash(JToken token) => Sha256(Write(token));

		public static string Write(JToken token)
		{
			var sb = new StringBuilder();
			Write(sb, token);
			return sb.ToString();
		}

		// Doubles stay doubles, dates stay strings (as SimulationJson.ReadSettings).
		public static JObject Parse(string json)
		{
			using (var reader = new JsonTextReader(new StringReader(json)) { DateParseHandling = DateParseHandling.None, FloatParseHandling = FloatParseHandling.Double })
				return JObject.Load(reader);
		}

		public static string Sha256(string text)
		{
			using (SHA256 sha = SHA256.Create())
			{
				byte[] hash = sha.ComputeHash(new UTF8Encoding(false).GetBytes(text));
				var sb = new StringBuilder(64);
				foreach (byte b in hash) sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
				return sb.ToString();
			}
		}

		private static void Write(StringBuilder sb, JToken token)
		{
			switch (token == null ? JTokenType.Null : token.Type)
			{
				case JTokenType.Object:
					sb.Append('{');
					bool first = true;
					foreach (JProperty p in ((JObject)token).Properties().OrderBy(p => p.Name, CodePointComparer))
					{
						if (!first) sb.Append(',');
						first = false;
						WriteString(sb, p.Name);
						sb.Append(':');
						Write(sb, p.Value);
					}
					sb.Append('}');
					return;
				case JTokenType.Array:
					sb.Append('[');
					int i = 0;
					foreach (JToken item in (JArray)token)
					{
						if (i++ > 0) sb.Append(',');
						Write(sb, item);
					}
					sb.Append(']');
					return;
				case JTokenType.Null:
					sb.Append("null");
					return;
				case JTokenType.Boolean:
					sb.Append((bool)token ? "true" : "false");
					return;
				case JTokenType.String:
					WriteString(sb, (string)token);
					return;
				case JTokenType.Integer:
					object v = ((JValue)token).Value;
					sb.Append(v is BigInteger big ? big.ToString(CultureInfo.InvariantCulture) : Convert.ToInt64(v, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture));
					return;
				case JTokenType.Float:
					object f = ((JValue)token).Value;
					if (f is decimal) throw new NotSupportedException("decimal JSON numbers have no canonical form here (parse floats as double)");
					sb.Append(FormatDouble(Convert.ToDouble(f, CultureInfo.InvariantCulture)));
					return;
			}
			throw new NotSupportedException("JSON token type " + token.Type + " has no canonical form");
		}

		// json.encoder with ensure_ascii=False: escapes only the quote, the backslash and control characters.
		public static void WriteString(StringBuilder sb, string s)
		{
			sb.Append('"');
			foreach (char c in s)
			{
				switch (c)
				{
					case '"': sb.Append("\\\""); break;
					case '\\': sb.Append("\\\\"); break;
					case '\n': sb.Append("\\n"); break;
					case '\r': sb.Append("\\r"); break;
					case '\t': sb.Append("\\t"); break;
					case '\b': sb.Append("\\b"); break;
					case '\f': sb.Append("\\f"); break;
					default:
						if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
						else sb.Append(c);
						break;
				}
			}
			sb.Append('"');
		}

		// Python orders str keys by code point (UTF-16 ordinal order differs above U+D7FF).
		public static readonly IComparer<string> CodePointComparer = Comparer<string>.Create((a, b) =>
		{
			int i = 0, j = 0;
			while (i < a.Length && j < b.Length)
			{
				int x = char.IsSurrogatePair(a, i) ? char.ConvertToUtf32(a, i) : a[i];
				int y = char.IsSurrogatePair(b, j) ? char.ConvertToUtf32(b, j) : b[j];
				if (x != y) return x < y ? -1 : 1;
				i += x > 0xFFFF ? 2 : 1;
				j += y > 0xFFFF ? 2 : 1;
			}
			return (a.Length - i).CompareTo(b.Length - j);
		});

		// float.__repr__ of a double; NaN and infinities as json.dumps writes them.
		public static string FormatDouble(double value)
		{
			if (double.IsNaN(value)) return "NaN";
			if (double.IsInfinity(value)) return value > 0 ? "Infinity" : "-Infinity";
			long bits = BitConverter.DoubleToInt64Bits(value);
			bool negative = bits < 0;
			int biased = (int)((bits >> 52) & 0x7FF);
			ulong fraction = (ulong)bits & 0xFFFFFFFFFFFFFUL;
			if (biased == 0 && fraction == 0) return negative ? "-0.0" : "0.0";
			ulong mantissa = biased == 0 ? fraction : fraction | (1UL << 52);
			int exp2 = biased == 0 ? -1074 : biased - 1075;
			Shortest(mantissa, exp2, fraction == 0 && biased > 1, out BigInteger digits, out int exp10);
			return (negative ? "-" : "") + Repr(digits, exp10);
		}

		// The shortest decimal of a float32, written as Python writes the double parsed from it: the text of a
		// float parameter whose value is that float32.
		public static string FormatFloat(float value)
		{
			if (float.IsNaN(value)) return "NaN";
			if (float.IsInfinity(value)) return value > 0 ? "Infinity" : "-Infinity";
			int bits = BitConverter.SingleToInt32Bits(value);
			bool negative = bits < 0;
			int biased = (bits >> 23) & 0xFF;
			uint fraction = (uint)bits & 0x7FFFFFU;
			if (biased == 0 && fraction == 0) return negative ? "-0.0" : "0.0";
			ulong mantissa = biased == 0 ? fraction : fraction | (1U << 23);
			int exp2 = biased == 0 ? -149 : biased - 150;
			Shortest(mantissa, exp2, fraction == 0 && biased > 1, out BigInteger digits, out int exp10);
			return (negative ? "-" : "") + Repr(digits, exp10);
		}

		// value = mantissa * 2^exp2 > 0. The fewest digits (digits * 10^exp10) that read back as the value under
		// round-half-even (interval ends count when the mantissa is even); of two such decimals the closer one.
		// In units u = 2^(exp2 - 2) the value is V = 4 * mantissa and the interval [V - lo, V + 2], lo = 1 at a
		// binade boundary (the lower neighbour is closer), else 2.
		private static void Shortest(ulong mantissa, int exp2, bool lowerHalfGap, out BigInteger digits, out int exp10)
		{
			BigInteger v = new BigInteger(mantissa) * 4;
			BigInteger lo = v - (lowerHalfGap ? 1 : 2), hi = v + 2;
			bool inclusive = (mantissa & 1) == 0;
			double approx = mantissa * Math.Pow(2.0, exp2);
			int e = double.IsInfinity(approx) || approx == 0.0 ? 0 : (int)Math.Floor(Math.Log10(approx));
			while (true)
			{
				Scale(exp2, e, out BigInteger a0, out BigInteger b0);
				BigInteger lead = BigInteger.Divide(v * b0, a0);
				if (lead < 1) e--;
				else if (lead >= 10) e++;
				else break;
			}
			for (int k = 1; k <= 17; k++)
			{
				int q = e - k + 1;
				Scale(exp2, q, out BigInteger a, out BigInteger b);
				BigInteger vb = v * b;
				BigInteger floor = BigInteger.Divide(vb, a);
				BigInteger ceil = floor + 1;
				bool floorOk = Within(floor * a, lo * b, hi * b, inclusive);
				bool ceilOk = Within(ceil * a, lo * b, hi * b, inclusive);
				if (!floorOk && !ceilOk) continue;
				BigInteger pick;
				if (floorOk && ceilOk)
				{
					BigInteger below = vb - floor * a, above = ceil * a - vb;
					pick = below < above ? floor : above < below ? ceil : (floor.IsEven ? floor : ceil);
				}
				else pick = floorOk ? floor : ceil;
				while (!pick.IsZero && (pick % 10).IsZero)
				{
					pick /= 10;
					q++;
				}
				digits = pick;
				exp10 = q;
				return;
			}
			throw new InvalidOperationException("no shortest decimal within 17 digits");
		}

		// Candidate c * 10^q against W units u: compare c * a with W * b.
		private static void Scale(int exp2, int q, out BigInteger a, out BigInteger b)
		{
			a = BigInteger.Pow(10, Math.Max(q, 0)) * BigInteger.Pow(2, Math.Max(2 - exp2, 0));
			b = BigInteger.Pow(2, Math.Max(exp2 - 2, 0)) * BigInteger.Pow(10, Math.Max(-q, 0));
		}

		private static bool Within(BigInteger x, BigInteger low, BigInteger high, bool inclusive) =>
			inclusive ? x >= low && x <= high : x > low && x < high;

		// format_float_short(x, 'r', 0, Py_DTSF_ADD_DOT_0, ...): exponent form when decpt <= -4 or decpt > 16.
		private static string Repr(BigInteger digits, int exp10)
		{
			string ds = digits.ToString(CultureInfo.InvariantCulture);
			int n = ds.Length, decpt = n + exp10;
			if (decpt <= -4 || decpt > 16)
			{
				int exp = decpt - 1;
				string mantissa = n == 1 ? ds : ds.Substring(0, 1) + "." + ds.Substring(1);
				return mantissa + "e" + (exp < 0 ? "-" : "+") + Math.Abs(exp).ToString("00", CultureInfo.InvariantCulture);
			}
			if (decpt <= 0) return "0." + new string('0', -decpt) + ds;
			if (decpt >= n) return ds + new string('0', decpt - n) + ".0";
			return ds.Substring(0, decpt) + "." + ds.Substring(decpt);
		}
	}
}
