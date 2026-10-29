using System.Collections.Generic;
using Codebase.Simulation.Harness;
using Newtonsoft.Json.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Codebase.Simulation.Rebuild
{
	// The parameter asset (design 4.8; unit C3c). It lives outside Resources/, so player builds do not carry it
	// and use the code defaults. Only ED/SimulationParamsImporter writes it, in a fresh editor process that never
	// entered Play; runs read it through the AssetDatabase and work on a copy.
	public class SimulationRebuildParamsAsset : ScriptableObject
	{
		public const string AssetPath = "Assets/Simulation/Simulation Rebuild Params.asset";

		public SimulationRebuildParams Params = new SimulationRebuildParams();
	}

	// Where a run's parameters come from (design 4.8), reported as manifest "params" (tufu.sim.params/1):
	// - batch, job.params_source "request" (default): job.params over the code defaults (null: the code
	//   defaults); unknown members are errors; job.params_hash, when given, must equal the canonical hash of
	//   job.params (Python's params_hash);
	// - batch, job.params_source "asset": the parameter asset, as an interactive run reads it; job.params must
	//   be null;
	// - interactive (no batch job): the asset in the editor (code defaults when it is missing), the code
	//   defaults in a player.
	// "hash" is the canonical hash of the effective values (SimulationRebuildParams.EffectiveHash).
	public static class SimulationRebuildParamsLoader
	{
		public const string Schema = "tufu.sim.params/1";
		public const string SourceRequest = "request", SourceAsset = "asset", SourceCodeDefaults = "code_defaults";

		// Null, or why the job cannot run (exit code 5). `result` is always usable (code defaults on errors).
		// Public for the importer's read-back, which takes this path with a non-default asset.
		public static string Load(SimulationRunRequest.Job job, out SimulationRebuildParams result, out JObject report)
		{
			result = new SimulationRebuildParams();
			report = new JObject { ["schema"] = Schema, ["interactive"] = job == null };
			string error = job == null ? LoadInteractive(report, ref result) : LoadBatch(job, report, ref result);
			if (error == null) error = result.Validate();
			report["hash"] = result.EffectiveHash();
			report["roll_mode"] = (int)result.RollMode;
			report["seed"] = result.Seed;
			if (report["request_hash"] != null && report["request_hash"].Type == JTokenType.String)
				report["matches_request"] = (string)report["request_hash"] == (string)report["hash"];
			report["error"] = error;
			if (error != null) result = new SimulationRebuildParams();
			return error;
		}

		private static string LoadBatch(SimulationRunRequest.Job job, JObject report, ref SimulationRebuildParams result)
		{
			string source = job.ParamsSource ?? SourceRequest;
			report["source"] = source;
			report["request_hash_given"] = job.ParamsHash;
			if (source == SourceAsset)
			{
				if (job.Params != null) return "params_source \"asset\" takes no params object";
				SimulationRebuildParams loaded = LoadAsset(report, out string error);
				if (loaded == null) return error ?? "parameter asset " + SimulationRebuildParamsAsset.AssetPath + " not found";
				result = loaded;
				return null;
			}
			if (source != SourceRequest) return "params_source must be \"request\" or \"asset\", got \"" + source + "\"";
			if (job.ParamsReadError != null) return "params: " + job.ParamsReadError;
			return SimulationRebuildParams.FromRequest(job.Params, job.ParamsHash, report, out result);
		}

		private static string LoadInteractive(JObject report, ref SimulationRebuildParams result)
		{
#if UNITY_EDITOR
			SimulationRebuildParams loaded = LoadAsset(report, out string error);
			if (error != null) return error;
			report["source"] = loaded != null ? SourceAsset : SourceCodeDefaults;
			if (loaded != null) result = loaded;
			return null;
#else
			report["source"] = SourceCodeDefaults;
			return null;
#endif
		}

		// A copy of the asset's parameters through the AssetDatabase; null when there is no asset (or no editor).
		public static SimulationRebuildParams LoadAsset(JObject report, out string error)
		{
			error = null;
			report["asset_path"] = SimulationRebuildParamsAsset.AssetPath;
#if UNITY_EDITOR
			var asset = AssetDatabase.LoadAssetAtPath<SimulationRebuildParamsAsset>(SimulationRebuildParamsAsset.AssetPath);
			report["asset_guid"] = AssetDatabase.AssetPathToGUID(SimulationRebuildParamsAsset.AssetPath);
			report["asset_found"] = asset != null;
			if (asset == null) return null;
			if (asset.Params == null)
			{
				error = "parameter asset " + SimulationRebuildParamsAsset.AssetPath + " has no Params";
				return null;
			}
			try
			{
				return asset.Params.Clone();
			}
			catch (System.Exception e)
			{
				error = "parameter asset: " + e.Message;
				return null;
			}
#else
			report["asset_found"] = false;
			error = "the parameter asset is read in the editor only";
			return null;
#endif
		}
	}
}
