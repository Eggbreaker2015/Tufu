using System;
using System.Globalization;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Codebase.Simulation.Harness
{
	// Newtonsoft settings shared by the request, manifests and side files (design 1.4.2, 1.4.3):
	// unknown members are errors, doubles round-trip ("R"), floats use G9, NaN/Infinity are bare symbols,
	// enums are integers, LF newlines.
	public static class SimulationJson
	{
		public static JsonSerializerSettings ReadSettings()
		{
			return new JsonSerializerSettings
			{
				MissingMemberHandling = MissingMemberHandling.Error,
				FloatParseHandling = FloatParseHandling.Double,
				DateParseHandling = DateParseHandling.None,
				MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
				TypeNameHandling = TypeNameHandling.None,
				Culture = CultureInfo.InvariantCulture,
			};
		}

		public static JsonSerializerSettings WriteSettings()
		{
			var settings = new JsonSerializerSettings
			{
				Formatting = Formatting.Indented,
				FloatFormatHandling = FloatFormatHandling.Symbol,
				DateParseHandling = DateParseHandling.None,
				TypeNameHandling = TypeNameHandling.None,
				Culture = CultureInfo.InvariantCulture,
				NullValueHandling = NullValueHandling.Include,
			};
			settings.Converters.Add(new FloatConverter());
			return settings;
		}

		public static T Read<T>(string json)
		{
			return JsonConvert.DeserializeObject<T>(json, ReadSettings());
		}

		public static T ReadFile<T>(string path)
		{
			return Read<T>(File.ReadAllText(path, Encoding.UTF8));
		}

		public static string Write(object value)
		{
			var serializer = JsonSerializer.Create(WriteSettings());
			var text = new StringWriter(CultureInfo.InvariantCulture) { NewLine = "\n" };
			using (var writer = new JsonTextWriter(text) { Formatting = Formatting.Indented, Indentation = 1, IndentChar = '\t' })
			{
				writer.FloatFormatHandling = FloatFormatHandling.Symbol;
				serializer.Serialize(writer, value);
			}
			return text.ToString() + "\n";
		}

		// Writes through a temporary file so readers never see a partial file.
		public static void WriteFile(string path, object value)
		{
			WriteText(path, Write(value));
		}

		public static void WriteText(string path, string text)
		{
			string dir = Path.GetDirectoryName(path);
			if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
			string tmp = path + ".tmp";
			File.WriteAllText(tmp, text, new UTF8Encoding(false));
			if (File.Exists(path)) File.Delete(path);
			File.Move(tmp, path);
		}

		public static string FormatDouble(double value)
		{
			if (double.IsNaN(value)) return "NaN";
			if (double.IsPositiveInfinity(value)) return "Infinity";
			if (double.IsNegativeInfinity(value)) return "-Infinity";
			return value.ToString("R", CultureInfo.InvariantCulture);
		}

		public static string FormatFloat(float value)
		{
			if (float.IsNaN(value)) return "NaN";
			if (float.IsPositiveInfinity(value)) return "Infinity";
			if (float.IsNegativeInfinity(value)) return "-Infinity";
			return value.ToString("G9", CultureInfo.InvariantCulture);
		}

		public static JObject ToJObject(object value)
		{
			return JObject.Parse(Write(value), new JsonLoadSettings());
		}

		private sealed class FloatConverter : JsonConverter
		{
			public override bool CanRead => false;

			public override bool CanConvert(Type objectType) =>
				objectType == typeof(float) || objectType == typeof(double) || objectType == typeof(float?) || objectType == typeof(double?);

			public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
			{
				throw new NotSupportedException();
			}

			public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
			{
				writer.WriteRawValue(value is float f ? FormatFloat(f) : FormatDouble((double)value));
			}
		}
	}
}
