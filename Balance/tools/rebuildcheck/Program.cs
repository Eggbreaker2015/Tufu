using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
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
//   {"op": "request", "text": "<run request JSON>"} -> {"jobs": [{"error", "report", "effective"}]} or {"read_error"}:
//     SimulationRunRequest.Load's exact params, then the loader's checks (SimulationRebuildParams.FromRequest)
//   {"op": "parse", "texts": ["<JSON>", ...]} -> {"texts": [canonical text or "error: ..."]}
//   {"op": "newtonsoft", "texts": ["<JSON number>", ...]} -> {"bits": ["<16 hex>", ...]}: the runtime's own
//     parse through Newtonsoft (what the request reader holds before SimulationRunRequest.ReadExactParams)
// Commands are read with SimulationCanonicalJson.ParseToken, as the editor reads request params; runs under .NET
// and under the editor's Mono (rebuildcheck.py).
internal static class Program
{
	private static int Main()
	{
		string input;
		using (var stdin = new StreamReader(Console.OpenStandardInput(), new UTF8Encoding(false)))
			input = stdin.ReadToEnd();
		var commands = (JArray)SimulationCanonicalJson.ParseToken(input);
		var results = new JArray();
		foreach (JObject command in commands.Cast<JObject>()) results.Add(Run(command));
		var output = new StringWriter(CultureInfo.InvariantCulture);
		using (var writer = new JsonTextWriter(output)) results.WriteTo(writer);
		byte[] bytes = new UTF8Encoding(false).GetBytes(output.ToString());
		using (Stream stdout = Console.OpenStandardOutput()) stdout.Write(bytes, 0, bytes.Length);
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
            case "export-format":
                return new JObject { ["texts"] = new JArray(command["doubles"].Select(h => (object)Codebase.Simulation.Harness.SimulationJson.JsonDouble(
                    BitConverter.Int64BitsToDouble(unchecked((long)ulong.Parse((string)h, NumberStyles.HexNumber))))).ToArray()) };
            case "export-object":
                var numbers = new JArray(command["doubles"].Select(h => (object)BitConverter.Int64BitsToDouble(
                    unchecked((long)ulong.Parse((string)h, NumberStyles.HexNumber)))).ToArray());
                return new JObject { ["text"] = Codebase.Simulation.Harness.SimulationJson.Write(
                    new JObject { ["generator"] = new JObject { ["values"] = numbers } }) };
            case "runtime-double-parse":
                return new JObject { ["bits"] = new JArray(command["texts"].Select(t => (object)BitConverter.DoubleToInt64Bits(
                    double.Parse((string)t, NumberStyles.Float, CultureInfo.InvariantCulture)).ToString("x16")).ToArray()) };
			case "params":
				return Params(command);
			case "request":
				return Request((string)command["text"]);
			case "newtonsoft":
				return new JObject { ["bits"] = new JArray(command["texts"].Select(t => (object)NewtonsoftBits((string)t)).ToArray()) };
			case "parse":
				return new JObject { ["texts"] = new JArray(command["texts"].Select(t => (object)ParseText((string)t)).ToArray()) };
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
			if (call[1].Type == JTokenType.Null) results.Add(SimulationCanonicalJson.FormatDouble(random.NextDouble(stream))); // not "R": Mono's is not round-trip
			else results.Add(random.Roll(stream, (double)call[1]));
		}
		var carried = new JObject();
		foreach (string name in random.StreamNames) carried[name] = BitConverter.DoubleToInt64Bits(random.Carried(name)).ToString("x16");
		return new JObject { ["results"] = results, ["state"] = random.StateText(), ["carried"] = carried };
	}

	private static string NewtonsoftBits(string number)
	{
		using (var reader = new JsonTextReader(new StringReader("[" + number + "]")) { FloatParseHandling = FloatParseHandling.Double })
		{
			double d = (double)((JValue)JArray.Load(reader)[0]).Value;
			return BitConverter.DoubleToInt64Bits(d).ToString("x16", CultureInfo.InvariantCulture);
		}
	}

	private static string ParseText(string text)
	{
		try
		{
			return SimulationCanonicalJson.Write(SimulationCanonicalJson.ParseToken(text));
		}
		catch (Exception e)
		{
			return "error: " + e.Message;
		}
	}

	private static JObject Request(string text)
	{
		List<JObject> exact;
		JObject root;
		try
		{
			root = SimulationCanonicalJson.Parse(text);
			exact = SimulationCanonicalJson.JobParams(text);
		}
		catch (Exception e)
		{
			return new JObject { ["read_error"] = e.Message };
		}
		var jobs = new JArray();
		for (int i = 0; i < exact.Count; i++)
		{
			var report = new JObject();
			string error = SimulationRebuildParams.FromRequest(exact[i], (string)root["jobs"][i]["params_hash"], report, out SimulationRebuildParams p);
			jobs.Add(new JObject { ["error"] = error, ["report"] = report, ["effective"] = error == null ? p.EffectiveCanonicalJson() : null });
		}
		return new JObject { ["jobs"] = jobs };
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
