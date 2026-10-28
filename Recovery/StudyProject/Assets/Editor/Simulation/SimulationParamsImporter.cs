using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Codebase.Simulation.Harness;
using Codebase.Simulation.Rebuild;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Codebase.Simulation.EditorTools
{
	// Parameter JSON -> Assets/Simulation/Simulation Rebuild Params.asset (design 4.8; unit C3c). One of the two
	// guarded editor entries that write assets (design 1.5 C# rule 6), started by tufu_balance.unity.paramsio
	// ("params import") in a fresh batch editor:
	//   Unity -batchmode -projectPath <run worktree>/Recovery/StudyProject ... \
	//     -executeMethod Codebase.Simulation.EditorTools.SimulationParamsImporter.Import \
	//     -tufuSimParams <params.json> -tufuSimParamsReport <import_report.json>
	// It refuses (exit 2) in a process that has entered Play mode at any time: Play leaves in-memory changes on
	// config assets (PlayerConfig modifiers) that any save in that process could write to disk. The input
	// ({"schema": "tufu.sim.params/1", "params": {...}, "params_hash": "..."}) must be a complete object
	// whose canonical hash is params_hash and whose floats are float32 values (exit 5 otherwise). Only the
	// parameter asset is saved; it is read back through the AssetDatabase loader the runs use.
	[InitializeOnLoad]
	public static class SimulationParamsImporter
	{
		public const string InputSchema = "tufu.sim.params/1";
		public const string ReportSchema = "tufu.sim.params-import/1";
		private const string EnteredPlayKey = "TufuSimParams.entered_play";
		private const string LogTag = "[TufuSim] ";

		static SimulationParamsImporter()
		{
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
		}

		private static void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			if (state == PlayModeStateChange.ExitingEditMode) SessionState.SetBool(EnteredPlayKey, true);
			// Batch runs log the guard's verdict once per Play exit (tests/unity/test_params.py reads it).
			if (state == PlayModeStateChange.ExitingPlayMode && Application.isBatchMode)
				Debug.Log(LogTag + "params import guard after Play: " + (Refusal() ?? "allowed"));
		}

		// Null when this process may write the parameter asset, else why not.
		public static string Refusal()
		{
			if (SessionState.GetBool(EnteredPlayKey, false)) return "refused: this editor process has entered Play mode";
			if (EditorApplication.isPlayingOrWillChangePlaymode) return "refused: the editor is in or entering Play mode";
			if (!Application.isBatchMode) return "refused: params import runs only in a batch-mode editor started for it";
			return null;
		}

		public static void Import()
		{
			string reportPath = SimulationBatch.GetArg("-tufuSimParamsReport");
			var report = new JObject { ["schema"] = ReportSchema, ["asset_path"] = SimulationRebuildParamsAsset.AssetPath, ["ok"] = false };
			int code;
			try
			{
				code = Run(report);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				report["error"] = e.GetType().Name + ": " + e.Message;
				code = SimulationBatch.ExitException;
			}
			report["exit_code"] = code;
			if (!string.IsNullOrEmpty(reportPath)) SimulationJson.WriteText(reportPath, report.ToString(Newtonsoft.Json.Formatting.Indented).Replace("\r\n", "\n") + "\n");
			Debug.Log(LogTag + "params import: exit " + code + (report["error"] != null && report["error"].Type != JTokenType.Null ? ", " + report["error"] : ""));
			EditorApplication.Exit(code);
		}

		private static int Run(JObject report)
		{
			string refusal = Refusal();
			report["entered_play"] = SessionState.GetBool(EnteredPlayKey, false);
			if (refusal != null)
			{
				report["error"] = refusal;
				return SimulationBatch.ExitGuard;
			}
			string input = SimulationBatch.GetArg("-tufuSimParams");
			if (string.IsNullOrEmpty(input) || !File.Exists(input))
			{
				report["error"] = "missing -tufuSimParams <params.json>";
				return SimulationBatch.ExitBadRequest;
			}
			report["input"] = Path.GetFullPath(input);
			JObject file = SimulationCanonicalJson.Parse(File.ReadAllText(input, Encoding.UTF8));
			if ((string)file["schema"] != InputSchema || !(file["params"] is JObject json) || file["params_hash"]?.Type != JTokenType.String)
			{
				report["error"] = "input must be {\"schema\": \"" + InputSchema + "\", \"params\": {...}, \"params_hash\": \"...\"}";
				return SimulationBatch.ExitBadRequest;
			}
			string requestHash = SimulationCanonicalJson.Hash(json);
			report["params_hash"] = (string)file["params_hash"];
			report["request_hash"] = requestHash;
			if (requestHash != (string)file["params_hash"])
			{
				report["error"] = "params_hash is not the canonical hash " + requestHash + " of params";
				return SimulationBatch.ExitBadRequest;
			}
			SimulationRebuildParams parameters;
			try
			{
				parameters = SimulationRebuildParams.FromJson(json, true, out List<string> _);
			}
			catch (Exception e)
			{
				report["error"] = "params: " + e.Message;
				return SimulationBatch.ExitBadRequest;
			}
			string invalid = parameters.Validate();
			report["effective_hash"] = parameters.EffectiveHash();
			if (invalid == null && parameters.EffectiveHash() != requestHash)
				invalid = "params hold a float that is not a float32 value (effective " + parameters.EffectiveCanonicalJson() + ")";
			if (invalid != null)
			{
				report["error"] = invalid;
				return SimulationBatch.ExitBadRequest;
			}

			string full = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? "", SimulationRebuildParamsAsset.AssetPath);
			report["file_sha256_before"] = Sha256File(full);
			var asset = AssetDatabase.LoadAssetAtPath<SimulationRebuildParamsAsset>(SimulationRebuildParamsAsset.AssetPath);
			report["created"] = asset == null;
			if (asset == null)
			{
				asset = ScriptableObject.CreateInstance<SimulationRebuildParamsAsset>();
				asset.Params = parameters;
				AssetDatabase.CreateAsset(asset, SimulationRebuildParamsAsset.AssetPath);
			}
			else
			{
				asset.Params = parameters;
				EditorUtility.SetDirty(asset);
			}
			AssetDatabase.SaveAssetIfDirty(asset);
			AssetDatabase.ImportAsset(SimulationRebuildParamsAsset.AssetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
			Resources.UnloadAsset(asset);

			// Read back the way a run reads it.
			var readBack = new JObject();
			SimulationRebuildParams loaded = SimulationRebuildParamsLoader.LoadAsset(readBack, out string error);
			report["read_back"] = readBack;
			report["file_sha256"] = Sha256File(full);
			if (loaded == null)
			{
				report["error"] = "read back: " + (error ?? "asset not found");
				return SimulationBatch.ExitException;
			}
			report["read_back_hash"] = loaded.EffectiveHash();
			if (loaded.EffectiveHash() != requestHash)
			{
				report["error"] = "read back " + loaded.EffectiveCanonicalJson() + " differs from the input";
				return SimulationBatch.ExitException;
			}
			// And through the run loader on the new (non-default) values: a batch job with params_source "asset",
			// and the interactive branch (no job).
			foreach (var (key, job) in new[] { ("read_back_job", new SimulationRunRequest.Job { JobId = "params-import", ParamsSource = SimulationRebuildParamsLoader.SourceAsset }),
			                                   ("read_back_interactive", (SimulationRunRequest.Job)null) })
			{
				string loadError = SimulationRebuildParamsLoader.Load(job, out SimulationRebuildParams viaLoader, out JObject loaderReport);
				loaderReport["result_hash"] = viaLoader.EffectiveHash();
				report[key] = loaderReport;
				if (loadError != null || (string)loaderReport["source"] != SimulationRebuildParamsLoader.SourceAsset || viaLoader.EffectiveHash() != requestHash)
				{
					report["error"] = key + ": " + (loadError ?? "source " + loaderReport["source"] + ", values " + viaLoader.EffectiveCanonicalJson());
					return SimulationBatch.ExitException;
				}
			}
			report["ok"] = true;
			return SimulationBatch.ExitOk;
		}

		private static string Sha256File(string path)
		{
			if (!File.Exists(path)) return null;
			using (SHA256 sha = SHA256.Create())
				return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
		}
	}
}
