using System;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace Codebase.Simulation.EditorTools
{
	// Headless entries (design 1.4.4, 2.2; unit C0b, from the C0a spike):
	//   Unity -batchmode -projectPath <run worktree>/Recovery/StudyProject \
	//     -disable-assembly-updater Unity.Mathematics.dll Unity.Burst.dll -logFile <run_dir>/unity.log \
	//     -executeMethod Codebase.Simulation.EditorTools.SimulationBatch.Run -tufuSimRequest <run_dir>/request.json
	// No -quit: SimulationPlayModeDriver runs every job (one Play session each) and ends the editor with
	// EditorApplication.Exit(code). SelfTest is Run with entry "selftest" in the reports. Serve keeps the
	// editor up and takes requests from a queue directory (-tufuSimServe <serve.json>).
	// Exit codes: 0 ok, 1 exception, 2 guard, 3 timeout, 4 not ready, 5 bad request, 6 self-check,
	// 7 dev worktree changed (launcher only), 8 ready check.
	public static class SimulationBatch
	{
		public const int ExitOk = 0, ExitException = 1, ExitGuard = 2, ExitTimeout = 3, ExitNotReady = 4, ExitBadRequest = 5,
			ExitSelfCheck = 6, ExitDevChanged = 7, ExitReadyCheck = 8;

		public static void Run() => Start("run");

		public static void SelfTest() => Start("selftest");

		public static void Serve()
		{
			try
			{
				string config = GetArg("-tufuSimServe");
				if (string.IsNullOrEmpty(config))
				{
					Debug.LogError("[TufuSim] missing -tufuSimServe <serve.json>");
					EditorApplication.Exit(ExitBadRequest);
					return;
				}
				SimulationPlayModeDriver.BeginServe(config);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				SimulationPlayModeDriver.Abort(ExitException);
			}
		}

		private static void Start(string entry)
		{
			try
			{
				string request = GetArg("-tufuSimRequest");
				if (string.IsNullOrEmpty(request))
				{
					Debug.LogError("[TufuSim] missing -tufuSimRequest <request.json>");
					EditorApplication.Exit(ExitBadRequest);
					return;
				}
				SimulationPlayModeDriver.BeginRun(entry, request);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				SimulationPlayModeDriver.Abort(ExitException);
			}
		}

		internal static string GetArg(string name)
		{
			string[] args = Environment.GetCommandLineArgs();
			int i = Array.IndexOf(args, name);
			return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
		}
	}
}
