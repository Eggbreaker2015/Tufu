using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Codebase.Simulation.Rebuild;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// stdin: a JSON array of commands; stdout: a JSON array of results, one per command.
//   {"op": "random", "mode": 0|1, "seed": n, "calls": [["stream", x], ["stream", null] = NextDouble, ...]}
//     -> {"results": [...], "state": StateText(), "carried": {stream: carry}}
//   {"op": "canonical", "value": <JSON>} -> {"text": ..., "hash": ...}
//   {"op": "format", "doubles": ["<16 hex>", ...], "floats": ["<8 hex>", ...]} -> {"doubles": [...], "floats": [...]}
//   {"op": "params", "json": {...} | null, "complete": bool}
//     -> {"ok", "error", "missing", "valid", "effective", "hash", "fields"}
internal static class Program
{
	private static int Main()
	{
		JArray commands;
		using (var reader = new JsonTextReader(Console.In) { DateParseHandling = DateParseHandling.None, FloatParseHandling = FloatParseHandling.Double })
			commands = JArray.Load(reader);
		var results = new JArray();
		foreach (JObject command in commands.Cast<JObject>()) results.Add(Run(command));
		var output = new StringWriter(CultureInfo.InvariantCulture);
		using (var writer = new JsonTextWriter(output)) results.WriteTo(writer);
		Console.Out.Write(output.ToString());
		return 0;
	}

	private static JObject Run(JObject command)
	{
		switch ((string)command["op"])
		{
			case "random":
				return RandomCalls(command);
			case "canonical":
				return new JObject { ["text"] = SimulationCanonicalJson.Write(command["value"]), ["hash"] = SimulationCanonicalJson.Hash(command["value"]) };
			case "format":
				return new JObject
				{
					["doubles"] = new JArray(command["doubles"].Select(h => (object)SimulationCanonicalJson.FormatDouble(BitConverter.Int64BitsToDouble(unchecked((long)ulong.Parse((string)h, NumberStyles.HexNumber))))).ToArray()),
					["floats"] = new JArray(command["floats"].Select(h => (object)SimulationCanonicalJson.FormatFloat(BitConverter.Int32BitsToSingle(unchecked((int)uint.Parse((string)h, NumberStyles.HexNumber))))).ToArray()),
				};
			case "params":
				return Params(command);
		}
		throw new ArgumentException("unknown op " + command["op"]);
	}

	private static JObject RandomCalls(JObject command)
	{
		var random = new SimulationRandom((RollMode)(int)command["mode"], (int)command["seed"]);
		var results = new JArray();
		foreach (JArray call in command["calls"].Cast<JArray>())
		{
			string stream = (string)call[0];
			if (call[1].Type == JTokenType.Null) results.Add(random.NextDouble(stream).ToString("R", CultureInfo.InvariantCulture));
			else results.Add(random.Roll(stream, (double)call[1]));
		}
		var carried = new JObject();
		foreach (string name in random.StreamNames) carried[name] = BitConverter.DoubleToInt64Bits(random.Carried(name)).ToString("x16");
		return new JObject { ["results"] = results, ["state"] = random.StateText(), ["carried"] = carried };
	}

	private static JObject Params(JObject command)
	{
		var result = new JObject { ["fields"] = new JArray(SimulationRebuildParams.FieldNames().ToArray()) };
		try
		{
			JObject json = command["json"] as JObject;
			SimulationRebuildParams p = SimulationRebuildParams.FromJson(json, (bool?)command["complete"] ?? false, out List<string> missing);
			result["ok"] = true;
			result["missing"] = new JArray(missing.ToArray());
			result["valid"] = p.Validate();
			result["effective"] = p.EffectiveCanonicalJson();
			result["hash"] = p.EffectiveHash();
			result["clone_hash"] = p.Clone().EffectiveHash();
		}
		catch (Exception e)
		{
			result["ok"] = false;
			result["error"] = e.GetType().Name + ": " + e.Message;
		}
		return result;
	}
}
